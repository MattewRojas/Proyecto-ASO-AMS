namespace MiniOS.Simulator;

// Compara por identificador PnP, no por nombre: dos ratones iguales son distintos.
public sealed class CatalogoDispositivos
{
    public IReadOnlyList<Dispositivo> Dispositivos { get; private set; } = Array.Empty<Dispositivo>();
    public IReadOnlyList<EventoDispositivo> Historial => historial.AsReadOnly();
    private readonly List<EventoDispositivo> historial = [];
    private bool inicializado;

    public IReadOnlyList<EventoDispositivo> Aplicar(IReadOnlyList<Dispositivo> nuevos)
    {
        var eventos = new List<EventoDispositivo>();
        var anteriores = Dispositivos.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        var actuales = nuevos.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        var hora = DateTime.Now;
        if (!inicializado)
            eventos.Add(new(hora, "Inventario inicial", "Equipo", $"{nuevos.Count} registros de E/S detectados."));
        else
        {
            foreach (var d in Dispositivos.Where(d => !actuales.ContainsKey(d.Id)))
                eventos.Add(new(hora, "Retirado", d.Nombre, $"{d.Tipo} · {d.Conexion}"));
            foreach (var d in nuevos)
            {
                if (!anteriores.TryGetValue(d.Id, out var anterior))
                    eventos.Add(new(hora, "Detectado", d.Nombre, $"{d.Tipo} · {d.Conexion} · {d.Estado}"));
                else if (anterior.Estado != d.Estado || anterior.Conectado != d.Conectado)
                    eventos.Add(new(hora, "Estado", d.Nombre, $"{anterior.Estado} → {d.Estado}"));
                else if (anterior.VersionDriver != d.VersionDriver || anterior.ArchivoInf != d.ArchivoInf
                    || anterior.ProveedorDriver != d.ProveedorDriver || anterior.FechaDriver != d.FechaDriver)
                    eventos.Add(new(hora, "Controlador", d.Nombre, $"{d.ProveedorDriver} · {d.VersionDriver}"));
            }
        }
        inicializado = true;
        Dispositivos = nuevos.ToArray();
        historial.AddRange(eventos);
        if (historial.Count > 200) historial.RemoveRange(0, historial.Count - 200);
        return eventos;
    }
}

// Se crea y utiliza en el hilo de la interfaz. Las consultas se ejecutan en segundo plano.
public sealed class MonitorDispositivos : IDisposable
{
    private readonly Func<CancellationToken, IReadOnlyList<Dispositivo>> consultar;
    private readonly InventarioWindows.Sesion sesion = new();
    private readonly System.Windows.Forms.Timer temporizador = new() { Interval = 2000 };
    private readonly CancellationTokenSource cancelacion = new();
    private bool consultando, pendiente, cerrado;
    public CatalogoDispositivos Catalogo { get; } = new();
    public DateTime? UltimaConsulta { get; private set; }
    public string? Error { get; private set; }
    public bool Consultando => consultando;
    public event Action? Actualizado;
    public event Action<EventoDispositivo>? Cambio;

    public MonitorDispositivos(Func<CancellationToken, IReadOnlyList<Dispositivo>>? consultar = null)
    {
        this.consultar = consultar ?? sesion.Consultar;
        temporizador.Tick += (_, _) => { if (!consultando) SolicitarActualizacion(); };
    }
    public void Iniciar()
    {
        if (cerrado) return;
        temporizador.Start();
        SolicitarActualizacion();
    }
    public void SolicitarActualizacion(bool recargarInformacion = false)
    {
        if (cerrado) return;
        if (recargarInformacion) sesion.Invalidar();
        if (consultando) { pendiente = true; return; }
        _ = ConsultarAsync();
    }
    private async Task ConsultarAsync()
    {
        consultando = true;
        var token = cancelacion.Token;
        try
        {
            do
            {
                pendiente = false;
                try
                {
                    var dispositivos = await Task.Run(() => consultar(token), token);
                    if (cerrado) return;
                    Error = null;
                    UltimaConsulta = DateTime.Now;
                    foreach (var cambio in Catalogo.Aplicar(dispositivos)) Cambio?.Invoke(cambio);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    if (cerrado) return;
                    // Una consulta fallida conserva el último inventario, sin falsas retiradas.
                    Error = $"No se pudo consultar Windows: {ex.Message}";
                }
                Actualizado?.Invoke();
            } while (pendiente && !cerrado);
        }
        finally { consultando = false; }
    }
    public void Dispose()
    {
        if (cerrado) return;
        cerrado = true;
        temporizador.Stop();
        temporizador.Dispose();
        cancelacion.Cancel();
        cancelacion.Dispose();
        Actualizado = null;
        Cambio = null;
    }
}
