namespace MiniOS.Simulator;

public enum AlgoritmoPlanificacion
{
    FCFS,
    SJF,
    RoundRobin,
    Prioridad,
    ColasMultiples,
    Garantizada,
    DosNiveles
}

public sealed class Planificador
{
    private readonly Queue<Proceso> colaFcfs = new();
    private readonly PriorityQueue<Proceso, (int Rafaga, int Llegada, int Id)> colaSjf = new();
    private readonly Queue<Proceso> colaRoundRobin = new();
    private readonly PriorityQueue<Proceso, (int Prioridad, int Llegada, int Id)> colaPrioridad = new();

    // Colas múltiples mantiene tres estructuras FIFO independientes.
    private readonly Queue<Proceso> colaNivel1 = new();
    private readonly Queue<Proceso> colaNivel2 = new();
    private readonly Queue<Proceso> colaNivel3 = new();

    // Garantizada conserva los listos en un conjunto y reconstruye una cola
    // de prioridad en cada decisión porque el ratio CPU recibida / CPU ideal cambia.
    private readonly Dictionary<int, Proceso> listosGarantizada = new();

    // Dos niveles: el planificador superior administra residencia RAM/disco,
    // mientras el inferior selecciona únicamente entre procesos residentes.
    private readonly Queue<Proceso> colaResidentesDosNiveles = new();
    private readonly Queue<Proceso> colaSuspendidosDosNiveles = new();
    private readonly Dictionary<int, Proceso> residentesDosNiveles = new();
    private readonly HashSet<int> idsSuspendidosDosNiveles = [];

    private readonly HashSet<int> idsEnListos = [];
    private int cupoResidentes = 2;

    public AlgoritmoPlanificacion Algoritmo { get; set; } = AlgoritmoPlanificacion.FCFS;

    public int CupoResidentes
    {
        get => cupoResidentes;
        set => cupoResidentes = Math.Max(1, value);
    }

    public IReadOnlyList<Proceso> ResidentesDosNiveles => residentesDosNiveles.Values
        .OrderBy(p => p.Id)
        .ToList();

    public IReadOnlyList<Proceso> SuspendidosDosNiveles => colaSuspendidosDosNiveles
        .Where(p => !p.Terminado)
        .ToList();

    public IReadOnlyList<Proceso> ColaListos => Algoritmo switch
    {
        AlgoritmoPlanificacion.FCFS => colaFcfs.ToList(),
        AlgoritmoPlanificacion.SJF => colaSjf.UnorderedItems
            .OrderBy(x => x.Priority.Rafaga)
            .ThenBy(x => x.Priority.Llegada)
            .ThenBy(x => x.Priority.Id)
            .Select(x => x.Element)
            .ToList(),
        AlgoritmoPlanificacion.RoundRobin => colaRoundRobin.ToList(),
        AlgoritmoPlanificacion.Prioridad => colaPrioridad.UnorderedItems
            .OrderBy(x => x.Priority.Prioridad)
            .ThenBy(x => x.Priority.Llegada)
            .ThenBy(x => x.Priority.Id)
            .Select(x => x.Element)
            .ToList(),
        AlgoritmoPlanificacion.ColasMultiples => colaNivel1
            .Concat(colaNivel2)
            .Concat(colaNivel3)
            .ToList(),
        AlgoritmoPlanificacion.Garantizada => listosGarantizada.Values
            .OrderBy(p => CalcularRatioGarantizado(p))
            .ThenBy(p => p.TiempoLlegada)
            .ThenBy(p => p.Id)
            .ToList(),
        AlgoritmoPlanificacion.DosNiveles => colaResidentesDosNiveles
            .Where(p => !p.Terminado)
            .ToList(),
        _ => colaFcfs.ToList()
    };

    public void Reiniciar()
    {
        colaFcfs.Clear();
        colaSjf.Clear();
        colaRoundRobin.Clear();
        colaPrioridad.Clear();
        colaNivel1.Clear();
        colaNivel2.Clear();
        colaNivel3.Clear();
        listosGarantizada.Clear();
        colaResidentesDosNiveles.Clear();
        colaSuspendidosDosNiveles.Clear();
        residentesDosNiveles.Clear();
        idsSuspendidosDosNiveles.Clear();
        idsEnListos.Clear();
    }

    public IReadOnlyList<Proceso> IncorporarProcesos(IEnumerable<Proceso> procesos, int tiempoActual)
    {
        var incorporados = new List<Proceso>();

        foreach (var proceso in procesos
                     .Where(p => p.TiempoLlegada <= tiempoActual && !p.Terminado && p.Estado == EstadoProceso.Nuevo)
                     .OrderBy(p => p.TiempoLlegada)
                     .ThenBy(p => p.Id))
        {
            if (Algoritmo == AlgoritmoPlanificacion.DosNiveles)
            {
                IncorporarDosNiveles(proceso);
                incorporados.Add(proceso);
                continue;
            }

            if (!idsEnListos.Add(proceso.Id))
                continue;

            proceso.Estado = EstadoProceso.Listo;

            switch (Algoritmo)
            {
                case AlgoritmoPlanificacion.SJF:
                    colaSjf.Enqueue(proceso, (proceso.RafagaCPU, proceso.TiempoLlegada, proceso.Id));
                    break;
                case AlgoritmoPlanificacion.RoundRobin:
                    colaRoundRobin.Enqueue(proceso);
                    break;
                case AlgoritmoPlanificacion.Prioridad:
                    colaPrioridad.Enqueue(proceso, (proceso.Prioridad, proceso.TiempoLlegada, proceso.Id));
                    break;
                case AlgoritmoPlanificacion.ColasMultiples:
                    EncolarColaMultiple(proceso);
                    break;
                case AlgoritmoPlanificacion.Garantizada:
                    listosGarantizada[proceso.Id] = proceso;
                    break;
                default:
                    colaFcfs.Enqueue(proceso);
                    break;
            }

            incorporados.Add(proceso);
        }

        return incorporados;
    }

    public Proceso? SeleccionarSiguiente(int tiempoActual = 0, int procesosActivos = 1)
    {
        return Algoritmo switch
        {
            AlgoritmoPlanificacion.FCFS => SeleccionarDeCola(colaFcfs),
            AlgoritmoPlanificacion.SJF => SeleccionarSjf(),
            AlgoritmoPlanificacion.RoundRobin => SeleccionarDeCola(colaRoundRobin),
            AlgoritmoPlanificacion.Prioridad => SeleccionarPrioridad(),
            AlgoritmoPlanificacion.ColasMultiples => SeleccionarColasMultiples(),
            AlgoritmoPlanificacion.Garantizada => SeleccionarGarantizada(),
            AlgoritmoPlanificacion.DosNiveles => SeleccionarDeCola(colaResidentesDosNiveles),
            _ => SeleccionarDeCola(colaFcfs)
        };
    }

    public bool HayPrioridadSuperiorA(Proceso procesoActual)
    {
        if (Algoritmo != AlgoritmoPlanificacion.Prioridad || colaPrioridad.Count == 0)
            return false;

        return colaPrioridad.TryPeek(out _, out var prioridadEnEspera) &&
               prioridadEnEspera.Prioridad < procesoActual.Prioridad;
    }

    public bool HayColaSuperiorA(Proceso procesoActual)
    {
        if (Algoritmo != AlgoritmoPlanificacion.ColasMultiples)
            return false;

        var colaActual = NormalizarCola(procesoActual.Cola);
        return colaActual switch
        {
            2 => colaNivel1.Count > 0,
            3 => colaNivel1.Count > 0 || colaNivel2.Count > 0,
            _ => false
        };
    }

    public bool HayGarantizadoMasAtrasadoQue(Proceso procesoActual, int tiempoActual = 0, int procesosActivos = 1)
    {
        if (Algoritmo != AlgoritmoPlanificacion.Garantizada || listosGarantizada.Count == 0)
            return false;

        var ratioActual = CalcularRatioGarantizado(procesoActual);
        var menorRatioEnEspera = listosGarantizada.Values.Min(p => CalcularRatioGarantizado(p));
        return menorRatioEnEspera + 0.0001 < ratioActual;
    }

    public void Reencolar(Proceso proceso)
    {
        if (proceso.Terminado)
            return;

        proceso.Estado = EstadoProceso.Listo;

        if (!idsEnListos.Add(proceso.Id))
            return;

        switch (Algoritmo)
        {
            case AlgoritmoPlanificacion.RoundRobin:
                colaRoundRobin.Enqueue(proceso);
                break;
            case AlgoritmoPlanificacion.Prioridad:
                colaPrioridad.Enqueue(proceso, (proceso.Prioridad, proceso.TiempoLlegada, proceso.Id));
                break;
            case AlgoritmoPlanificacion.ColasMultiples:
                EncolarColaMultiple(proceso);
                break;
            case AlgoritmoPlanificacion.Garantizada:
                listosGarantizada[proceso.Id] = proceso;
                break;
            case AlgoritmoPlanificacion.DosNiveles:
                residentesDosNiveles[proceso.Id] = proceso;
                colaResidentesDosNiveles.Enqueue(proceso);
                break;
            default:
                idsEnListos.Remove(proceso.Id);
                break;
        }
    }

    public void NotificarFinalizacion(Proceso proceso)
    {
        idsEnListos.Remove(proceso.Id);
        listosGarantizada.Remove(proceso.Id);
        residentesDosNiveles.Remove(proceso.Id);
        idsSuspendidosDosNiveles.Remove(proceso.Id);
    }

    public IReadOnlyList<Proceso> PromoverSuspendidosHastaCupo()
    {
        var promovidos = new List<Proceso>();

        if (Algoritmo != AlgoritmoPlanificacion.DosNiveles)
            return promovidos;

        while (residentesDosNiveles.Count < CupoResidentes && colaSuspendidosDosNiveles.Count > 0)
        {
            var proceso = ExtraerSuspendido();
            if (proceso is null)
                break;

            proceso.Estado = EstadoProceso.Listo;
            residentesDosNiveles[proceso.Id] = proceso;
            idsEnListos.Add(proceso.Id);
            colaResidentesDosNiveles.Enqueue(proceso);
            promovidos.Add(proceso);
        }

        return promovidos;
    }

    // Cuando vence el quantum del nivel inferior y hay procesos suspendidos,
    // se produce un intercambio: el que acaba de usar CPU sale a disco y el
    // suspendido más antiguo entra en RAM. Si no hay suspendidos, solo reencola.
    public Proceso? RotarDosNiveles(Proceso procesoEjecutado)
    {
        if (Algoritmo != AlgoritmoPlanificacion.DosNiveles || procesoEjecutado.Terminado)
            return null;

        if (colaSuspendidosDosNiveles.Count == 0)
        {
            Reencolar(procesoEjecutado);
            return null;
        }

        residentesDosNiveles.Remove(procesoEjecutado.Id);
        idsEnListos.Remove(procesoEjecutado.Id);
        procesoEjecutado.Estado = EstadoProceso.Suspendido;
        colaSuspendidosDosNiveles.Enqueue(procesoEjecutado);
        idsSuspendidosDosNiveles.Add(procesoEjecutado.Id);

        var promovido = ExtraerSuspendido();
        if (promovido is null)
            return null;

        promovido.Estado = EstadoProceso.Listo;
        residentesDosNiveles[promovido.Id] = promovido;
        idsEnListos.Add(promovido.Id);
        colaResidentesDosNiveles.Enqueue(promovido);
        return promovido;
    }

    public static double CalcularTiempoIdealGarantizado(Proceso proceso, int tiempoActual = 0, int procesosActivos = 1)
        => proceso.TiempoCpuIdealAcumulado;

    public static double CalcularRatioGarantizado(Proceso proceso, int tiempoActual = 0, int procesosActivos = 1)
    {
        var ideal = proceso.TiempoCpuIdealAcumulado;
        if (ideal <= 0.0001)
            return proceso.TiempoCpuRecibido == 0 ? 0 : double.MaxValue;

        return proceso.TiempoCpuRecibido / ideal;
    }

    private Proceso? SeleccionarSjf()
    {
        while (colaSjf.Count > 0)
        {
            var proceso = colaSjf.Dequeue();
            idsEnListos.Remove(proceso.Id);
            if (!proceso.Terminado)
                return proceso;
        }
        return null;
    }

    private Proceso? SeleccionarPrioridad()
    {
        while (colaPrioridad.Count > 0)
        {
            var proceso = colaPrioridad.Dequeue();
            idsEnListos.Remove(proceso.Id);
            if (!proceso.Terminado)
                return proceso;
        }
        return null;
    }

    private Proceso? SeleccionarColasMultiples()
    {
        return SeleccionarDeCola(colaNivel1)
               ?? SeleccionarDeCola(colaNivel2)
               ?? SeleccionarDeCola(colaNivel3);
    }

    private Proceso? SeleccionarGarantizada()
    {
        if (listosGarantizada.Count == 0)
            return null;

        var colaDinamica = new PriorityQueue<Proceso, (double Ratio, int Llegada, int Id)>();
        foreach (var proceso in listosGarantizada.Values.Where(p => !p.Terminado))
        {
            colaDinamica.Enqueue(
                proceso,
                (CalcularRatioGarantizado(proceso), proceso.TiempoLlegada, proceso.Id));
        }

        if (colaDinamica.Count == 0)
            return null;

        var seleccionado = colaDinamica.Dequeue();
        listosGarantizada.Remove(seleccionado.Id);
        idsEnListos.Remove(seleccionado.Id);
        return seleccionado;
    }

    private Proceso? SeleccionarDeCola(Queue<Proceso> cola)
    {
        while (cola.Count > 0)
        {
            var proceso = cola.Dequeue();
            idsEnListos.Remove(proceso.Id);
            if (!proceso.Terminado)
                return proceso;
        }
        return null;
    }

    private void IncorporarDosNiveles(Proceso proceso)
    {
        if (residentesDosNiveles.Count < CupoResidentes)
        {
            proceso.Estado = EstadoProceso.Listo;
            residentesDosNiveles[proceso.Id] = proceso;
            idsEnListos.Add(proceso.Id);
            colaResidentesDosNiveles.Enqueue(proceso);
            return;
        }

        proceso.Estado = EstadoProceso.Suspendido;
        idsSuspendidosDosNiveles.Add(proceso.Id);
        colaSuspendidosDosNiveles.Enqueue(proceso);
    }

    private Proceso? ExtraerSuspendido()
    {
        while (colaSuspendidosDosNiveles.Count > 0)
        {
            var proceso = colaSuspendidosDosNiveles.Dequeue();
            idsSuspendidosDosNiveles.Remove(proceso.Id);
            if (!proceso.Terminado)
                return proceso;
        }
        return null;
    }

    private void EncolarColaMultiple(Proceso proceso)
    {
        switch (NormalizarCola(proceso.Cola))
        {
            case 1:
                colaNivel1.Enqueue(proceso);
                break;
            case 2:
                colaNivel2.Enqueue(proceso);
                break;
            default:
                colaNivel3.Enqueue(proceso);
                break;
        }
    }

    private static int NormalizarCola(int cola) => Math.Clamp(cola, 1, 3);
}
