namespace MiniOS.Simulator;

public sealed class CPU
{
    public Proceso? ProcesoActual { get; private set; }
    public bool Ocupada => ProcesoActual is not null;
    public int Uso => Ocupada ? 100 : 0;

    public void Ejecutar(Proceso proceso)
    {
        ProcesoActual = proceso;
        proceso.Estado = EstadoProceso.Ejecutando;
    }

    public void EjecutarTick()
    {
        if (ProcesoActual is null)
            return;

        ProcesoActual.TiempoRestante = Math.Max(0, ProcesoActual.TiempoRestante - 1);
        ProcesoActual.TiempoCpuRecibido++;

        if (ProcesoActual.TiempoRestante == 0)
            ProcesoActual.Estado = EstadoProceso.Terminado;
    }

    public Proceso? Liberar(bool terminar = false)
    {
        var anterior = ProcesoActual;

        if (anterior is not null && terminar)
            anterior.Estado = EstadoProceso.Terminado;
        else if (anterior is not null && anterior.Estado == EstadoProceso.Ejecutando)
            anterior.Estado = EstadoProceso.Listo;

        ProcesoActual = null;
        return anterior;
    }
}
