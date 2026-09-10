namespace MiniOS.Simulator;

public sealed class Kernel
{
    // =========================================================
    // DATOS GENERALES
    // =========================================================

    private int siguienteId = 5;

    public EstadoKernel Estado { get; private set; }
        = EstadoKernel.Detenido;

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

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public Kernel()
    {
        CargarProcesosBase();
    }

    // =========================================================
    // PROCESOS BASE
    // =========================================================

    private void CargarProcesosBase()
    {
        CrearProcesoBase(
            1,
            "Editor",
            llegada: 0,
            rafaga: 5,
            prioridad: 2,
            cola: 3
        );

        CrearProcesoBase(
            2,
            "Navegador",
            llegada: 1,
            rafaga: 3,
            prioridad: 1,
            cola: 2
        );

        CrearProcesoBase(
            3,
            "Compilador",
            llegada: 2,
            rafaga: 4,
            prioridad: 3,
            cola: 1
        );

        CrearProcesoBase(
            4,
            "Calculadora",
            llegada: 4,
            rafaga: 2,
            prioridad: 2,
            cola: 2
        );
    }

    private void CrearProcesoBase(
        int id,
        string nombre,
        int llegada,
        int rafaga,
        int prioridad,
        int cola)
    {
        const int memoriaMB = 64;

        bool reservado =
            Memoria.ReservarProceso(
                id,
                memoriaMB
            );

        if (!reservado)
            return;

        var proceso = new Proceso
        {
            Id = id,
            Nombre = nombre,
            MemoriaMB = memoriaMB,

            TiempoLlegada = llegada,

            RafagaCPU = rafaga,
            TiempoRestante = rafaga,

            Prioridad = prioridad,
            Cola = cola,

            Estado = EstadoProceso.Listo
        };

        Procesos.Add(proceso);
    }

    // =========================================================
    // RESTAURAR LOS 4 PROCESOS BASE
    // =========================================================

    // Este método es utilizado por FrmPlanificacion
    // cuando se presiona "Cargar ejemplo".
    public void RestaurarProcesosBase()
    {
        // Liberamos la CPU actual para evitar que
        // conserve una referencia a un proceso eliminado.
        CPU.Liberar();

        // Borramos todos los procesos del Kernel.
        Procesos.Clear();

        // Reiniciamos completamente el mapa de bits.
        Memoria.LiberarToda();

        // Los nuevos procesos volverán a comenzar en P05.
        siguienteId = 5;

        // Recuperamos P01 - P04.
        CargarProcesosBase();

        // Si el Kernel estaba ejecutándose,
        // volvemos a colocar un proceso en CPU.
        if (Estado == EstadoKernel.Ejecutando)
        {
            var primeroListo =
                Procesos.FirstOrDefault(
                    p =>
                        p.Estado == EstadoProceso.Listo &&
                        !p.Terminado
                );

            if (primeroListo is not null)
                CPU.Ejecutar(primeroListo);
        }
    }

    // =========================================================
    // INICIAR SISTEMA
    // =========================================================

    public void Iniciar()
    {
        Estado = EstadoKernel.Ejecutando;

        if (CPU.ProcesoActual is null)
        {
            var listo =
                Procesos.FirstOrDefault(
                    p =>
                        p.Estado == EstadoProceso.Listo &&
                        !p.Terminado
                );

            if (listo is not null)
                CPU.Ejecutar(listo);
        }
    }

    // =========================================================
    // DETENER SISTEMA
    // =========================================================

    public void Detener()
    {
        Estado = EstadoKernel.Detenido;

        CPU.Liberar();
    }

    // =========================================================
    // REINICIAR SISTEMA
    // =========================================================

    public void Reiniciar()
    {
        Detener();

        Reloj.Reiniciar();

        Procesos.Clear();

        Memoria.LiberarToda();

        siguienteId = 5;

        CargarProcesosBase();

        Iniciar();
    }

    // =========================================================
    // AVANZAR RELOJ
    // =========================================================

    public void AvanzarSegundo()
    {
        if (Estado == EstadoKernel.Ejecutando)
            Reloj.AvanzarSegundo();
    }

    // =========================================================
    // CREAR PROCESO DESDE EL MONITOR PRINCIPAL
    // =========================================================

    public Proceso? CrearProceso(
        string nombre,
        int memoria)
    {
        if (Estado != EstadoKernel.Ejecutando)
            return null;

        if (string.IsNullOrWhiteSpace(nombre))
            return null;

        if (memoria <= 0)
            return null;

        int nuevoId = siguienteId;

        bool reservado =
            Memoria.ReservarProceso(
                nuevoId,
                memoria
            );

        if (!reservado)
            return null;

        var proceso = new Proceso
        {
            Id = nuevoId,
            Nombre = nombre.Trim(),

            MemoriaMB = memoria,

            TiempoLlegada = 0,

            RafagaCPU = 1,
            TiempoRestante = 1,

            Prioridad = 1,
            Cola = 1,

            Estado = EstadoProceso.Listo
        };

        Procesos.Add(proceso);

        siguienteId++;

        // En el monitor principal sí puede ocupar CPU.
        if (CPU.ProcesoActual is null)
            CPU.Ejecutar(proceso);

        return proceso;
    }

    // =========================================================
    // CREAR PROCESO DESDE PLANIFICACIÓN
    // =========================================================

    // ESTE ES UNO DE LOS MÉTODOS QUE TE FALTABA.
    //
    // FrmPlanificacion manda:
    //
    // nombre
    // memoria
    // llegada
    // ráfaga
    // prioridad
    // cola
    //
    // El proceso también queda guardado en Kernel.Procesos
    // para compartir la misma lista entre módulos.
    public Proceso? CrearProcesoPlanificacion(
        string nombre,
        int memoria,
        int llegada,
        int rafaga,
        int prioridad,
        int cola)
    {
        // -----------------------------
        // Validaciones
        // -----------------------------

        if (string.IsNullOrWhiteSpace(nombre))
            return null;

        if (memoria <= 0)
            return null;

        if (llegada < 0)
            return null;

        if (rafaga <= 0)
            return null;

        if (prioridad <= 0)
            return null;

        if (cola < 1 || cola > 3)
            return null;

        int nuevoId = siguienteId;

        // -----------------------------
        // Reservar memoria mediante
        // el MAPA DE BITS
        // -----------------------------

        bool reservado =
            Memoria.ReservarProceso(
                nuevoId,
                memoria
            );

        if (!reservado)
            return null;

        // -----------------------------
        // Crear proceso
        // -----------------------------

        var proceso = new Proceso
        {
            Id = nuevoId,

            Nombre = nombre.Trim(),

            MemoriaMB = memoria,

            TiempoLlegada = llegada,

            RafagaCPU = rafaga,

            TiempoRestante = rafaga,

            Prioridad = prioridad,

            Cola = cola,

            Estado = EstadoProceso.Listo
        };

        // -----------------------------
        // Compartirlo con el Kernel
        // -----------------------------

        Procesos.Add(proceso);

        siguienteId++;

        // IMPORTANTE:
        //
        // Aquí NO hacemos:
        //
        // CPU.Ejecutar(proceso);
        //
        // porque FrmPlanificacion utiliza
        // su propio SimuladorPlanificacion
        // y su propia CPU simulada.

        return proceso;
    }

    // =========================================================
    // FINALIZAR PROCESO
    // =========================================================

    public bool FinalizarProceso(int procesoId)
    {
        var proceso =
            Procesos.FirstOrDefault(
                p => p.Id == procesoId
            );

        if (proceso is null)
            return false;

        if (proceso.Estado == EstadoProceso.Terminado)
            return false;

        // Si estaba utilizando CPU,
        // liberamos primero la CPU.
        if (CPU.ProcesoActual?.Id == procesoId)
        {
            CPU.Liberar(terminar: true);
        }
        else
        {
            proceso.Estado =
                EstadoProceso.Terminado;

            proceso.TiempoRestante = 0;
        }

        // =====================================================
        // MAPA DE BITS:
        //
        // Todos los bloques pertenecientes a este PID
        // vuelven de 1 a 0.
        // =====================================================

        Memoria.LiberarProceso(procesoId);

        // Buscar otro proceso para CPU
        // únicamente en el monitor principal.
        if (
            Estado == EstadoKernel.Ejecutando &&
            CPU.ProcesoActual is null)
        {
            var siguiente =
                Procesos.FirstOrDefault(
                    p =>
                        p.Estado ==
                        EstadoProceso.Listo &&
                        !p.Terminado
                );

            if (siguiente is not null)
                CPU.Ejecutar(siguiente);
        }

        return true;
    }

    // =========================================================
    // OBTENER BLOQUES DE UN PROCESO
    // =========================================================

    public List<int> ObtenerBloquesProceso(
        int procesoId)
    {
        return Memoria.ObtenerBloquesProceso(
            procesoId
        );
    }
}