namespace MiniOS.Simulator;

public sealed record Periferico
{
    public string Id { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Categoria { get; init; } = "";
    public string Conexion { get; init; } = "";
    public string Estado { get; init; } = "";
    public IReadOnlyList<Dispositivo> Funciones { get; init; } = Array.Empty<Dispositivo>();
    public Dispositivo Principal => Funciones[0];
    public override string ToString() => Nombre;
}

// Proyección de presentación. Conserva el inventario técnico completo y sus IDs.
public static class PresentacionDispositivos
{
    private static readonly HashSet<string> ClasesVisibles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Keyboard", "Mouse", "Monitor", "DiskDrive", "CDROM", "Camera", "Image",
        "WPD", "Printer", "PrintQueue", "Net", "SmartCardReader", "Ports"
    };
    private static bool Es(Dispositivo d, string clase) => d.Clase.Equals(clase, StringComparison.OrdinalIgnoreCase);
    private static bool Tactil(Dispositivo d) => d.Nombre.Contains("táctil", StringComparison.OrdinalIgnoreCase)
        || d.Nombre.Contains("touchpad", StringComparison.OrdinalIgnoreCase);
    private static bool RemotoBluetooth(Dispositivo d) => d.Conexion == "Bluetooth";

    public static IReadOnlyList<Periferico> Agrupar(IReadOnlyList<Dispositivo> inventario)
    {
        var presentes = inventario.Where(d => d.Conectado != false && (!RemotoBluetooth(d) || d.Conectado == true)).ToArray();
        // Los endpoints de audio son nodos SWD, incluso los físicos. Se incluyen solo
        // si su nombre identifica una función MEDIA de hardware presente.
        var audioFisico = presentes.Where(d => Es(d, "MEDIA") && !d.EsVirtual
            && (d.Conexion is "HDAUDIO" or "USB" or "Bluetooth")).ToArray();
        var audioPadres = new Dictionary<string, Dispositivo>(StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in presentes.Where(d => Es(d, "AudioEndpoint")))
        {
            var coincidencias = audioFisico.Where(d => endpoint.Nombre.EndsWith($"({d.Nombre})", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (coincidencias.Length == 1) audioPadres[endpoint.Id] = coincidencias[0];
        }
        var candidatos = presentes.Where(d => audioPadres.ContainsKey(d.Id) || (!d.EsVirtual && (
            ClasesVisibles.Contains(d.Clase)
            || (Es(d, "HIDClass") && Tactil(d))
            || (d.Conexion is "USB" or "Bluetooth" && !d.EsContenedorEquipo
                && (Es(d, "MEDIA") || Es(d, "HIDClass") || Es(d, "USBDevice") || Es(d, "USB") || Es(d, "Bluetooth")))
        ))).Where(d => !Es(d, "Net") || d.Conexion is "PCI" or "USB" or "Bluetooth" or "PCMCIA").ToArray();

        string Clave(Dispositivo d)
        {
            if (audioPadres.TryGetValue(d.Id, out var audio)) return Clave(audio);
            if (d.EsContenedorEquipo && Es(d, "Keyboard")) return "integrado:teclado";
            if (d.EsContenedorEquipo && (Es(d, "Mouse") || Tactil(d))) return "integrado:puntero";
            if (!d.EsContenedorEquipo && d.ContenedorId.Length > 0) return "contenedor:" + d.ContenedorId;
            if (d.OrigenFisico.Length > 0) return "usb:" + d.OrigenFisico;
            return "nodo:" + d.Id;
        }
        var resultado = new List<Periferico>();
        foreach (var grupo in candidatos.GroupBy(Clave, StringComparer.OrdinalIgnoreCase))
        {
            var principales = grupo.OrderBy(Prioridad).ThenBy(d => d.Id).ToArray();
            var d = principales[0];
            // Asociar todas las funciones del mismo periférico para consultar cada driver.
            var funciones = presentes.Where(x => principales.Any(p => p.Id == x.Id)
                || (d.OrigenFisico.Length > 0 && x.OrigenFisico == d.OrigenFisico)
                || (!d.EsContenedorEquipo && d.ContenedorId.Length > 0 && x.ContenedorId == d.ContenedorId)
                || (audioPadres.TryGetValue(d.Id, out var audio) && x.Id == audio.Id))
                .OrderBy(x => x.Id == d.Id ? 0 : 1).ThenBy(Prioridad).ThenBy(x => x.Nombre).ToArray();
            string categoria = Categoria(d);
            string nombre = Nombre(d);
            if (principales.Any(Tactil) && d.EsContenedorEquipo) nombre = "Panel táctil";
            if (Es(d, "AudioEndpoint"))
            {
                bool entrada = principales.Any(x => x.Nombre.Contains("micro", StringComparison.OrdinalIgnoreCase));
                bool salida = principales.Any(x => x.Nombre.Contains("speakers", StringComparison.OrdinalIgnoreCase) || x.Nombre.Contains("altavoces", StringComparison.OrdinalIgnoreCase) || x.Nombre.Contains("headphone", StringComparison.OrdinalIgnoreCase));
                nombre = entrada && salida ? "Altavoces y micrófono" : entrada ? "Micrófono" : salida ? "Altavoces / auriculares" : "Dispositivo de audio";
                categoria = "Audio";
            }
            if (!d.EsContenedorEquipo && principales.Any(x => Es(x, "Mouse")) && principales.Any(x => Es(x, "Keyboard")))
                nombre = "Teclado y ratón";
            string estado = funciones.Any(x => x.Estado.Contains("Problema de Windows", StringComparison.Ordinal)) ? "Requiere atención"
                : Es(d, "Net") && d.Estado.Contains("Sin enlace de red", StringComparison.Ordinal) ? "Sin enlace de red"
                : Es(d, "Net") && d.Estado.Contains("Red conectada", StringComparison.Ordinal) ? "Red conectada" : "Disponible";
            resultado.Add(new Periferico { Id = grupo.Key, Nombre = nombre, Categoria = categoria,
                Conexion = Conexion(d), Estado = estado, Funciones = funciones });
        }
        // Etiquetas iguales no se fusionan: dos ratones físicos mantienen dos filas.
        foreach (var grupo in resultado.GroupBy(p => p.Nombre).Where(g => g.Count() > 1).ToArray())
        {
            int n = 0;
            foreach (var p in grupo.OrderBy(p => p.Id))
            {
                int indice = resultado.IndexOf(p);
                resultado[indice] = p with { Nombre = $"{p.Nombre} {++n}" };
            }
        }
        return resultado.OrderBy(p => Orden(p.Categoria)).ThenBy(p => p.Nombre).ToArray();
    }

    public static Periferico Tecnico(Dispositivo d) => new()
    {
        Id = d.Id, Nombre = d.Nombre, Categoria = d.Tipo, Conexion = d.Conexion,
        Estado = d.Estado, Funciones = new[] { d }
    };
    private static int Orden(string categoria) => categoria switch { "Entrada" => 0, "Imagen" => 1, "Audio" => 2, "Salida" => 3, "Almacenamiento" => 4, "Red" => 5, _ => 6 };
    private static int Prioridad(Dispositivo d) => d.Clase.ToUpperInvariant() switch
    { "WPD" => 0, "MOUSE" => 1, "KEYBOARD" => 2, "CAMERA" => 3, "DISKDRIVE" => 4, "MONITOR" => 5, "AUDIOENDPOINT" => 6, "MEDIA" => 7, "NET" => 8, "HIDCLASS" => 9, _ => 10 };
    private static string Categoria(Dispositivo d) => Es(d, "Camera") || Es(d, "Image") ? "Imagen"
        : Es(d, "HIDClass") ? "Entrada" : Es(d, "MEDIA") || Es(d, "AudioEndpoint") ? "Audio" : d.Tipo;
    private static string Nombre(Dispositivo d)
    {
        if (Tactil(d)) return "Panel táctil";
        if (Es(d, "Mouse")) return d.Conexion == "Bluetooth" ? "Ratón Bluetooth" : d.Conexion == "USB" ? "Ratón USB" : "Ratón";
        if (Es(d, "Keyboard")) return d.EsContenedorEquipo ? "Teclado integrado" : "Teclado";
        if (Es(d, "Camera")) return d.EsContenedorEquipo ? "Cámara integrada" : "Cámara";
        if (Es(d, "Monitor")) return d.EsContenedorEquipo ? "Pantalla integrada" : "Monitor externo";
        if (Es(d, "DiskDrive")) return (d.Conexion == "USB" ? "Unidad USB" : "Disco interno") + " · " + d.Nombre;
        if (Es(d, "Net")) return d.Nombre.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) || d.Nombre.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ? "Adaptador Wi-Fi" : "Adaptador Ethernet";
        if (Es(d, "WPD")) return d.Nombre;
        if (Es(d, "MEDIA")) return string.IsNullOrWhiteSpace(d.NombreBus) ? d.Nombre : d.NombreBus;
        if (Es(d, "HIDClass")) return "Dispositivo de entrada";
        return string.IsNullOrWhiteSpace(d.NombreBus) ? d.Nombre : d.NombreBus;
    }
    public static string Conexion(Dispositivo d) => d.EsContenedorEquipo ? "Integrado"
        : d.Conexion switch { "USB" => "USB", "Bluetooth" => "Bluetooth", "DISPLAY" => "Pantalla", "PCI" or "ACPI" or "HDAUDIO" or "SCSI" => "Interno", "SWD" or "ROOT" => "Software / red", _ => d.Conexion };
}
