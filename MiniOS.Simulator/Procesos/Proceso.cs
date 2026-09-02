namespace MiniOS.Simulator;

public sealed class Proceso
{
    public int Id { get; init; }
    public string Nombre { get; init; } = "Proceso";
    public int MemoriaMB { get; init; }
    public EstadoProceso Estado { get; set; } = EstadoProceso.Listo;
    public override string ToString() => $"P{Id:00}  {Nombre}  ({MemoriaMB} MB) - {Estado}";
}
