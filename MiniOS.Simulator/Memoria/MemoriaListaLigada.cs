using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOS.Simulator;

public enum AlgoritmoAsignacionMemoria
{
    FirstFit,
    NextFit,
    BestFit,
    WorstFit
}

public sealed class MemoriaListaLigada
{
    public int MemoriaTotalMB { get; }

    public LinkedList<BloqueListaLigada> Bloques { get; } = new();

    // Guarda el punto desde donde Next Fit continuará buscando.
    private LinkedListNode<BloqueListaLigada>? posicionNextFit;

    public MemoriaListaLigada(int memoriaTotalMB = 1024)
    {
        MemoriaTotalMB = memoriaTotalMB;

        Reiniciar();
    }

    // =========================================================
    // MÉTRICAS
    // =========================================================

    public int MemoriaUsadaMB =>
        Bloques
            .Where(b => !b.Libre)
            .Sum(b => b.TamanoMB);

    public int MemoriaLibreMB =>
        Bloques
            .Where(b => b.Libre)
            .Sum(b => b.TamanoMB);

    public int CantidadHuecosLibres =>
        Bloques.Count(b => b.Libre);

    // =========================================================
    // REINICIAR
    // =========================================================

    public void Reiniciar()
    {
        Bloques.Clear();

        Bloques.AddFirst(
            new BloqueListaLigada
            {
                InicioMB = 0,
                TamanoMB = MemoriaTotalMB,
                Libre = true
            }
        );

        posicionNextFit = Bloques.First;
    }

    // =========================================================
    // ASIGNACIÓN
    // =========================================================

    public bool AsignarProceso(
        int procesoId,
        string nombre,
        int tamanoMB,
        AlgoritmoAsignacionMemoria algoritmo)
    {
        if (tamanoMB <= 0)
            return false;

        if (tamanoMB > MemoriaLibreMB)
            return false;

        LinkedListNode<BloqueListaLigada>? elegido =
            algoritmo switch
            {
                AlgoritmoAsignacionMemoria.FirstFit =>
                    BuscarFirstFit(tamanoMB),

                AlgoritmoAsignacionMemoria.NextFit =>
                    BuscarNextFit(tamanoMB),

                AlgoritmoAsignacionMemoria.BestFit =>
                    BuscarBestFit(tamanoMB),

                AlgoritmoAsignacionMemoria.WorstFit =>
                    BuscarWorstFit(tamanoMB),

                _ => null
            };

        if (elegido is null)
            return false;

        AsignarEnNodo(
            elegido,
            procesoId,
            nombre,
            tamanoMB
        );

        // Next Fit continúa después del bloque asignado.
        if (algoritmo == AlgoritmoAsignacionMemoria.NextFit)
        {
            posicionNextFit =
                elegido.Next ??
                Bloques.First;
        }

        return true;
    }

    // =========================================================
    // FIRST FIT
    // =========================================================

    private LinkedListNode<BloqueListaLigada>? BuscarFirstFit(
        int tamanoMB)
    {
        var actual = Bloques.First;

        while (actual is not null)
        {
            if (actual.Value.Libre &&
                actual.Value.TamanoMB >= tamanoMB)
            {
                return actual;
            }

            actual = actual.Next;
        }

        return null;
    }

    // =========================================================
    // NEXT FIT
    // =========================================================

    private LinkedListNode<BloqueListaLigada>? BuscarNextFit(
        int tamanoMB)
    {
        if (Bloques.First is null)
            return null;

        var inicio =
            posicionNextFit ??
            Bloques.First;

        var actual = inicio;

        do
        {
            if (actual.Value.Libre &&
                actual.Value.TamanoMB >= tamanoMB)
            {
                return actual;
            }

            actual =
                actual.Next ??
                Bloques.First;

        } while (actual != inicio);

        return null;
    }

    // =========================================================
    // BEST FIT
    // =========================================================

    private LinkedListNode<BloqueListaLigada>? BuscarBestFit(
        int tamanoMB)
    {
        LinkedListNode<BloqueListaLigada>? mejor = null;

        var actual = Bloques.First;

        while (actual is not null)
        {
            if (actual.Value.Libre &&
                actual.Value.TamanoMB >= tamanoMB)
            {
                if (mejor is null ||
                    actual.Value.TamanoMB <
                    mejor.Value.TamanoMB)
                {
                    mejor = actual;
                }
            }

            actual = actual.Next;
        }

        return mejor;
    }

    // =========================================================
    // WORST FIT
    // =========================================================

    private LinkedListNode<BloqueListaLigada>? BuscarWorstFit(
        int tamanoMB)
    {
        LinkedListNode<BloqueListaLigada>? peor = null;

        var actual = Bloques.First;

        while (actual is not null)
        {
            if (actual.Value.Libre &&
                actual.Value.TamanoMB >= tamanoMB)
            {
                if (peor is null ||
                    actual.Value.TamanoMB >
                    peor.Value.TamanoMB)
                {
                    peor = actual;
                }
            }

            actual = actual.Next;
        }

        return peor;
    }

    // =========================================================
    // DIVIDIR BLOQUE Y ASIGNAR
    // =========================================================

    private void AsignarEnNodo(
        LinkedListNode<BloqueListaLigada> nodo,
        int procesoId,
        string nombre,
        int tamanoMB)
    {
        int tamanoOriginal =
            nodo.Value.TamanoMB;

        int inicioOriginal =
            nodo.Value.InicioMB;

        nodo.Value.TamanoMB = tamanoMB;
        nodo.Value.Libre = false;
        nodo.Value.ProcesoId = procesoId;
        nodo.Value.NombreProceso = nombre;

        int sobrante =
            tamanoOriginal - tamanoMB;

        if (sobrante > 0)
        {
            var bloqueLibre =
                new BloqueListaLigada
                {
                    InicioMB =
                        inicioOriginal + tamanoMB,

                    TamanoMB =
                        sobrante,

                    Libre = true
                };

            Bloques.AddAfter(
                nodo,
                bloqueLibre
            );
        }
    }

    // =========================================================
    // LIBERAR PROCESO
    // =========================================================

    public bool LiberarProceso(int procesoId)
    {
        var actual = Bloques.First;

        while (actual is not null)
        {
            if (!actual.Value.Libre &&
                actual.Value.ProcesoId == procesoId)
            {
                actual.Value.Libre = true;

                actual.Value.ProcesoId = null;

                actual.Value.NombreProceso =
                    string.Empty;

                // IMPORTANTE:
                // NO se fusionan bloques libres vecinos.
                // Cada bloque permanece independiente.

                return true;
            }

            actual = actual.Next;
        }

        return false;
    }
}
