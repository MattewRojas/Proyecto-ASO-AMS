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
    private readonly HashSet<int> idsEnCola = [];

    public AlgoritmoPlanificacion Algoritmo { get; set; } = AlgoritmoPlanificacion.FCFS;

    public IReadOnlyList<Proceso> ColaListos => Algoritmo switch
    {
        AlgoritmoPlanificacion.FCFS => colaFcfs.ToList(),
        _ => colaFcfs.ToList()
    };

    public void Reiniciar()
    {
        colaFcfs.Clear();
        idsEnCola.Clear();
    }

    public IReadOnlyList<Proceso> IncorporarProcesos(IEnumerable<Proceso> procesos, int tiempoActual)
    {
        var incorporados = new List<Proceso>();

        foreach (var proceso in procesos
                     .Where(p => p.TiempoLlegada <= tiempoActual && !p.Terminado && p.Estado == EstadoProceso.Nuevo)
                     .OrderBy(p => p.TiempoLlegada)
                     .ThenBy(p => p.Id))
        {
            if (!idsEnCola.Add(proceso.Id))
                continue;

            proceso.Estado = EstadoProceso.Listo;
            colaFcfs.Enqueue(proceso);
            incorporados.Add(proceso);
        }

        return incorporados;
    }

    public Proceso? SeleccionarSiguiente()
    {
        return Algoritmo switch
        {
            AlgoritmoPlanificacion.FCFS => SeleccionarFcfs(),
            _ => SeleccionarFcfs()
        };
    }

    public void NotificarFinalizacion(Proceso proceso)
    {
        idsEnCola.Remove(proceso.Id);
    }

    private Proceso? SeleccionarFcfs()
    {
        while (colaFcfs.Count > 0)
        {
            var proceso = colaFcfs.Dequeue();
            idsEnCola.Remove(proceso.Id);

            if (!proceso.Terminado)
                return proceso;
        }

        return null;
    }
}
