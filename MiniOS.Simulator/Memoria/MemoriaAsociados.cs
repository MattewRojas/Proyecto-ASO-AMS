using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOS.Simulator;

public sealed class MemoriaAsociados
{
    public int MemoriaTotalMB { get; }

    public BloqueAsociado Raiz { get; private set; }

    public MemoriaAsociados(int memoriaTotalMB = 1024)
    {
        if (!EsPotenciaDeDos(memoriaTotalMB))
        {
            throw new ArgumentException(
                "La memoria total debe ser una potencia de 2."
            );
        }

        MemoriaTotalMB = memoriaTotalMB;

        Raiz = CrearBloqueInicial();
    }

    // =========================================================
    // MÉTRICAS
    // =========================================================

    public int MemoriaAsignadaMB =>
        ObtenerBloquesOcupados()
            .Sum(b => b.TamanoMB);

    public int MemoriaSolicitadaMB =>
        ObtenerBloquesOcupados()
            .Sum(b => b.MemoriaSolicitadaMB);

    public int MemoriaLibreMB =>
        MemoriaTotalMB - MemoriaAsignadaMB;

    public int FragmentacionInternaMB =>
        ObtenerBloquesOcupados()
            .Sum(b => b.FragmentacionInternaMB);

    // =========================================================
    // REINICIAR
    // =========================================================

    public void Reiniciar()
    {
        Raiz = CrearBloqueInicial();
    }

    private BloqueAsociado CrearBloqueInicial()
    {
        return new BloqueAsociado
        {
            InicioMB = 0,
            TamanoMB = MemoriaTotalMB
        };
    }

    // =========================================================
    // ASIGNAR PROCESO
    // =========================================================

    public BloqueAsociado? AsignarProceso(
        int procesoId,
        string nombre,
        int memoriaSolicitadaMB)
    {
        if (memoriaSolicitadaMB <= 0)
            return null;

        if (memoriaSolicitadaMB > MemoriaTotalMB)
            return null;

        // Busca el bloque potencia de 2 más pequeño
        // capaz de contener el proceso.
        int tamanoNecesario =
            ObtenerSiguientePotenciaDeDos(
                memoriaSolicitadaMB
            );

        return AsignarRecursivo(
            Raiz,
            procesoId,
            nombre,
            memoriaSolicitadaMB,
            tamanoNecesario
        );
    }

    private BloqueAsociado? AsignarRecursivo(
        BloqueAsociado bloque,
        int procesoId,
        string nombre,
        int memoriaSolicitadaMB,
        int tamanoNecesario)
    {
        // Si ya está ocupado, aquí no podemos asignar.
        if (bloque.EstaOcupado)
            return null;

        // Si está dividido, buscamos primero
        // por la izquierda y después por la derecha.
        if (bloque.EstaDividido)
        {
            if (bloque.Izquierdo is not null)
            {
                var resultadoIzquierdo =
                    AsignarRecursivo(
                        bloque.Izquierdo,
                        procesoId,
                        nombre,
                        memoriaSolicitadaMB,
                        tamanoNecesario
                    );

                if (resultadoIzquierdo is not null)
                    return resultadoIzquierdo;
            }

            if (bloque.Derecho is not null)
            {
                return AsignarRecursivo(
                    bloque.Derecho,
                    procesoId,
                    nombre,
                    memoriaSolicitadaMB,
                    tamanoNecesario
                );
            }

            return null;
        }

        // El bloque es más pequeño de lo necesario.
        if (bloque.TamanoMB < tamanoNecesario)
            return null;

        // Si tiene exactamente el tamaño requerido,
        // se asigna al proceso.
        if (bloque.TamanoMB == tamanoNecesario)
        {
            bloque.ProcesoId = procesoId;
            bloque.NombreProceso = nombre;

            bloque.MemoriaSolicitadaMB =
                memoriaSolicitadaMB;

            return bloque;
        }

        // Todavía es demasiado grande.
        // Se divide exactamente por la mitad.
        DividirBloque(bloque);

        // Después de dividir intentamos asignar
        // primero en el asociado izquierdo.
        if (bloque.Izquierdo is not null)
        {
            var resultado =
                AsignarRecursivo(
                    bloque.Izquierdo,
                    procesoId,
                    nombre,
                    memoriaSolicitadaMB,
                    tamanoNecesario
                );

            if (resultado is not null)
                return resultado;
        }

        if (bloque.Derecho is not null)
        {
            return AsignarRecursivo(
                bloque.Derecho,
                procesoId,
                nombre,
                memoriaSolicitadaMB,
                tamanoNecesario
            );
        }

        return null;
    }

    // =========================================================
    // DIVIDIR BLOQUE
    // =========================================================

    private static void DividirBloque(
        BloqueAsociado bloque)
    {
        int mitad =
            bloque.TamanoMB / 2;

        bloque.Izquierdo =
            new BloqueAsociado
            {
                InicioMB =
                    bloque.InicioMB,

                TamanoMB =
                    mitad
            };

        bloque.Derecho =
            new BloqueAsociado
            {
                InicioMB =
                    bloque.InicioMB + mitad,

                TamanoMB =
                    mitad
            };
    }

    // =========================================================
    // LIBERAR PROCESO
    // =========================================================

    public bool LiberarProceso(
        int procesoId)
    {
        bool liberado =
            LiberarRecursivo(
                Raiz,
                procesoId
            );

        if (liberado)
        {
            // Después de liberar, comprobamos si
            // bloques asociados pueden volver a unirse.
            FusionarAsociados(Raiz);
        }

        return liberado;
    }

    private static bool LiberarRecursivo(
        BloqueAsociado bloque,
        int procesoId)
    {
        if (bloque.ProcesoId == procesoId)
        {
            bloque.ProcesoId = null;

            bloque.NombreProceso =
                string.Empty;

            bloque.MemoriaSolicitadaMB = 0;

            return true;
        }

        if (bloque.Izquierdo is not null &&
            LiberarRecursivo(
                bloque.Izquierdo,
                procesoId))
        {
            return true;
        }

        if (bloque.Derecho is not null &&
            LiberarRecursivo(
                bloque.Derecho,
                procesoId))
        {
            return true;
        }

        return false;
    }

    // =========================================================
    // FUSIÓN DE BLOQUES ASOCIADOS
    // =========================================================

    private static void FusionarAsociados(
        BloqueAsociado bloque)
    {
        if (!bloque.EstaDividido)
            return;

        if (bloque.Izquierdo is not null)
        {
            FusionarAsociados(
                bloque.Izquierdo
            );
        }

        if (bloque.Derecho is not null)
        {
            FusionarAsociados(
                bloque.Derecho
            );
        }

        if (bloque.Izquierdo is null ||
            bloque.Derecho is null)
        {
            return;
        }

        bool izquierdoLibre =
            bloque.Izquierdo.EstaLibre;

        bool derechoLibre =
            bloque.Derecho.EstaLibre;

        // SOLO si ambos buddies están libres
        // se vuelven a unir.
        if (izquierdoLibre &&
            derechoLibre)
        {
            bloque.Izquierdo = null;
            bloque.Derecho = null;
        }
    }

    // =========================================================
    // OBTENER BLOQUES
    // =========================================================

    public List<BloqueAsociado>
        ObtenerBloquesFinales()
    {
        var resultado =
            new List<BloqueAsociado>();

        ObtenerHojas(
            Raiz,
            resultado
        );

        return resultado;
    }

    private static void ObtenerHojas(
        BloqueAsociado bloque,
        List<BloqueAsociado> resultado)
    {
        if (!bloque.EstaDividido)
        {
            resultado.Add(bloque);
            return;
        }

        if (bloque.Izquierdo is not null)
        {
            ObtenerHojas(
                bloque.Izquierdo,
                resultado
            );
        }

        if (bloque.Derecho is not null)
        {
            ObtenerHojas(
                bloque.Derecho,
                resultado
            );
        }
    }

    public IEnumerable<BloqueAsociado>
        ObtenerBloquesOcupados()
    {
        return ObtenerBloquesFinales()
            .Where(
                bloque =>
                    bloque.EstaOcupado
            );
    }

    // =========================================================
    // POTENCIAS DE DOS
    // =========================================================

    private static int
        ObtenerSiguientePotenciaDeDos(
            int numero)
    {
        int potencia = 1;

        while (potencia < numero)
        {
            potencia *= 2;
        }

        return potencia;
    }

    private static bool
        EsPotenciaDeDos(
            int numero)
    {
        return numero > 0 &&
               (numero & (numero - 1)) == 0;
    }
}