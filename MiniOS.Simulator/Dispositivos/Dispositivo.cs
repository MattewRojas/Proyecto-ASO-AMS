namespace MiniOS.Simulator;

// Una fila representa una función PnP; un periférico puede tener varias.
public sealed record Dispositivo
{
    public string Id { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Tipo { get; init; } = "";
    public string Clase { get; init; } = "";
    public string Fabricante { get; init; } = "";
    public string Conexion { get; init; } = "";
    public string Estado { get; init; } = "Presente";
    public bool? Conectado { get; init; } = true;
    public string Ubicacion { get; init; } = "";
    public string Servicio { get; init; } = "";
    public string ProveedorDriver { get; init; } = "";
    public string VersionDriver { get; init; } = "";
    public string FechaDriver { get; init; } = "";
    public string ArchivoInf { get; init; } = "";
    public string DescripcionDriver { get; init; } = "";
    public bool EsSoporte { get; init; }
    public override string ToString() => $"{Nombre} · {Tipo} · {Estado}";
}

public sealed record EventoDispositivo(DateTime Hora, string Accion, string Nombre, string Detalle);
