namespace MiniOS.Simulator;

public sealed class Kernel
{
    private int siguienteId = 5;

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
        new("Teclado", "Entrada"),
        new("Ratón", "Entrada"),
        new("Monitor", "Salida"),
        new("Disco duro 1", "Almacenamiento")
    ];

    public Kernel()
    {
        CargarProcesosBase();
    }

    // =========================================================
    // PROCESOS BASE DEL SISTEMA
    // =========================================================

    private void CargarProcesosBase()
    {
        AgregarProcesoBase(1, "Editor", 0, 5, 2, 3);
        AgregarProcesoBase(2, "Navegador", 1, 3, 1, 2);
        AgregarProcesoBase(3, "Compilador", 2, 4, 3, 1);
        AgregarProcesoBase(4, "Calculadora", 4, 2, 2, 2);

        siguienteId = 5;
    }

    private void AgregarProcesoBase(
        int id,
        string nombre,
        int llegada,
        int rafaga,
        int prioridad,
        int cola)
    {
        const int memoria = 64;

        if (!Memoria.Reservar(memoria))
            return;

        Procesos.Add(new Proceso
        {
            Id = id,
            Nombre = nombre,
            MemoriaMB = memoria,

            TiempoLlegada = llegada,
            RafagaCPU = rafaga,
            TiempoRestante = rafaga,
            Prioridad = prioridad,
            Cola = cola,

            Estado = EstadoProceso.Nuevo
        });
    }

    // =========================================================
    // CONTROL DEL KERNEL
    // =========================================================

    public void Iniciar()
    {
        Estado = EstadoKernel.Ejecutando;

        // Los procesos base ya fueron admitidos por el sistema.
        foreach (var proceso in Procesos.Where(
                     p => p.Estado == EstadoProceso.Nuevo && !p.Terminado))
        {
            proceso.Estado = EstadoProceso.Listo;
        }

        // Si la CPU está libre, ejecuta el primer proceso disponible.
        if (CPU.ProcesoActual is null)
        {
            var listo = Procesos.FirstOrDefault(
                p => p.Estado == EstadoProceso.Listo && !p.Terminado);

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

        CargarProcesosBase();

        Iniciar();
    }

    public void AvanzarSegundo()
    {
        if (Estado == EstadoKernel.Ejecutando)
            Reloj.AvanzarSegundo();
    }

    // =========================================================
    // RESTAURAR LOS 4 PROCESOS BASE
    // =========================================================

    public void RestaurarProcesosBase()
    {
        bool estabaEjecutando = Estado == EstadoKernel.Ejecutando;

        CPU.Liberar();

        Procesos.Clear();
        Memoria.Liberar(Memoria.UsadaMB);

        CargarProcesosBase();

        if (estabaEjecutando)
        {
            foreach (var proceso in Procesos)
                proceso.Estado = EstadoProceso.Listo;

            if (Procesos.Count > 0)
                CPU.Ejecutar(Procesos[0]);
        }
    }

    // =========================================================
    // CREAR PROCESO DESDE LA PANTALLA PRINCIPAL
    // =========================================================

    public Proceso? CrearProceso(string nombre, int memoria)
    {
        if (Estado != EstadoKernel.Ejecutando ||
            !Memoria.Reservar(memoria))
        {
            return null;
        }

        var proceso = new Proceso
        {
            Id = siguienteId++,
            Nombre = nombre,
            MemoriaMB = memoria,

            // Valores predeterminados para que también pueda
            // utilizarse posteriormente en Planificación.
            TiempoLlegada = 0,
            RafagaCPU = 1,
            TiempoRestante = 1,
            Prioridad = 1,
            Cola = 1,

            Estado = EstadoProceso.Listo
        };

        Procesos.Add(proceso);

        if (CPU.ProcesoActual is null)
            CPU.Ejecutar(proceso);

        return proceso;
    }

    // =========================================================
    // CREAR PROCESO DESDE PLANIFICACIÓN
    // =========================================================

    public Proceso? CrearProcesoPlanificacion(
        string nombre,
        int memoria,
        int llegada,
        int rafaga,
        int prioridad,
        int cola)
    {
        if (!Memoria.Reservar(memoria))
            return null;

        var proceso = new Proceso
        {
            Id = siguienteId++,
            Nombre = nombre,
            MemoriaMB = memoria,

            TiempoLlegada = llegada,
            RafagaCPU = rafaga,
            TiempoRestante = rafaga,
            Prioridad = prioridad,
            Cola = cola,

            Estado = Estado == EstadoKernel.Ejecutando
                ? EstadoProceso.Listo
                : EstadoProceso.Nuevo
        };

        Procesos.Add(proceso);

        if (Estado == EstadoKernel.Ejecutando &&
            CPU.ProcesoActual is null)
        {
            CPU.Ejecutar(proceso);
        }

        return proceso;
    }
}