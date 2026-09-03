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

    public void Reiniciar()
    {
        colaFcfs.Clear();
        idsEnCola.Clear();
    }

    public void IncorporarProcesos(IEnumerable<Proceso> procesos, int tiempoActual)
    {
        foreach (var proceso in procesos
                     .Where(p => p.TiempoLlegada <= tiempoActual && !p.Terminado)
                     .OrderBy(p => p.TiempoLlegada)
                     .ThenBy(p => p.Id))
        {
            if (idsEnCola.Add(proceso.Id))
            {
                proceso.Estado = EstadoProceso.Listo;
                colaFcfs.Enqueue(proceso);
            }
        }
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
