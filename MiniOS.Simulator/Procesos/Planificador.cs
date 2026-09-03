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
    // FCFS usa una cola FIFO real.
    private readonly Queue<Proceso> colaFcfs = new();

    // SJF usa una cola de prioridad: primero la ráfaga más corta disponible.
    // Los desempates se resuelven por tiempo de llegada y luego por ID.
    private readonly PriorityQueue<Proceso, (int Rafaga, int Llegada, int Id)> colaSjf = new();

    // Round Robin usa su propia cola FIFO. Un proceso que agota el quantum
    // vuelve al final de esta cola si todavía no ha terminado.
    private readonly Queue<Proceso> colaRoundRobin = new();

    private readonly HashSet<int> idsEnListos = [];

    public AlgoritmoPlanificacion Algoritmo { get; set; } = AlgoritmoPlanificacion.FCFS;

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
        _ => colaFcfs.ToList()
    };

    public void Reiniciar()
    {
        colaFcfs.Clear();
        colaSjf.Clear();
        colaRoundRobin.Clear();
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

                default:
                    colaFcfs.Enqueue(proceso);
                    break;
            }

            incorporados.Add(proceso);
        }

        return incorporados;
    }

    public Proceso? SeleccionarSiguiente()
    {
        return Algoritmo switch
        {
            AlgoritmoPlanificacion.FCFS => SeleccionarFcfs(),
            AlgoritmoPlanificacion.SJF => SeleccionarSjf(),
            AlgoritmoPlanificacion.RoundRobin => SeleccionarRoundRobin(),
            _ => SeleccionarFcfs()
        };
    }

    public void Reencolar(Proceso proceso)
    {
        if (Algoritmo != AlgoritmoPlanificacion.RoundRobin || proceso.Terminado)
            return;

        proceso.Estado = EstadoProceso.Listo;

        if (idsEnListos.Add(proceso.Id))
            colaRoundRobin.Enqueue(proceso);
    }

    public void NotificarFinalizacion(Proceso proceso)
    {
        idsEnListos.Remove(proceso.Id);
    }

    private Proceso? SeleccionarFcfs()
    {
        while (colaFcfs.Count > 0)
        {
            var proceso = colaFcfs.Dequeue();
            idsEnListos.Remove(proceso.Id);

            if (!proceso.Terminado)
                return proceso;
        }

        return null;
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

    private Proceso? SeleccionarRoundRobin()
    {
        while (colaRoundRobin.Count > 0)
        {
            var proceso = colaRoundRobin.Dequeue();
            idsEnListos.Remove(proceso.Id);

            if (!proceso.Terminado)
                return proceso;
        }

        return null;
    }
}
