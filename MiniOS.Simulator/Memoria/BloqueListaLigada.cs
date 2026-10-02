using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOS.Simulator;

public sealed class BloqueListaLigada
{
    public int InicioMB { get; set; }

    public int TamanoMB { get; set; }

    public bool Libre { get; set; } = true;

    public int? ProcesoId { get; set; }

    public string NombreProceso { get; set; } = string.Empty;

    public int FinMB => InicioMB + TamanoMB;

    public override string ToString()
    {
        if (Libre)
            return $"Libre - {TamanoMB} MB";

        return $"P{ProcesoId:00} - {NombreProceso} - {TamanoMB} MB";
    }
}
