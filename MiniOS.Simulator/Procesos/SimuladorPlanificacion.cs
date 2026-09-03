namespace MiniOS.Simulator;

public sealed record SegmentoEjecucion(int Inicio, int Fin, int ProcesoId, string NombreProceso);

public sealed class SimuladorPlanificacion
{
    private readonly CPU cpu;
    private readonly Planificador planificador = new();
    private readonly List<Proceso> procesos = [];
    private readonly List<SegmentoEjecucion> lineaTiempo = [];
    private int quantum = 2;

    public int TiempoActual { get; private set; }
    public int Quantum
    {
        get => quantum;
        set => quantum = Math.Max(1, value);
    }
    public int QuantumRestante { get; private set; }

    public AlgoritmoPlanificacion Algoritmo
    {
        get => planificador.Algoritmo;
        set
        {
            planificador.Algoritmo = value;
            QuantumRestante = 0;
        }
    }

    public IReadOnlyList<Proceso> Procesos => procesos;
    public IReadOnlyList<SegmentoEjecucion> LineaTiempo => lineaTiempo;
    public IReadOnlyList<Proceso> ColaListos => Algoritmo == AlgoritmoPlanificacion.Garantizada
        ? planificador.ColaListos
            .OrderBy(p => RatioGarantizado(p))
            .ThenBy(p => p.TiempoLlegada)
            .ThenBy(p => p.Id)
            .ToList()
        : planificador.ColaListos;
    public Proceso? ProcesoActual => cpu.ProcesoActual;
    public bool Finalizado => procesos.Count > 0 && procesos.All(p => p.Terminado);

    public int ProcesosActivos => procesos.Count(p => !p.Terminado && p.TiempoLlegada <= TiempoActual);

    public string NombreAlgoritmo => Algoritmo switch
    {
        AlgoritmoPlanificacion.FCFS => "FCFS",
        AlgoritmoPlanificacion.SJF => "SJF",
        AlgoritmoPlanificacion.RoundRobin => "Round Robin",
        AlgoritmoPlanificacion.Prioridad => "Prioridad",
        AlgoritmoPlanificacion.ColasMultiples => "Colas múltiples",
        AlgoritmoPlanificacion.Garantizada => "Planificación garantizada",
        AlgoritmoPlanificacion.DosNiveles => "Planificación a dos niveles",
        _ => Algoritmo.ToString()
    };

    public event Action<string>? EventoGenerado;

    public SimuladorPlanificacion(CPU cpu)
    {
        this.cpu = cpu;
    }

    public void CargarProcesos(IEnumerable<Proceso> nuevosProcesos)
    {
        Reiniciar();
        procesos.AddRange(nuevosProcesos.OrderBy(p => p.TiempoLlegada).ThenBy(p => p.Id));

        foreach (var proceso in procesos)
            proceso.PrepararParaSimulacion();
    }

    public void Reiniciar()
    {
        cpu.Liberar();
        planificador.Reiniciar();
        procesos.Clear();
        lineaTiempo.Clear();
        TiempoActual = 0;
        QuantumRestante = 0;
    }

    public void ReiniciarEjecucion()
    {
        cpu.Liberar();
        planificador.Reiniciar();
        lineaTiempo.Clear();
        TiempoActual = 0;
        QuantumRestante = 0;

        foreach (var proceso in procesos)
            proceso.PrepararParaSimulacion();

        EventoGenerado?.Invoke($"Simulación {NombreAlgoritmo} reiniciada.");
    }

    public double TiempoIdealGarantizado(Proceso proceso)
    {
        return Planificador.CalcularTiempoIdealGarantizado(
            proceso,
            TiempoActual,
            Math.Max(1, ProcesosActivos));
    }

    public double RatioGarantizado(Proceso proceso)
    {
        return Planificador.CalcularRatioGarantizado(
            proceso,
            TiempoActual,
            Math.Max(1, ProcesosActivos));
    }

    public void EjecutarPaso()
    {
        if (Finalizado || procesos.Count == 0)
            return;

        IncorporarLlegadas();

        if (Algoritmo == AlgoritmoPlanificacion.Prioridad &&
            cpu.ProcesoActual is not null &&
            planificador.HayPrioridadSuperiorA(cpu.ProcesoActual))
        {
            var desalojado = cpu.Liberar();
            if (desalojado is not null)
            {
                planificador.Reencolar(desalojado);
                EventoGenerado?.Invoke(
                    $"t={TiempoActual}: P{desalojado.Id:00} fue desalojado porque llegó un proceso de mayor prioridad.");
            }
        }

        if (Algoritmo == AlgoritmoPlanificacion.ColasMultiples &&
            cpu.ProcesoActual is not null &&
            planificador.HayColaSuperiorA(cpu.ProcesoActual))
        {
            var desalojado = cpu.Liberar();
            if (desalojado is not null)
            {
                planificador.Reencolar(desalojado);
                EventoGenerado?.Invoke(
                    $"t={TiempoActual}: P{desalojado.Id:00} (cola {desalojado.Cola}) fue desalojado porque hay procesos en una cola de nivel superior.");
            }
        }

        // La planificación garantizada busca que, con n procesos activos,
        // cada uno reciba aproximadamente 1/n de la CPU. Se compara el tiempo
        // realmente recibido con el tiempo al que cada proceso tiene derecho.
        if (Algoritmo == AlgoritmoPlanificacion.Garantizada &&
            cpu.ProcesoActual is not null &&
            planificador.HayGarantizadoMasAtrasadoQue(
                cpu.ProcesoActual,
                TiempoActual,
                Math.Max(1, ProcesosActivos)))
        {
            var desalojado = cpu.Liberar();
            if (desalojado is not null)
            {
                var ratio = RatioGarantizado(desalojado);
                planificador.Reencolar(desalojado);
                EventoGenerado?.Invoke(
                    $"t={TiempoActual}: P{desalojado.Id:00} cede la CPU; su ratio actual es {ratio:F2} y existe un proceso con menor ratio.");
            }
        }

        var nuevoDespacho = false;

        if (cpu.ProcesoActual is null)
        {
            var siguiente = planificador.SeleccionarSiguiente(
                TiempoActual,
                Math.Max(1, ProcesosActivos));

            if (siguiente is not null)
            {
                if (siguiente.TiempoInicio is null)
                {
                    siguiente.TiempoInicio = TiempoActual;
                    siguiente.TiempoRespuesta = TiempoActual - siguiente.TiempoLlegada;
                }

                cpu.Ejecutar(siguiente);
                nuevoDespacho = true;

                if (Algoritmo == AlgoritmoPlanificacion.RoundRobin)
                    QuantumRestante = Math.Min(Quantum, siguiente.TiempoRestante);

                var detalle = Algoritmo switch
                {
                    AlgoritmoPlanificacion.RoundRobin => $" Quantum={Quantum}.",
                    AlgoritmoPlanificacion.Prioridad => $" Prioridad={siguiente.Prioridad}.",
                    AlgoritmoPlanificacion.ColasMultiples => $" Cola={siguiente.Cola}.",
                    AlgoritmoPlanificacion.Garantizada =>
                        $" CPU recibida={siguiente.TiempoCpuRecibido}, ideal={TiempoIdealGarantizado(siguiente):F2}, ratio={RatioGarantizado(siguiente):F2}.",
                    _ => string.Empty
                };

                EventoGenerado?.Invoke(
                    $"t={TiempoActual}: CPU asignada a P{siguiente.Id:00} ({siguiente.Nombre}) por {NombreAlgoritmo}." + detalle);
            }
        }

        var ejecutando = cpu.ProcesoActual;

        if (ejecutando is not null)
        {
            RegistrarSegmento(
                ejecutando.Id,
                ejecutando.Nombre,
                forzarNuevo: nuevoDespacho &&
                             (Algoritmo == AlgoritmoPlanificacion.RoundRobin ||
                              Algoritmo == AlgoritmoPlanificacion.Garantizada));

            cpu.EjecutarTick();

            if (Algoritmo == AlgoritmoPlanificacion.RoundRobin)
                QuantumRestante = Math.Max(0, QuantumRestante - 1);
        }
        else
        {
            RegistrarSegmento(0, "CPU libre", false);
        }

        TiempoActual++;
        ActualizarTiemposDeEspera();

        if (ejecutando is not null && ejecutando.Terminado)
        {
            FinalizarProceso(ejecutando);
        }
        else if (ejecutando is not null &&
                 Algoritmo == AlgoritmoPlanificacion.RoundRobin &&
                 QuantumRestante == 0)
        {
            IncorporarLlegadas();

            var desalojado = cpu.Liberar();
            if (desalojado is not null)
            {
                planificador.Reencolar(desalojado);
                EventoGenerado?.Invoke(
                    $"t={TiempoActual}: P{desalojado.Id:00} agotó su quantum y vuelve al final de la cola con {desalojado.TiempoRestante} unidades restantes.");
            }
        }

        if (Finalizado)
            EventoGenerado?.Invoke($"Simulación {NombreAlgoritmo} finalizada en t={TiempoActual}.");
    }

    public void EjecutarHastaFinalizar(int limitePasos = 10000)
    {
        var pasos = 0;
        while (!Finalizado && procesos.Count > 0 && pasos++ < limitePasos)
            EjecutarPaso();
    }

    public double EsperaPromedio => Promedio(p => p.TiempoEspera);
    public double RespuestaPromedio => Promedio(p => p.TiempoRespuesta);
    public double RetornoPromedio => Promedio(p => p.TiempoRetorno);

    private void IncorporarLlegadas()
    {
        var incorporados = planificador.IncorporarProcesos(procesos, TiempoActual);
        foreach (var proceso in incorporados)
        {
            var detalle = Algoritmo switch
            {
                AlgoritmoPlanificacion.ColasMultiples => $" en cola {proceso.Cola}",
                AlgoritmoPlanificacion.Garantizada => " con cuota equitativa de CPU",
                _ => string.Empty
            };

            EventoGenerado?.Invoke($"t={TiempoActual}: P{proceso.Id:00} ({proceso.Nombre}) llegó y entró a listos{detalle}.");
        }
    }

    private void FinalizarProceso(Proceso proceso)
    {
        proceso.TiempoFinalizacion = TiempoActual;
        proceso.TiempoRetorno = TiempoActual - proceso.TiempoLlegada;
        proceso.TiempoEspera = proceso.TiempoRetorno - proceso.RafagaCPU;
        planificador.NotificarFinalizacion(proceso);
        cpu.Liberar(terminar: true);
        QuantumRestante = 0;

        EventoGenerado?.Invoke(
            $"t={TiempoActual}: P{proceso.Id:00} finalizó. " +
            $"Espera={proceso.TiempoEspera}, respuesta={proceso.TiempoRespuesta}, retorno={proceso.TiempoRetorno}, CPU recibida={proceso.TiempoCpuRecibido}.");
    }

    private void ActualizarTiemposDeEspera()
    {
        foreach (var proceso in procesos)
        {
            if (!proceso.Terminado &&
                proceso.Estado == EstadoProceso.Listo &&
                proceso.TiempoLlegada < TiempoActual)
            {
                proceso.TiempoEspera++;
            }
        }
    }

    private void RegistrarSegmento(int procesoId, string nombreProceso, bool forzarNuevo)
    {
        var ultimo = lineaTiempo.LastOrDefault();

        if (!forzarNuevo && ultimo is not null && ultimo.ProcesoId == procesoId && ultimo.Fin == TiempoActual)
        {
            lineaTiempo[^1] = ultimo with { Fin = TiempoActual + 1 };
            return;
        }

        lineaTiempo.Add(new SegmentoEjecucion(TiempoActual, TiempoActual + 1, procesoId, nombreProceso));
    }

    private double Promedio(Func<Proceso, int> selector)
    {
        var terminados = procesos.Where(p => p.Terminado).ToList();
        return terminados.Count == 0 ? 0 : terminados.Average(selector);
    }
}
