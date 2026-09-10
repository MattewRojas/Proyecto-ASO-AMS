using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniOS.Simulator;

public sealed class Memoria
{
    // =========================================================
    // CONFIGURACIÓN GENERAL
    // =========================================================


    public int TotalMB { get; } = 4096;

    // Cada bloque de memoria representa 64 MB.
    public int TamanoBloqueMB { get; } = 64;

    // 4096 / 64 = 64 bloques.
    public int TotalBloques => TotalMB / TamanoBloqueMB;

    // =========================================================
    // MAPA DE BITS
    // =========================================================

    private readonly bool[] mapaBits;


    private readonly int?[] propietarioBloque;


    private readonly Dictionary<int, int> memoriaSolicitadaProcesos = new();

    public Memoria()
    {
        mapaBits = new bool[TotalBloques];
        propietarioBloque = new int?[TotalBloques];
    }

    // =========================================================
    // INFORMACIÓN GENERAL
    // =========================================================

    public int UsadaMB =>
        mapaBits.Count(bloque => bloque) * TamanoBloqueMB;

    public int DisponibleMB =>
        TotalMB - UsadaMB;

    public int Porcentaje =>
        TotalMB == 0
            ? 0
            : UsadaMB * 100 / TotalMB;

    public int BloquesOcupados =>
        mapaBits.Count(bloque => bloque);

    public int BloquesLibres =>
        mapaBits.Count(bloque => !bloque);


    public int MemoriaSolicitadaTotalMB =>
        memoriaSolicitadaProcesos.Values.Sum();

    public int FragmentacionInternaMB =>
        Math.Max(0, UsadaMB - MemoriaSolicitadaTotalMB);

    // =========================================================
    // CALCULAR BLOQUES NECESARIOS
    // =========================================================

    public int CalcularBloquesNecesarios(int memoriaMB)
    {
        if (memoriaMB <= 0)
            return 0;

        return (int)Math.Ceiling(
            (double)memoriaMB / TamanoBloqueMB
        );
    }

    // =========================================================
    // RESERVAR MEMORIA PARA UN PROCESO
    // =========================================================

    public bool ReservarProceso(int procesoId, int memoriaMB)
    {
        return ReservarProceso(
            procesoId,
            memoriaMB,
            out _
        );
    }

    public bool ReservarProceso(
        int procesoId,
        int memoriaMB,
        out List<int> bloquesAsignados)
    {
        bloquesAsignados = new List<int>();

        if (procesoId <= 0 || memoriaMB <= 0)
            return false;


        if (propietarioBloque.Any(p => p == procesoId))
            return false;

        int bloquesNecesarios =
            CalcularBloquesNecesarios(memoriaMB);

        if (bloquesNecesarios <= 0)
            return false;

        if (bloquesNecesarios > BloquesLibres)
            return false;

        // Buscamos bloques consecutivos.
        int posicionInicial =
            BuscarBloquesContiguos(bloquesNecesarios);

        if (posicionInicial == -1)
            return false;

        for (
            int i = posicionInicial;
            i < posicionInicial + bloquesNecesarios;
            i++)
        {
            mapaBits[i] = true;
            propietarioBloque[i] = procesoId;

            bloquesAsignados.Add(i);
        }

        memoriaSolicitadaProcesos[procesoId] = memoriaMB;

        return true;
    }

    // =========================================================
    // COMPATIBILIDAD CON EL MÉTODO ANTERIOR
    // =========================================================


    public bool Reservar(int mb)
    {
        if (mb <= 0)
            return false;

        int bloquesNecesarios =
            CalcularBloquesNecesarios(mb);

        int posicionInicial =
            BuscarBloquesContiguos(bloquesNecesarios);

        if (posicionInicial == -1)
            return false;

        for (
            int i = posicionInicial;
            i < posicionInicial + bloquesNecesarios;
            i++)
        {
            mapaBits[i] = true;


            propietarioBloque[i] = null;
        }

        return true;
    }

    // =========================================================
    // BUSCAR BLOQUES CONSECUTIVOS
    // =========================================================

    private int BuscarBloquesContiguos(
        int bloquesNecesarios)
    {
        int consecutivos = 0;
        int inicio = -1;

        for (int i = 0; i < mapaBits.Length; i++)
        {
            if (!mapaBits[i])
            {
                if (consecutivos == 0)
                    inicio = i;

                consecutivos++;

                if (consecutivos == bloquesNecesarios)
                    return inicio;
            }
            else
            {
                consecutivos = 0;
                inicio = -1;
            }
        }

        return -1;
    }

    // =========================================================
    // LIBERAR MEMORIA DE UN PROCESO
    // =========================================================

    public bool LiberarProceso(int procesoId)
    {
        bool encontrado = false;

        for (int i = 0; i < propietarioBloque.Length; i++)
        {
            if (propietarioBloque[i] == procesoId)
            {
                mapaBits[i] = false;
                propietarioBloque[i] = null;

                encontrado = true;
            }
        }

        memoriaSolicitadaProcesos.Remove(procesoId);

        return encontrado;
    }

    // =========================================================
    // MÉTODO ANTIGUO LIBERAR
    // =========================================================

  
    public void Liberar(int mb)
    {
        if (mb <= 0)
            return;

        if (mb >= UsadaMB)
        {
            LiberarToda();
            return;
        }

        int bloquesALiberar =
            CalcularBloquesNecesarios(mb);

        for (
            int i = mapaBits.Length - 1;
            i >= 0 && bloquesALiberar > 0;
            i--)
        {
            if (!mapaBits[i])
                continue;

            int? propietario = propietarioBloque[i];

            mapaBits[i] = false;
            propietarioBloque[i] = null;

            bloquesALiberar--;

            if (propietario.HasValue &&
                !propietarioBloque.Any(
                    p => p == propietario.Value))
            {
                memoriaSolicitadaProcesos.Remove(
                    propietario.Value
                );
            }
        }
    }

    // =========================================================
    // LIBERAR TODA LA MEMORIA
    // =========================================================

    public void LiberarToda()
    {
        for (int i = 0; i < mapaBits.Length; i++)
        {
            mapaBits[i] = false;
            propietarioBloque[i] = null;
        }

        memoriaSolicitadaProcesos.Clear();
    }

    // =========================================================
    // CONSULTAR ESTADO DE UN BLOQUE
    // =========================================================

    public bool EstaOcupado(int numeroBloque)
    {
        ValidarNumeroBloque(numeroBloque);

        return mapaBits[numeroBloque];
    }

    // =========================================================
    // CONSULTAR PROPIETARIO
    // =========================================================

    public int? ObtenerPropietarioBloque(
        int numeroBloque)
    {
        ValidarNumeroBloque(numeroBloque);

        return propietarioBloque[numeroBloque];
    }

    // =========================================================
    // OBTENER BLOQUES DE UN PROCESO
    // =========================================================

    public List<int> ObtenerBloquesProceso(
        int procesoId)
    {
        var bloques = new List<int>();

        for (int i = 0; i < propietarioBloque.Length; i++)
        {
            if (propietarioBloque[i] == procesoId)
                bloques.Add(i);
        }

        return bloques;
    }

    // =========================================================
    // REPRESENTACIÓN DEL BITMAP
    // =========================================================

    public string ObtenerMapaBitsTexto()
    {
        return string.Join(
            " ",
            mapaBits.Select(
                ocupado => ocupado ? "1" : "0"
            )
        );
    }

    public bool[] ObtenerMapaBits()
    {
        return (bool[])mapaBits.Clone();
    }

    // =========================================================
    // MEMORIA ASIGNADA A UN PROCESO
    // =========================================================

    public int ObtenerMemoriaAsignadaMB(
        int procesoId)
    {
        int bloques =
            propietarioBloque.Count(
                p => p == procesoId
            );

        return bloques * TamanoBloqueMB;
    }

    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private void ValidarNumeroBloque(
        int numeroBloque)
    {
        if (
            numeroBloque < 0 ||
            numeroBloque >= TotalBloques)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numeroBloque),
                $"El bloque debe estar entre 0 y {TotalBloques - 1}."
            );
        }
    }
}