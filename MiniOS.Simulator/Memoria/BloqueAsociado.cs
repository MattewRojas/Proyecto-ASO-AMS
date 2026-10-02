using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOS.Simulator;

public sealed class BloqueAsociado
{
    public int InicioMB { get; set; }

    public int TamanoMB { get; set; }

    // Tamaño real solicitado por el proceso.
    public int MemoriaSolicitadaMB { get; set; }

    public int? ProcesoId { get; set; }

    public string NombreProceso { get; set; } = string.Empty;

    // Hijos producidos al dividir el bloque.
    public BloqueAsociado? Izquierdo { get; set; }

    public BloqueAsociado? Derecho { get; set; }

    public bool EstaDividido =>
        Izquierdo is not null ||
        Derecho is not null;

    public bool EstaOcupado =>
        ProcesoId.HasValue;

    public bool EstaLibre =>
        !EstaDividido &&
        !EstaOcupado;

    public int FragmentacionInternaMB =>
        EstaOcupado
            ? TamanoMB - MemoriaSolicitadaMB
            : 0;
}