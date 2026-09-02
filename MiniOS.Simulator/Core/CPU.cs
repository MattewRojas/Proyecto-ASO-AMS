namespace MiniOS.Simulator;

public sealed class CPU
{
    public Proceso? ProcesoActual { get; private set; }
    public int Uso => ProcesoActual is null ? 0 : 35 + (ProcesoActual.Id * 13 % 55);
    public void Ejecutar(Proceso proceso) { ProcesoActual = proceso; proceso.Estado = EstadoProceso.Ejecutando; }
    public void Liberar() { if (ProcesoActual is not null) ProcesoActual.Estado = EstadoProceso.Terminado; ProcesoActual = null; }
}
