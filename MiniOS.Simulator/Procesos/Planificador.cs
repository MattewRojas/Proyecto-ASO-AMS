namespace MiniOS.Simulator;

public sealed class Planificador
{
    public Proceso? SeleccionarSiguiente(IEnumerable<Proceso> procesos) =>
        procesos.FirstOrDefault(p => p.Estado == EstadoProceso.Listo);
}
