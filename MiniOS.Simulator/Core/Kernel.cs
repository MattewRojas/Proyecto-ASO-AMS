namespace MiniOS.Simulator;

public sealed class Kernel
{
    private int siguienteId = 1;
    public EstadoKernel Estado { get; private set; } = EstadoKernel.Detenido;
    public RelojSistema Reloj { get; } = new();
    public TimeSpan Tiempo => Reloj.Tiempo;
    public CPU CPU { get; } = new();
    public Memoria Memoria { get; } = new();
    public SistemaArchivos Archivos { get; } = new();
    public RegistroEventos Registro { get; } = new();
    public List<Proceso> Procesos { get; } = [];
    public List<Dispositivo> Dispositivos { get; } =
    [
        new("Teclado", "Entrada"), new("Ratón", "Entrada"),
        new("Monitor", "Salida"), new("Disco duro 1", "Almacenamiento")
    ];

    public void Iniciar() => Estado = EstadoKernel.Ejecutando;
    public void Detener() { Estado = EstadoKernel.Detenido; CPU.Liberar(); }
    public void Reiniciar() { Detener(); Reloj.Reiniciar(); Procesos.Clear(); Memoria.Liberar(Memoria.UsadaMB); Iniciar(); }
    public void AvanzarSegundo() { if (Estado == EstadoKernel.Ejecutando) Reloj.AvanzarSegundo(); }
    public Proceso? CrearProceso(string nombre, int memoria)
    {
        if (Estado != EstadoKernel.Ejecutando || !Memoria.Reservar(memoria)) return null;
        var proceso = new Proceso { Id = siguienteId++, Nombre = nombre, MemoriaMB = memoria };
        Procesos.Add(proceso);
        if (CPU.ProcesoActual is null) CPU.Ejecutar(proceso);
        return proceso;
    }
}
