namespace MiniOS.Simulator;

public enum EstadoKernel { Detenido, Ejecutando, Pausado }

public enum EstadoProceso
{
    Nuevo,
    Listo,
    Ejecutando,
    Bloqueado,
    Terminado
}
