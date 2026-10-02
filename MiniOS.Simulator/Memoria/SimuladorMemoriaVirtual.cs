using System.Globalization;
using System.Text.RegularExpressions;

namespace MiniOS.Simulator;

public enum AlgoritmoReemplazo { OPT, NRU, FIFO, LRU }

public sealed record ReferenciaPagina(int Pagina, bool Escritura = false)
{
    public override string ToString() => Pagina.ToString(CultureInfo.InvariantCulture) + (Escritura ? "*" : "");
}

public sealed record EstadoMarco(int? Pagina, bool R, bool M)
{
    public int Clase => (R ? 2 : 0) + (M ? 1 : 0);
}

public sealed record PasoMemoriaVirtual(
    int Tiempo, ReferenciaPagina Referencia, IReadOnlyList<EstadoMarco> Marcos,
    bool Fallo, int MarcoAccedido, int? PaginaReemplazada, int? ClaseVictima,
    bool LimpiezaR, int FallosAcumulados, string Explicacion);

/// <summary>Simulación didáctica independiente de la asignación física del kernel.</summary>
public sealed class SimuladorMemoriaVirtual
{
    public const int MaxReferencias = 200;
    public const int MaxMarcos = 16;
    public const int MaxPagina = 999999;
    private readonly ReferenciaPagina[] referencias;
    private readonly int?[] paginas;
    private readonly bool[] r;
    private readonly bool[] m;
    private readonly int[] entrada;
    private readonly int[] ultimoUso;
    private readonly List<PasoMemoriaVirtual> pasos = [];

    public AlgoritmoReemplazo Algoritmo { get; }
    public int IntervaloLimpiezaR { get; }
    public int CantidadMarcos => paginas.Length;
    public int TotalReferencias => referencias.Length;
    public int Procesadas => pasos.Count;
    public int Fallos { get; private set; }
    public int Aciertos => Procesadas - Fallos;
    public int Reemplazos => pasos.Count(p => p.PaginaReemplazada.HasValue);
    public double FrecuenciaFallos => Procesadas == 0 ? 0 : (double)Fallos / Procesadas;
    public double Rendimiento => Procesadas == 0 ? 0 : (double)Aciertos / Procesadas;
    public bool Terminado => Procesadas == TotalReferencias;
    public IReadOnlyList<PasoMemoriaVirtual> Pasos => pasos.AsReadOnly();

    public SimuladorMemoriaVirtual(IEnumerable<ReferenciaPagina> referencias,
        int cantidadMarcos, AlgoritmoReemplazo algoritmo, int intervaloLimpiezaR = 4)
    {
        ArgumentNullException.ThrowIfNull(referencias);
        this.referencias = referencias.Take(MaxReferencias + 1).ToArray();
        if (this.referencias.Length is < 1 or > MaxReferencias ||
            this.referencias.Any(x => x is null || x.Pagina is < 0 or > MaxPagina))
            throw new ArgumentException($"Ingrese de 1 a {MaxReferencias} referencias con páginas entre 0 y {MaxPagina}.");
        if (cantidadMarcos is < 1 or > MaxMarcos)
            throw new ArgumentOutOfRangeException(nameof(cantidadMarcos));
        if (!Enum.IsDefined(algoritmo)) throw new ArgumentOutOfRangeException(nameof(algoritmo));
        if (intervaloLimpiezaR is < 0 or > MaxReferencias)
            throw new ArgumentOutOfRangeException(nameof(intervaloLimpiezaR));
        Algoritmo = algoritmo;
        IntervaloLimpiezaR = intervaloLimpiezaR;
        paginas = new int?[cantidadMarcos];
        r = new bool[cantidadMarcos];
        m = new bool[cantidadMarcos];
        entrada = new int[cantidadMarcos];
        ultimoUso = new int[cantidadMarcos];
    }

    public static bool IntentarLeerReferencias(string texto,
        out ReferenciaPagina[] resultado, out string error)
    {
        resultado = [];
        error = "";
        var tokens = Regex.Split(texto.Trim(), @"[\s,;]+");
        if (string.IsNullOrWhiteSpace(texto) || tokens.Length > MaxReferencias)
        {
            error = $"Ingrese de 1 a {MaxReferencias} páginas separadas por comas o espacios.";
            return false;
        }
        var lista = new List<ReferenciaPagina>();
        foreach (var token in tokens)
        {
            bool escritura = token.EndsWith('*');
            string numero = escritura ? token[..^1] : token;
            if (!int.TryParse(numero, NumberStyles.None, CultureInfo.InvariantCulture, out int pagina) ||
                pagina < 0 || pagina > MaxPagina)
            {
                error = $"Referencia inválida: «{token}». Use páginas de 0 a {MaxPagina}; añada * para una escritura, por ejemplo 5*.";
                return false;
            }
            lista.Add(new ReferenciaPagina(pagina, escritura));
        }
        resultado = lista.ToArray();
        return true;
    }

    public static bool IntentarLeerReferencias(string texto, AlgoritmoReemplazo algoritmo,
        out ReferenciaPagina[] resultado, out string error)
    {
        resultado = [];
        if (algoritmo != AlgoritmoReemplazo.NRU && texto.Contains('*'))
        {
            error = $"{algoritmo} utiliza solo números de página. Quite los asteriscos; la escritura (*) se configura únicamente en NRU.";
            return false;
        }
        return IntentarLeerReferencias(texto, out resultado, out error);
    }

    public PasoMemoriaVirtual? Avanzar()
    {
        if (Terminado) return null;
        int indice = Procesadas;
        var referencia = referencias[indice];
        int marco = Array.IndexOf(paginas, (int?)referencia.Pagina);
        bool fallo = marco < 0;
        int? victima = null;
        int? claseVictima = null;
        string motivo;

        if (fallo)
        {
            Fallos++;
            marco = Array.IndexOf(paginas, null);
            if (marco >= 0)
                motivo = $"Fallo: la página {referencia.Pagina} se carga en el marco {marco + 1}, que estaba vacío.";
            else
            {
                marco = ElegirVictima(indice);
                victima = paginas[marco];
                claseVictima = Algoritmo == AlgoritmoReemplazo.NRU ? Clase(marco) : null;
                string criterio = Algoritmo switch
                {
                    AlgoritmoReemplazo.FIFO => $"entró en el paso {entrada[marco]} y es la más antigua; los aciertos no cambian el orden FIFO",
                    AlgoritmoReemplazo.LRU => $"su último uso fue en el paso {ultimoUso[marco]}, el más lejano en el pasado",
                    AlgoritmoReemplazo.OPT => ProximoUso(marco, indice) == int.MaxValue
                        ? "no volverá a utilizarse; en empate se elige el primer marco"
                        : $"su próximo uso será en el paso {ProximoUso(marco, indice) + 1}, el más lejano en el futuro",
                    _ => $"pertenece a la clase {claseVictima} (R={(r[marco] ? 1 : 0)}, M={(m[marco] ? 1 : 0)}), la menor disponible; en empate se elige el primer marco"
                };
                motivo = $"Fallo: sale la página {victima} del marco {marco + 1} porque {criterio}. Entra la página {referencia.Pagina}.";
                if (m[marco]) motivo += " La página que sale estaba modificada: se simula su escritura a disco.";
            }
            paginas[marco] = referencia.Pagina;
            entrada[marco] = indice + 1;
            m[marco] = false;
        }
        else
            motivo = $"Acierto: la página {referencia.Pagina} ya está en el marco {marco + 1}; no se reemplaza ninguna página.";

        ultimoUso[marco] = indice + 1;
        r[marco] = true;
        m[marco] |= referencia.Escritura;
        if (referencia.Escritura) motivo += " Es una escritura: M queda en 1 hasta que esa página salga de memoria.";
        bool limpieza = Algoritmo == AlgoritmoReemplazo.NRU && IntervaloLimpiezaR > 0 &&
            (indice + 1) % IntervaloLimpiezaR == 0;
        if (limpieza)
        {
            Array.Clear(r);
            motivo += " Al terminar este paso se limpian todos los bits R; los bits M se conservan.";
        }

        var estado = Array.AsReadOnly(Enumerable.Range(0, CantidadMarcos)
            .Select(i => new EstadoMarco(paginas[i], r[i], m[i])).ToArray());
        var paso = new PasoMemoriaVirtual(indice + 1, referencia, estado, fallo, marco,
            victima, claseVictima, limpieza, Fallos, motivo);
        pasos.Add(paso);
        return paso;
    }

    public void EjecutarTodo() { while (!Terminado) Avanzar(); }

    private int Clase(int marco) => (r[marco] ? 2 : 0) + (m[marco] ? 1 : 0);

    private int ProximoUso(int marco, int actual)
    {
        for (int i = actual + 1; i < referencias.Length; i++)
            if (referencias[i].Pagina == paginas[marco]) return i;
        return int.MaxValue;
    }

    private int ElegirVictima(int actual)
    {
        int elegida = 0;
        for (int i = 1; i < CantidadMarcos; i++)
        {
            bool preferida = Algoritmo switch
            {
                AlgoritmoReemplazo.OPT => ProximoUso(i, actual) > ProximoUso(elegida, actual),
                AlgoritmoReemplazo.FIFO => entrada[i] < entrada[elegida],
                AlgoritmoReemplazo.LRU => ultimoUso[i] < ultimoUso[elegida],
                _ => Clase(i) < Clase(elegida)
            };
            if (preferida) elegida = i;
        }
        return elegida;
    }
}
