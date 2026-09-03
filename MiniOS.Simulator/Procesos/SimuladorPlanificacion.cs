namespace MiniOS.Simulator;

public sealed record SegmentoEjecucion(int Inicio, int Fin, int ProcesoId, string NombreProceso);

public sealed class SimuladorPlanificacion
{
    private readonly CPU cpu;
    private readonly Planificador planificador = new();
    private readonly List<Proceso> procesos = [];
    private readonly List<SegmentoEjecucion> lineaTiempo = [];

    public int TiempoActual { get; private set; }

    public AlgoritmoPlanificacion Algoritmo
    {
        get => planificador.Algoritmo;
        set => planificador.Algoritmo = value;
    }

    public IReadOnlyList<Proceso> Procesos => procesos;
    public IReadOnlyList<SegmentoEjecucion> LineaTiempo => lineaTiempo;
    public IReadOnlyList<Proceso> ColaListos => planificador.ColaListos;
    public Proceso? ProcesoActual => cpu.ProcesoActual;
    public bool Finalizado => procesos.Count > 0 && procesos.All(p => p.Terminado);

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
    }

    public void ReiniciarEjecucion()
    {
        cpu.Liberar();
        planificador.Reiniciar();
        lineaTiempo.Clear();
        TiempoActual = 0;

        foreach (var proceso in procesos)
            proceso.PrepararParaSimulacion();

        EventoGenerado?.Invoke($"Simulación {NombreAlgoritmo} reiniciada.");
    }

    public void EjecutarPaso()
    {
        if (Finalizado || procesos.Count == 0)
            return;

        var incorporados = planificador.IncorporarProcesos(procesos, TiempoActual);
        foreach (var proceso in incorporados)
            EventoGenerado?.Invoke($"t={TiempoActual}: P{proceso.Id:00} ({proceso.Nombre}) llegó y entró a listos.");

        if (cpu.ProcesoActual is null)
        {
            var siguiente = planificador.SeleccionarSiguiente();
            if (siguiente is not null)
            {
                if (siguiente.TiempoInicio is null)
                {
                    siguiente.TiempoInicio = TiempoActual;
                    siguiente.TiempoRespuesta = TiempoActual - siguiente.TiempoLlegada;
                }

                cpu.Ejecutar(siguiente);
                EventoGenerado?.Invoke(
                    $"t={TiempoActual}: CPU asignada a P{siguiente.Id:00} ({siguiente.Nombre}) por {NombreAlgoritmo}.");
            }
        }

        var ejecutando = cpu.ProcesoActual;

        if (ejecutando is not null)
        {
            RegistrarSegmento(ejecutando.Id, ejecutando.Nombre);
            cpu.EjecutarTick();
        }
        else
        {
            RegistrarSegmento(0, "CPU libre");
        }

        TiempoActual++;
        ActualizarTiemposDeEspera();

        if (ejecutando is not null && ejecutando.Terminado)
        {
            ejecutando.TiempoFinalizacion = TiempoActual;
            ejecutando.TiempoRetorno = TiempoActual - ejecutando.TiempoLlegada;
            ejecutando.TiempoEspera = ejecutando.TiempoRetorno - ejecutando.RafagaCPU;
            planificador.NotificarFinalizacion(ejecutando);
            cpu.Liberar(terminar: true);
            EventoGenerado?.Invoke(
                $"t={TiempoActual}: P{ejecutando.Id:00} finalizó. " +
                $"Espera={ejecutando.TiempoEspera}, respuesta={ejecutando.TiempoRespuesta}, retorno={ejecutando.TiempoRetorno}.");
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

    private void RegistrarSegmento(int procesoId, string nombreProceso)
    {
        var ultimo = lineaTiempo.LastOrDefault();

        if (ultimo is not null && ultimo.ProcesoId == procesoId && ultimo.Fin == TiempoActual)
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
