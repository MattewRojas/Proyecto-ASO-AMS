namespace MiniOS.Simulator;

public sealed class Proceso
{
    public int Id { get; init; }
    public string Nombre { get; init; } = "Proceso";
    public int MemoriaMB { get; init; }

    // Datos utilizados por los algoritmos de planificación.
    public int TiempoLlegada { get; set; }
    public int RafagaCPU { get; set; } = 1;
    public int TiempoRestante { get; set; } = 1;
    public int Prioridad { get; set; } = 1;
    public int Cola { get; set; }

    public EstadoProceso Estado { get; set; } = EstadoProceso.Nuevo;

    // Métricas de planificación.
    public int? TiempoInicio { get; set; }
    public int? TiempoFinalizacion { get; set; }
    public int TiempoEspera { get; set; }
    public int TiempoRespuesta { get; set; }
    public int TiempoRetorno { get; set; }
    public int TiempoCpuRecibido { get; set; }

    public bool Terminado => TiempoRestante <= 0 || Estado == EstadoProceso.Terminado;

    public void PrepararParaSimulacion()
    {
        TiempoRestante = Math.Max(1, RafagaCPU);
        TiempoInicio = null;
        TiempoFinalizacion = null;
        TiempoEspera = 0;
        TiempoRespuesta = 0;
        TiempoRetorno = 0;
        TiempoCpuRecibido = 0;
        Estado = EstadoProceso.Nuevo;
    }

    public override string ToString() =>
        $"P{Id:00}  {Nombre}  ({MemoriaMB} MB) - {Estado}";
}
