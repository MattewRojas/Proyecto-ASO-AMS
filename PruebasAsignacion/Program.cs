using MiniOS.Simulator;

int comprobaciones = 0;
void Verificar(bool ok, string caso)
{
    if (!ok) throw new Exception(caso);
    comprobaciones++;
}
void Rechaza(Action accion, string caso)
{
    try { accion(); } catch (InvalidOperationException) { comprobaciones++; return; }
    throw new Exception(caso);
}
void Integridad(DiscoAsignacion d)
{
    var usados = new HashSet<int>();
    foreach (var a in d.Archivos)
    {
        var ruta = d.Recorrer(a);
        Verificar(ruta.Count == a.Cantidad, "Longitud");
        foreach (int b in ruta) Verificar(usados.Add(b) && d.Propietario(b) == a.Nombre, "Propiedad única");
        if (d.Metodo == MetodoAsignacion.Ligada) Verificar(d.Siguiente(ruta[^1]) == null, "FIN");
    }
    Verificar(usados.Count + d.Libres == 20, "Conservación de bloques");
}
foreach (var metodo in Enum.GetValues<MetodoAsignacion>())
{
    var d = new DiscoAsignacion(metodo);
    Rechaza(() => d.Crear(" ", 1), "Nombre vacío");
    Rechaza(() => d.Crear("x/y", 1), "Nombre inválido");
    Rechaza(() => d.Crear("a", 0), "Tamaño cero");
    Rechaza(() => d.Crear("a", 21), "Tamaño excesivo");
    foreach (string n in new[] { "A", "B", "C", "D", "E" }) d.Crear(n, 4);
    Rechaza(() => d.Crear("a", 1), "Duplicado sin distinguir mayúsculas");
    Rechaza(() => d.Crear("F", 1), "Disco lleno");
    Verificar(d.Consultar(" a ").Nombre == "A", "Consulta Trim");
    d.Eliminar("B"); d.Eliminar("D");
    Verificar(d.Libres == 8 && d.MayorHueco == 4, "Fragmentación");
    if (metodo == MetodoAsignacion.Contigua)
    {
        Rechaza(() => d.Crear("F", 6), "Rechazo por fragmentación");
        Verificar(d.Libres == 8 && d.Archivos.Count() == 3, "Fallo atómico");
        d.Compactar();
        Verificar(d.Libres == 8 && d.MayorHueco == 8 && d.Consultar("C").Inicio == 4, "Compactación");
        var f = d.Crear("F", 6);
        Verificar(d.Recorrer(f).SequenceEqual(Enumerable.Range(12, 6)), "Bloques contiguos");
    }
    else
    {
        var f = d.Crear("F", 6);
        Verificar(d.Recorrer(f).SequenceEqual(new[] { 4, 5, 6, 7, 12, 13 }), "Cadena dispersa");
        Verificar(d.Siguiente(7) == 12, "Enlace entre huecos");
        Rechaza(d.Compactar, "Compactación no ligada");
    }
    Integridad(d);
    d.Eliminar("F");
    Verificar(d.Libres == 8 && d.Siguiente(7) == null, "Liberar bloques y enlaces");
    Rechaza(() => d.Eliminar("F"), "Eliminar inexistente");
    d.Reiniciar(); d.Crear("Completo", 20); Integridad(d); d.Eliminar("Completo");
    Verificar(d.Libres == 20, "Liberación completa");
    var random = new Random(42);
    for (int i = 0; i < 300; i++)
    {
        if (d.Archivos.Any() && random.Next(3) == 0) d.Eliminar(d.Archivos.First().Nombre);
        else { try { d.Crear("Archivo" + i, random.Next(1, 9)); } catch (InvalidOperationException) { } }
        if (metodo == MetodoAsignacion.Contigua && i % 10 == 0) d.Compactar();
        Integridad(d);
    }
}
Console.WriteLine($"Correcto: {comprobaciones} comprobaciones de asignación.");
