using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniOS.Simulator;

// Solo lectura: no instala, inicia ni modifica controladores.
public static class InventarioWindows
{
    private const string PropDispositivo = "a45c254e-df1c-4efd-8020-67d146a850e0";
    private const string PropDriver = "a8b865dd-2e3d-4094-ad97-e593a70c75d6";
    private static readonly Dictionary<string, string> Tipos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Keyboard"] = "Entrada", ["Mouse"] = "Entrada", ["HIDClass"] = "Entrada / salida",
        ["Camera"] = "Entrada", ["Image"] = "Entrada", ["Biometric"] = "Entrada",
        ["Sensor"] = "Entrada", ["Monitor"] = "Salida", ["Display"] = "Salida",
        ["Printer"] = "Salida", ["PrintQueue"] = "Salida", ["AudioEndpoint"] = "Audio",
        ["MEDIA"] = "Audio / video", ["DiskDrive"] = "Almacenamiento", ["Volume"] = "Almacenamiento",
        ["CDROM"] = "Almacenamiento", ["WPD"] = "Dispositivo portátil", ["Net"] = "Red",
        ["Bluetooth"] = "Bluetooth", ["USB"] = "USB", ["USBDevice"] = "USB",
        ["Ports"] = "Puertos", ["Modem"] = "Red", ["SmartCardReader"] = "Entrada"
    };

    // Una sesión conserva metadatos estables, nunca el estado ni la presencia.
    // Solo el trabajador de MonitorDispositivos accede al diccionario.
    public sealed class Sesion
    {
        private readonly Dictionary<string, Nodo> cache = new(StringComparer.OrdinalIgnoreCase);
        private int invalidar;
        public void Invalidar() => Interlocked.Exchange(ref invalidar, 1);
        public IReadOnlyList<Dispositivo> Consultar(CancellationToken token = default)
        {
            if (Interlocked.Exchange(ref invalidar, 0) == 1) cache.Clear();
            return ConsultarInterno(token, cache);
        }
    }

    public static IReadOnlyList<Dispositivo> Consultar(CancellationToken cancelacion = default) => ConsultarInterno(cancelacion, null);

    private static IReadOnlyList<Dispositivo> ConsultarInterno(CancellationToken cancelacion, Dictionary<string, Nodo>? cache)
    {
        cancelacion.ThrowIfCancellationRequested();
        var tiempoConsulta = System.Diagnostics.Stopwatch.StartNew();
        // DIGCF_PRESENT | DIGCF_ALLCLASSES: excluye dispositivos antiguos no presentes.
        IntPtr conjunto = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, 0x2 | 0x4);
        if (conjunto == new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastWin32Error());
        System.Diagnostics.Trace.WriteLine($"E/S: conjunto PnP en {tiempoConsulta.ElapsedMilliseconds} ms");
        try
        {
            var nodos = new Dictionary<string, Nodo>(StringComparer.OrdinalIgnoreCase);
            var clases = new Dictionary<Guid, string>();
            for (uint i = 0; ; i++)
            {
                cancelacion.ThrowIfCancellationRequested();
                var info = new InfoDispositivo { Size = (uint)Marshal.SizeOf<InfoDispositivo>() };
                if (!SetupDiEnumDeviceInfo(conjunto, i, ref info))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 259) break;
                    throw new Win32Exception(error);
                }
                var id = new StringBuilder(1024);
                if (!SetupDiGetDeviceInstanceIdW(conjunto, ref info, id, id.Capacity, out _)) continue;
                if (!clases.TryGetValue(info.Clase, out string? clase))
                {
                    var nombreClase = new StringBuilder(256);
                    SetupDiClassNameFromGuidW(ref info.Clase, nombreClase, nombreClase.Capacity, out _);
                    clase = nombreClase.ToString();
                    clases[info.Clase] = clase;
                }
                string descripcion = "";
                string padre = Padre(info.DevInst);
                string enumerador = id.ToString().Split('\\')[0];
                bool esES = Tipos.TryGetValue(clase, out string? tipo);
                esES |= id.ToString().StartsWith("USB\\", StringComparison.OrdinalIgnoreCase);
                Dispositivo? dispositivo = null;
                if (esES)
                {
                    uint resultado = CM_Get_DevNode_Status(out uint estado, out uint problema, info.DevInst, 0);
                    string estadoTexto = resultado != 0 ? "Estado no disponible"
                        : problema != 0 ? $"Problema de Windows ({problema})"
                        : (estado & 8) != 0 ? "Funcionando" : "Presente";
                    if (cache is not null && cache.TryGetValue(id.ToString(), out var guardado)
                        && guardado.Dispositivo is { } anterior && anterior.Estado == estadoTexto
                        && anterior.VersionDriver.Length > 0 && anterior.ArchivoInf.Length > 0)
                    {
                        dispositivo = anterior;
                        descripcion = guardado.Descripcion;
                    }
                    else
                    {
                        descripcion = Texto(conjunto, ref info, PropDispositivo, 2);
                        string nombre = Texto(conjunto, ref info, PropDispositivo, 14);
                        if (string.IsNullOrWhiteSpace(nombre)) nombre = descripcion;
                        if (string.IsNullOrWhiteSpace(nombre)) nombre = "Dispositivo sin nombre";
                        dispositivo = new Dispositivo
                        {
                        Id = id.ToString(), Nombre = nombre, Clase = clase, Tipo = tipo ?? "USB",
                        Fabricante = Texto(conjunto, ref info, PropDispositivo, 13),
                        Ubicacion = Texto(conjunto, ref info, PropDispositivo, 15),
                        Servicio = Texto(conjunto, ref info, PropDispositivo, 6),
                        ProveedorDriver = Texto(conjunto, ref info, PropDriver, 9),
                        VersionDriver = Texto(conjunto, ref info, PropDriver, 3),
                        DescripcionDriver = Texto(conjunto, ref info, PropDriver, 4),
                        ArchivoInf = Texto(conjunto, ref info, PropDriver, 5),
                        FechaDriver = Fecha(conjunto, ref info), Estado = estadoTexto,
                        EsSoporte = clase.Equals("USB", StringComparison.OrdinalIgnoreCase)
                            || clase.Equals("Volume", StringComparison.OrdinalIgnoreCase)
                            || clase.Equals("HIDClass", StringComparison.OrdinalIgnoreCase)
                            || clase.Equals("Display", StringComparison.OrdinalIgnoreCase)
                        };
                    }
                }
                nodos[id.ToString()] = new Nodo(padre, enumerador, descripcion, dispositivo);
            }
            if (cache is not null)
            {
                foreach (var id in cache.Keys.Where(id => !nodos.ContainsKey(id)).ToArray()) cache.Remove(id);
                foreach (var (id, nodo) in nodos) if (nodo.Dispositivo is not null) cache[id] = nodo;
            }
            System.Diagnostics.Trace.WriteLine($"E/S: propiedades en {tiempoConsulta.ElapsedMilliseconds} ms");
            var bluetooth = ConsultarBluetooth();
            System.Diagnostics.Trace.WriteLine($"E/S: Bluetooth en {tiempoConsulta.ElapsedMilliseconds} ms");
            NetworkInterface[] redes;
            try { redes = NetworkInterface.GetAllNetworkInterfaces(); }
            catch (NetworkInformationException) { redes = []; }
            System.Diagnostics.Trace.WriteLine($"E/S: red en {tiempoConsulta.ElapsedMilliseconds} ms");
            var lista = new List<Dispositivo>();
            foreach (var (id, nodo) in nodos)
            {
                cancelacion.ThrowIfCancellationRequested();
                if (nodo.Dispositivo is not { } d) continue;
                var ancestros = new List<string> { id };
                string padre = nodo.Padre;
                var visitados = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
                while (visitados.Add(padre) && nodos.TryGetValue(padre, out var superior))
                {
                    ancestros.Add(padre);
                    padre = superior.Padre;
                }
                bool esBluetooth = ancestros.Any(x => x.StartsWith("BTH", StringComparison.OrdinalIgnoreCase));
                string conexion = esBluetooth ? "Bluetooth"
                    : ancestros.Any(x => x.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase)) ? "USB"
                    : string.IsNullOrEmpty(nodo.Enumerador) ? "No informada" : nodo.Enumerador;
                d = d with { Conexion = conexion };
                if (esBluetooth)
                {
                    // Windows puede conservar nodos emparejados aunque estén desconectados.
                    var direccion = ancestros.Select(x => Regex.Match(x, @"(?:DEV_|BLUETOOTHDEVICE_)([0-9A-F]{12})", RegexOptions.IgnoreCase))
                        .FirstOrDefault(x => x.Success);
                    if (direccion is not null && bluetooth.TryGetValue(direccion.Groups[1].Value, out bool conectado))
                        d = d with { Conectado = conectado, Estado = conectado ? $"Bluetooth conectado · {d.Estado}" : "Bluetooth sin conexión" };
                    else
                        d = d with { Conectado = null, Estado = $"Bluetooth registrado · {d.Estado}" };
                }
                if (d.Clase.Equals("Net", StringComparison.OrdinalIgnoreCase))
                {
                    var red = redes.FirstOrDefault(x => x.Description.Equals(d.Nombre, StringComparison.OrdinalIgnoreCase)
                        || x.Description.Equals(nodo.Descripcion, StringComparison.OrdinalIgnoreCase));
                    if (red is not null)
                        d = d with { Estado = $"{d.Estado} · " + (red.OperationalStatus == OperationalStatus.Up ? "Red conectada" : "Sin enlace de red") };
                }
                lista.Add(d);
            }
            return lista.OrderBy(d => d.Tipo).ThenBy(d => d.Nombre).ThenBy(d => d.Id).ToArray();
        }
        finally { SetupDiDestroyDeviceInfoList(conjunto); }
    }

    private sealed record Nodo(string Padre, string Enumerador, string Descripcion, Dispositivo? Dispositivo);
    private static string Padre(uint devInst)
    {
        if (CM_Get_Parent(out uint padre, devInst, 0) != 0) return "";
        var id = new StringBuilder(1024);
        return CM_Get_Device_IDW(padre, id, id.Capacity, 0) == 0 ? id.ToString() : "";
    }
    private static byte[]? Propiedad(IntPtr conjunto, ref InfoDispositivo info, string grupo, uint pid, out uint tipo)
    {
        var clave = new ClavePropiedad { Grupo = new Guid(grupo), Id = pid };
        // La mayoría de propiedades caben en un solo acceso; se amplía si Windows lo pide.
        var datos = new byte[4096];
        if (SetupDiGetDevicePropertyW(conjunto, ref info, ref clave, out tipo, datos, (uint)datos.Length, out uint tamano, 0))
            return datos[..(int)tamano];
        if (tamano == 0 || tamano > 1024 * 1024) return null;
        datos = new byte[tamano];
        return SetupDiGetDevicePropertyW(conjunto, ref info, ref clave, out tipo, datos, tamano, out _, 0) ? datos : null;
    }
    private static string Texto(IntPtr conjunto, ref InfoDispositivo info, string grupo, uint pid)
    {
        if (grupo == PropDispositivo)
        {
            uint propiedad = pid switch { 2 => 0, 6 => 4, 13 => 11, 14 => 12, 15 => 13, _ => uint.MaxValue };
            var buffer = new byte[8192];
            if (propiedad != uint.MaxValue && SetupDiGetDeviceRegistryPropertyW(conjunto, ref info, propiedad, out uint formato, buffer, (uint)buffer.Length, out uint longitud)
                && formato is 1 or 2 && longitud <= buffer.Length)
                return Encoding.Unicode.GetString(buffer, 0, (int)longitud).TrimEnd('\0');
            return "";
        }
        var datos = Propiedad(conjunto, ref info, grupo, pid, out uint tipo);
        return datos is not null && tipo == 0x12 ? Encoding.Unicode.GetString(datos).TrimEnd('\0') : "";
    }
    private static string Fecha(IntPtr conjunto, ref InfoDispositivo info)
    {
        var datos = Propiedad(conjunto, ref info, PropDriver, 2, out uint tipo);
        if (datos is null || datos.Length < 8 || tipo != 0x10) return "";
        try { return DateTime.FromFileTimeUtc(BitConverter.ToInt64(datos)).ToString("yyyy-MM-dd"); }
        catch (ArgumentOutOfRangeException) { return ""; }
    }
    private static Dictionary<string, bool> ConsultarBluetooth()
    {
        var dispositivos = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var busqueda = new BusquedaBluetooth
        {
            Size = (uint)Marshal.SizeOf<BusquedaBluetooth>(), Autenticados = true,
            Recordados = true, Desconocidos = true, Conectados = true
            // Consulta local; no inicia descubrimiento de dispositivos cercanos.
        };
        var info = new InfoBluetooth { Size = (uint)Marshal.SizeOf<InfoBluetooth>() };
        IntPtr identificador = BluetoothFindFirstDevice(ref busqueda, ref info);
        if (identificador == IntPtr.Zero) return dispositivos;
        try
        {
            do { dispositivos[info.Direccion.ToString("X12")] = info.Conectado; }
            while (BluetoothFindNextDevice(identificador, ref info));
        }
        finally { BluetoothFindDeviceClose(identificador); }
        return dispositivos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InfoDispositivo { public uint Size; public Guid Clase; public uint DevInst; public UIntPtr Reservado; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ClavePropiedad { public Guid Grupo; public uint Id; }
    [StructLayout(LayoutKind.Sequential)]
    private struct HoraWindows { public ushort Ano, Mes, DiaSemana, Dia, Hora, Minuto, Segundo, Milisegundo; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct InfoBluetooth
    {
        public uint Size;
        public ulong Direccion;
        public uint Clase;
        [MarshalAs(UnmanagedType.Bool)] public bool Conectado;
        [MarshalAs(UnmanagedType.Bool)] public bool Recordado;
        [MarshalAs(UnmanagedType.Bool)] public bool Autenticado;
        public HoraWindows UltimaVista, UltimoUso;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)] public string Nombre;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct BusquedaBluetooth
    {
        public uint Size;
        [MarshalAs(UnmanagedType.Bool)] public bool Autenticados;
        [MarshalAs(UnmanagedType.Bool)] public bool Recordados;
        [MarshalAs(UnmanagedType.Bool)] public bool Desconocidos;
        [MarshalAs(UnmanagedType.Bool)] public bool Conectados;
        [MarshalAs(UnmanagedType.Bool)] public bool Descubrir;
        public byte Tiempo;
        public IntPtr Radio;
    }
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevsW(IntPtr clase, string? enumerador, IntPtr ventana, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr conjunto, uint indice, ref InfoDispositivo info);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInstanceIdW(IntPtr conjunto, ref InfoDispositivo info, StringBuilder id, int capacidad, out uint requerido);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDevicePropertyW(IntPtr conjunto, ref InfoDispositivo info, ref ClavePropiedad clave, out uint tipo, byte[]? datos, uint capacidad, out uint requerido, uint flags);
    [DllImport("setupapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr conjunto);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiClassNameFromGuidW(ref Guid clase, StringBuilder nombre, int capacidad, out uint requerido);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr conjunto, ref InfoDispositivo info, uint propiedad, out uint tipo, byte[] datos, uint capacidad, out uint requerido);
    [DllImport("cfgmgr32.dll")]
    private static extern uint CM_Get_Parent(out uint padre, uint devInst, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_Device_IDW(uint devInst, StringBuilder id, int capacidad, uint flags);
    [DllImport("cfgmgr32.dll")]
    private static extern uint CM_Get_DevNode_Status(out uint estado, out uint problema, uint devInst, uint flags);
    [DllImport("bthprops.cpl")]
    private static extern IntPtr BluetoothFindFirstDevice(ref BusquedaBluetooth busqueda, ref InfoBluetooth info);
    [DllImport("bthprops.cpl")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BluetoothFindNextDevice(IntPtr identificador, ref InfoBluetooth info);
    [DllImport("bthprops.cpl")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BluetoothFindDeviceClose(IntPtr identificador);
}
