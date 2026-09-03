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

    public void Iniciar()
    {
        Estado = EstadoKernel.Ejecutando;

        // Si el sistema se había detenido, la CPU quedó libre y el proceso que
        // estaba ejecutándose volvió a Listo. Al iniciar de nuevo retomamos el
        // primer proceso listo para mantener coherente el monitor principal.
        if (CPU.ProcesoActual is null)
        {
            var listo = Procesos.FirstOrDefault(p => p.Estado == EstadoProceso.Listo && !p.Terminado);
            if (listo is not null)
                CPU.Ejecutar(listo);
        }
    }

    public void Detener()
    {
        Estado = EstadoKernel.Detenido;
        CPU.Liberar();
    }

    public void Reiniciar()
    {
        Detener();
        Reloj.Reiniciar();
        Procesos.Clear();
        Memoria.Liberar(Memoria.UsadaMB);
        Iniciar();
    }

    public void AvanzarSegundo()
    {
        if (Estado == EstadoKernel.Ejecutando)
            Reloj.AvanzarSegundo();
    }

    public Proceso? CrearProceso(string nombre, int memoria)
    {
        if (Estado != EstadoKernel.Ejecutando || !Memoria.Reservar(memoria))
            return null;

        // En el monitor principal un proceso recién creado ya fue admitido por
        // el kernel, así que debe quedar Listo; el primero pasa a Ejecutando.
        // El estado Nuevo se reserva para el simulador de planificación, donde
        // representa procesos que aún no alcanzan su tiempo de llegada.
        var proceso = new Proceso
        {
            Id = siguienteId++,
            Nombre = nombre,
            MemoriaMB = memoria,
            Estado = EstadoProceso.Listo
        };

        Procesos.Add(proceso);
        if (CPU.ProcesoActual is null)
            CPU.Ejecutar(proceso);
        return proceso;
    }
}
