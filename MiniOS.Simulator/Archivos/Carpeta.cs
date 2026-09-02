namespace MiniOS.Simulator;

public sealed class Carpeta
{
    public string Nombre { get; init; } = "Carpeta";
    public List<Archivo> Archivos { get; } = [];
    public List<Carpeta> Subcarpetas { get; } = [];
}
