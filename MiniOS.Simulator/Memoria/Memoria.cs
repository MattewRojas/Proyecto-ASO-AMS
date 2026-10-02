using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniOS.Simulator;

public sealed class Memoria
{
    // =========================================================
    // CONFIGURACIÓN GENERAL
    // =========================================================

    // Todos los módulos de memoria trabajarán con 1024 MB.
    public int TotalMB { get; } = 1024;

    // Unidad de asignación inicial.
    // El usuario puede modificarla desde FrmMapaBits.
    public int UnidadAsignacionMB { get; private set; } = 4;

    // Compatibilidad con código anterior que todavía utilice
    // el nombre TamanoBloqueMB.
    public int TamanoBloqueMB => UnidadAsignacionMB;

    // Mapa:
    // false = 0 = libre
    // true  = 1 = ocupado
    private bool[] mapaBits = [];

    // PID propietario de cada casilla.
    // null significa que no tiene propietario.
    private int?[] propietarios = [];

    // Memoria que realmente solicitó cada proceso.
    // Se utiliza para calcular fragmentación interna.
    private readonly Dictionary<int, int> memoriaSolicitada = new();

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public Memoria()
    {
        InicializarMapa();
    }

    // =========================================================
    // PROPIEDADES GENERALES
    // =========================================================

    public int TotalBloques =>
        TotalMB / UnidadAsignacionMB;

    public int BloquesOcupados =>
        mapaBits.Count(b => b);

    public int BloquesLibres =>
        TotalBloques - BloquesOcupados;

    public int UsadaMB =>
        BloquesOcupados * UnidadAsignacionMB;

    public int DisponibleMB =>
        TotalMB - UsadaMB;

    // Esta propiedad la utiliza FrmPrincipal.
    public int Porcentaje
    {
        get
        {
            if (TotalMB <= 0)
                return 0;

            return (int)Math.Round(
                UsadaMB * 100.0 / TotalMB
            );
        }
    }

    // =========================================================
    // FRAGMENTACIÓN INTERNA TOTAL
    // =========================================================

    public int FragmentacionInternaMB
    {
        get
        {
            int total = 0;

            foreach (var par in memoriaSolicitada)
            {
                int procesoId = par.Key;
                int solicitada = par.Value;

                int asignada =
                    ObtenerMemoriaAsignadaProceso(
                        procesoId
                    );

                total += Math.Max(
                    0,
                    asignada - solicitada
                );
            }

            return total;
        }
    }

    // =========================================================
    // CREAR / RECREAR MAPA
    // =========================================================

    private void InicializarMapa()
    {
        int cantidad =
            TotalMB / UnidadAsignacionMB;

        mapaBits =
            new bool[cantidad];

        propietarios =
            new int?[cantidad];
    }

    // =========================================================
    // CAMBIAR UNIDAD DE ASIGNACIÓN
    // =========================================================

    public bool ReconfigurarUnidadAsignacion(
        int nuevaUnidadMB,
        IEnumerable<Proceso> procesos)
    {
        // La unidad debe ser positiva.
        if (nuevaUnidadMB <= 0)
            return false;

        // Debe dividir exactamente la memoria total.
        //
        // Ejemplo:
        // 1024 / 4 = 256 casillas
        //
        // 1024 / 3 no sería válido.
        if (TotalMB % nuevaUnidadMB != 0)
            return false;

        // Solo recolocamos procesos que todavía
        // forman parte activa del sistema.
        var activos =
            procesos
                .Where(p => !p.Terminado)
                .OrderBy(p => p.Id)
                .ToList();

        int cantidadCasillas =
            TotalMB / nuevaUnidadMB;

        // Creamos el nuevo mapa de manera temporal.
        // Así no destruimos el mapa actual hasta estar
        // seguros de que todos los procesos caben.
        var nuevoMapa =
            new bool[cantidadCasillas];

        var nuevosPropietarios =
            new int?[cantidadCasillas];

        var nuevasSolicitudes =
            new Dictionary<int, int>();

        int cursor = 0;

        foreach (var proceso in activos)
        {
            int casillasNecesarias =
                (int)Math.Ceiling(
                    proceso.MemoriaMB /
                    (double)nuevaUnidadMB
                );

            // No cabe con esta configuración.
            if (
                cursor + casillasNecesarias >
                cantidadCasillas)
            {
                return false;
            }

            for (
                int i = cursor;
                i < cursor + casillasNecesarias;
                i++)
            {
                nuevoMapa[i] = true;

                nuevosPropietarios[i] =
                    proceso.Id;
            }

            nuevasSolicitudes[proceso.Id] =
                proceso.MemoriaMB;

            cursor += casillasNecesarias;
        }

        // Solo después de comprobar que todo cabe
        // aplicamos realmente la nueva configuración.
        UnidadAsignacionMB =
            nuevaUnidadMB;

        mapaBits =
            nuevoMapa;

        propietarios =
            nuevosPropietarios;

        memoriaSolicitada.Clear();

        foreach (var par in nuevasSolicitudes)
        {
            memoriaSolicitada[par.Key] =
                par.Value;
        }

        return true;
    }

    // =========================================================
    // RESERVAR MEMORIA PARA UN PROCESO
    // =========================================================

    public bool ReservarProceso(
        int procesoId,
        int memoriaMB)
    {
        if (memoriaMB <= 0)
            return false;

        // Evitamos reservar dos veces el mismo PID.
        if (
            memoriaSolicitada.ContainsKey(
                procesoId))
        {
            return false;
        }

        // Ejemplo:
        //
        // memoria = 202 MB
        // unidad = 4 MB
        //
        // 202 / 4 = 50.5
        // necesitamos 51 casillas.
        int bloquesNecesarios =
            (int)Math.Ceiling(
                memoriaMB /
                (double)UnidadAsignacionMB
            );

        int inicio =
            BuscarHuecoContiguo(
                bloquesNecesarios
            );

        if (inicio < 0)
            return false;

        for (
            int i = inicio;
            i < inicio + bloquesNecesarios;
            i++)
        {
            mapaBits[i] = true;

            propietarios[i] =
                procesoId;
        }

        memoriaSolicitada[procesoId] =
            memoriaMB;

        return true;
    }

    // =========================================================
    // BUSCAR ESPACIO CONTIGUO
    // =========================================================

    private int BuscarHuecoContiguo(
        int bloquesNecesarios)
    {
        if (bloquesNecesarios <= 0)
            return -1;

        int consecutivos = 0;
        int inicio = 0;

        for (
            int i = 0;
            i < mapaBits.Length;
            i++)
        {
            if (!mapaBits[i])
            {
                if (consecutivos == 0)
                    inicio = i;

                consecutivos++;

                if (
                    consecutivos >=
                    bloquesNecesarios)
                {
                    return inicio;
                }
            }
            else
            {
                consecutivos = 0;
            }
        }

        return -1;
    }

    // =========================================================
    // LIBERAR UN PROCESO
    // =========================================================

    public bool LiberarProceso(
        int procesoId)
    {
        bool encontrado = false;

        for (
            int i = 0;
            i < propietarios.Length;
            i++)
        {
            if (
                propietarios[i] ==
                procesoId)
            {
                mapaBits[i] = false;

                propietarios[i] =
                    null;

                encontrado = true;
            }
        }

        if (encontrado)
        {
            memoriaSolicitada.Remove(
                procesoId
            );
        }

        return encontrado;
    }

    // =========================================================
    // LIBERAR TODA LA MEMORIA
    // =========================================================

    // Este método es necesario porque Kernel.cs
    // todavía lo utiliza al restaurar o reiniciar procesos.
    public void LiberarToda()
    {
        Array.Fill(
            mapaBits,
            false
        );

        Array.Fill<int?>(
            propietarios,
            null
        );

        memoriaSolicitada.Clear();
    }

    // =========================================================
    // CONSULTAR CASILLA
    // =========================================================

    public bool EstaOcupado(
        int indice)
    {
        if (
            indice < 0 ||
            indice >= mapaBits.Length)
        {
            return false;
        }

        return mapaBits[indice];
    }

    // =========================================================
    // OBTENER PROPIETARIO
    // =========================================================

    public int? ObtenerPropietarioBloque(
        int indice)
    {
        if (
            indice < 0 ||
            indice >= propietarios.Length)
        {
            return null;
        }

        return propietarios[indice];
    }

    // =========================================================
    // OBTENER CASILLAS DE UN PROCESO
    // =========================================================

    public List<int> ObtenerBloquesProceso(
        int procesoId)
    {
        var resultado =
            new List<int>();

        for (
            int i = 0;
            i < propietarios.Length;
            i++)
        {
            if (
                propietarios[i] ==
                procesoId)
            {
                resultado.Add(i);
            }
        }

        return resultado;
    }

    // =========================================================
    // MEMORIA ASIGNADA A UN PROCESO
    // =========================================================

    public int ObtenerMemoriaAsignadaProceso(
        int procesoId)
    {
        int cantidadCasillas =
            ObtenerBloquesProceso(
                procesoId
            ).Count;

        return
            cantidadCasillas *
            UnidadAsignacionMB;
    }

    // ---------------------------------------------------------
    // Alias de compatibilidad.
    //
    // Algunas partes anteriores del proyecto utilizaban
    // ObtenerMemoriaAsignadaMB().
    // ---------------------------------------------------------

    public int ObtenerMemoriaAsignadaMB(
        int procesoId)
    {
        return ObtenerMemoriaAsignadaProceso(
            procesoId
        );
    }

    // =========================================================
    // MEMORIA SOLICITADA
    // =========================================================

    public int ObtenerMemoriaSolicitadaProceso(
        int procesoId)
    {
        return memoriaSolicitada
            .TryGetValue(
                procesoId,
                out int valor)
            ? valor
            : 0;
    }

    // =========================================================
    // FRAGMENTACIÓN DE UN PROCESO
    // =========================================================

    public int ObtenerFragmentacionProceso(
        int procesoId)
    {
        int solicitada =
            ObtenerMemoriaSolicitadaProceso(
                procesoId
            );

        int asignada =
            ObtenerMemoriaAsignadaProceso(
                procesoId
            );

        return Math.Max(
            0,
            asignada - solicitada
        );
    }

    // =========================================================
    // REPRESENTACIÓN BINARIA
    // =========================================================

    public string ObtenerRepresentacionBinaria()
    {
        return string.Join(
            " ",
            mapaBits.Select(
                ocupado =>
                    ocupado
                        ? "1"
                        : "0"
            )
        );
    }

    // Alias para mantener compatibilidad con el
    // FrmMapaBits anterior.
    public string ObtenerMapaBitsTexto()
    {
        return ObtenerRepresentacionBinaria();
    }

    // =========================================================
    // COMPATIBILIDAD CON CÓDIGO ANTERIOR
    // =========================================================

    // Estos métodos se mantienen porque otras partes del
    // simulador pueden seguir haciendo reservas generales.

    public bool Reservar(
        int memoriaMB)
    {
        if (memoriaMB <= 0)
            return false;

        int bloquesNecesarios =
            (int)Math.Ceiling(
                memoriaMB /
                (double)UnidadAsignacionMB
            );

        int inicio =
            BuscarHuecoContiguo(
                bloquesNecesarios
            );

        if (inicio < 0)
            return false;

        for (
            int i = inicio;
            i < inicio + bloquesNecesarios;
            i++)
        {
            mapaBits[i] = true;
        }

        return true;
    }

    public void Liberar(
        int memoriaMB)
    {
        if (memoriaMB <= 0)
            return;

        if (memoriaMB >= UsadaMB)
        {
            LiberarToda();
            return;
        }

        int bloquesLiberar =
            (int)Math.Ceiling(
                memoriaMB /
                (double)UnidadAsignacionMB
            );

        for (
            int i = mapaBits.Length - 1;
            i >= 0 &&
            bloquesLiberar > 0;
            i--)
        {
            if (!mapaBits[i])
                continue;

            int? propietario =
                propietarios[i];

            mapaBits[i] =
                false;

            propietarios[i] =
                null;

            bloquesLiberar--;

            if (propietario.HasValue)
            {
                bool quedanBloques =
                    propietarios.Any(
                        p =>
                            p ==
                            propietario
                    );

                if (!quedanBloques)
                {
                    memoriaSolicitada.Remove(
                        propietario.Value
                    );
                }
            }
        }
    }
}