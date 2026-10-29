namespace MiniOS.Simulator;

public enum MetodoAsignacion { Contigua, Ligada }

// Modelo didáctico: un bloque guarda propietario y, en asignación ligada, el enlace.
// No accede al sistema de archivos del equipo.
public sealed class DiscoAsignacion
{
    public sealed class Entrada
    {
        public string Nombre { get; init; } = "";
        public int Inicio { get; internal set; }
        public int Cantidad { get; init; }
    }

    private readonly string?[] propietarios = new string?[20];
    private readonly int?[] siguientes = new int?[20];
    private readonly Dictionary<string, Entrada> archivos = new(StringComparer.OrdinalIgnoreCase);
    public MetodoAsignacion Metodo { get; }
    public int Total => propietarios.Length;
    public int Libres => propietarios.Count(p => p == null);
    public IEnumerable<Entrada> Archivos => archivos.Values.OrderBy(a => a.Inicio);
    public string? Propietario(int bloque) => propietarios[bloque];
    public int? Siguiente(int bloque) => siguientes[bloque];
    public int MayorHueco
    {
        get
        {
            int mayor = 0, actual = 0;
            foreach (var p in propietarios)
            {
                actual = p == null ? actual + 1 : 0;
                mayor = Math.Max(mayor, actual);
            }
            return mayor;
        }
    }

    public DiscoAsignacion(MetodoAsignacion metodo) => Metodo = metodo;

    public Entrada Consultar(string nombre) => archivos.TryGetValue(nombre.Trim(), out var a)
        ? a : throw new InvalidOperationException("No existe el archivo indicado.");

    public Entrada Crear(string nombre, int cantidad)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0 || nombre is "." or ".." ||
            nombre.Any(c => c < 32 || "<>:\"/\\|?*".Contains(c)))
            throw new InvalidOperationException("Escriba un nombre de archivo válido.");
        if (archivos.ContainsKey(nombre)) throw new InvalidOperationException("Ya existe un archivo con ese nombre.");
        if (cantidad < 1 || cantidad > Total) throw new InvalidOperationException("El tamaño debe estar entre 1 y 20 bloques.");
        if (cantidad > Libres) throw new InvalidOperationException($"Espacio insuficiente: necesita {cantidad} bloques y hay {Libres} libres.");

        int[] bloques;
        if (Metodo == MetodoAsignacion.Contigua)
        {
            int inicio = -1, consecutivos = 0;
            for (int i = 0; i < Total; i++)
            {
                consecutivos = propietarios[i] == null ? consecutivos + 1 : 0;
                if (consecutivos == cantidad) { inicio = i - cantidad + 1; break; }
            }
            if (inicio < 0) throw new InvalidOperationException(
                $"Fragmentación externa: hay {Libres} bloques libres, pero el mayor hueco es de {MayorHueco}. Compacte o solicite menos bloques.");
            bloques = Enumerable.Range(inicio, cantidad).ToArray();
        }
        else bloques = Enumerable.Range(0, Total).Where(i => propietarios[i] == null).Take(cantidad).ToArray();

        var archivo = new Entrada { Nombre = nombre, Inicio = bloques[0], Cantidad = cantidad };
        archivos.Add(nombre, archivo);
        for (int i = 0; i < bloques.Length; i++)
        {
            propietarios[bloques[i]] = nombre;
            siguientes[bloques[i]] = Metodo == MetodoAsignacion.Ligada && i + 1 < bloques.Length ? bloques[i + 1] : null;
        }
        return archivo;
    }

    public IReadOnlyList<int> Recorrer(Entrada archivo)
    {
        if (Metodo == MetodoAsignacion.Contigua)
            return Enumerable.Range(archivo.Inicio, archivo.Cantidad).ToArray();
        var ruta = new List<int>();
        int? actual = archivo.Inicio;
        while (actual.HasValue)
        {
            int bloque = actual.Value;
            if (bloque < 0 || bloque >= Total || ruta.Contains(bloque) ||
                !string.Equals(propietarios[bloque], archivo.Nombre, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cadena de bloques inválida.");
            ruta.Add(bloque);
            actual = siguientes[bloque];
        }
        if (ruta.Count != archivo.Cantidad) throw new InvalidOperationException("Cadena incompleta.");
        return ruta;
    }

    public void Eliminar(string nombre)
    {
        var archivo = Consultar(nombre);
        var bloques = Recorrer(archivo); // Leer la cadena antes de borrar sus enlaces.
        foreach (int bloque in bloques) { propietarios[bloque] = null; siguientes[bloque] = null; }
        archivos.Remove(archivo.Nombre);
    }

    public void Compactar()
    {
        if (Metodo != MetodoAsignacion.Contigua) throw new InvalidOperationException("La compactación solo se aplica a esta simulación contigua.");
        var orden = Archivos.ToArray();
        Array.Clear(propietarios);
        int posicion = 0;
        foreach (var archivo in orden)
        {
            archivo.Inicio = posicion;
            for (int i = 0; i < archivo.Cantidad; i++) propietarios[posicion++] = archivo.Nombre;
        }
    }

    public void Reiniciar() { archivos.Clear(); Array.Clear(propietarios); Array.Clear(siguientes); }
}
