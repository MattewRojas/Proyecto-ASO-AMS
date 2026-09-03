using System.IO;

namespace MiniOS.Simulator;

public sealed record ElementoSistemaArchivo(
    string Nombre,
    string Tipo,
    string Ruta,
    long? TamanoBytes,
    DateTime? Modificado,
    bool EsDirectorio);

public sealed class SistemaArchivos
{
    private const string NodoPendiente = "__MINIOS_PENDIENTE__";

    public string ZonaSegura
    {
        get
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var baseUsuario = string.IsNullOrWhiteSpace(documentos)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : documentos;
            return Path.Combine(baseUsuario, "MiniOS_Sandbox");
        }
    }

    public TreeNode CrearArbol()
    {
        var raiz = new TreeNode("Este equipo");

        foreach (var unidad in ObtenerUnidades())
        {
            string etiqueta;
            try
            {
                etiqueta = string.IsNullOrWhiteSpace(unidad.VolumeLabel)
                    ? unidad.Name
                    : $"{unidad.VolumeLabel} ({unidad.Name.TrimEnd('\\')})";
            }
            catch
            {
                etiqueta = unidad.Name;
            }

            var nodo = new TreeNode(etiqueta) { Tag = unidad.RootDirectory.FullName };
            AgregarMarcadorExpansion(nodo);
            raiz.Nodes.Add(nodo);
        }

        raiz.Expand();
        return raiz;
    }

    public IReadOnlyList<DriveInfo> ObtenerUnidades()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .OrderBy(d => d.Name)
                .ToList();
        }
        catch
        {
            return Array.Empty<DriveInfo>();
        }
    }

    public void CargarHijos(TreeNode nodo)
    {
        if (nodo.Tag is not string ruta || string.IsNullOrWhiteSpace(ruta) || !Directory.Exists(ruta))
            return;

        if (nodo.Nodes.Count == 0 || nodo.Nodes[0].Tag as string != NodoPendiente)
            return;

        nodo.Nodes.Clear();

        try
        {
            foreach (var directorio in Directory.GetDirectories(ruta).OrderBy(p => Path.GetFileName(p)))
            {
                var nombre = Path.GetFileName(directorio);
                if (string.IsNullOrWhiteSpace(nombre))
                    nombre = directorio;

                var hijo = new TreeNode(nombre) { Tag = directorio };
                if (PuedeContenerDirectorios(directorio))
                    AgregarMarcadorExpansion(hijo);
                nodo.Nodes.Add(hijo);
            }
        }
        catch (UnauthorizedAccessException)
        {
            nodo.Nodes.Add(new TreeNode("Acceso restringido") { ForeColor = Color.Gray });
        }
        catch (IOException)
        {
            nodo.Nodes.Add(new TreeNode("No se pudo leer esta ubicación") { ForeColor = Color.Gray });
        }
    }

    public IReadOnlyList<ElementoSistemaArchivo> ObtenerElementos(string ruta)
    {
        var elementos = new List<ElementoSistemaArchivo>();
        if (string.IsNullOrWhiteSpace(ruta) || !Directory.Exists(ruta))
            return elementos;

        try
        {
            foreach (var directorio in Directory.GetDirectories(ruta).OrderBy(p => Path.GetFileName(p)))
            {
                try
                {
                    var info = new DirectoryInfo(directorio);
                    elementos.Add(new ElementoSistemaArchivo(
                        info.Name,
                        "Carpeta",
                        info.FullName,
                        null,
                        info.LastWriteTime,
                        true));
                }
                catch
                {
                    // Un elemento inaccesible no impide mostrar el resto del directorio.
                }
            }

            foreach (var archivo in Directory.GetFiles(ruta).OrderBy(p => Path.GetFileName(p)))
            {
                try
                {
                    var info = new FileInfo(archivo);
                    var tipo = string.IsNullOrWhiteSpace(info.Extension)
                        ? "Archivo"
                        : info.Extension.TrimStart('.').ToUpperInvariant();
                    elementos.Add(new ElementoSistemaArchivo(
                        info.Name,
                        tipo,
                        info.FullName,
                        info.Length,
                        info.LastWriteTime,
                        false));
                }
                catch
                {
                    // Igual que con carpetas: se omiten únicamente entradas problemáticas.
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("Windows no permite acceder a esta carpeta con los permisos actuales.");
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"No se pudo leer la ubicación: {ex.Message}");
        }

        return elementos;
    }

    public string AsegurarZonaSegura()
    {
        Directory.CreateDirectory(ZonaSegura);
        return ZonaSegura;
    }

    public bool EsRutaEnZonaSegura(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return false;

        try
        {
            var zona = Path.GetFullPath(ZonaSegura).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var objetivo = Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return objetivo.Equals(zona, StringComparison.OrdinalIgnoreCase) ||
                   objetivo.StartsWith(zona + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public bool EsElementoEditable(string? ruta)
    {
        if (!EsRutaEnZonaSegura(ruta) || string.IsNullOrWhiteSpace(ruta))
            return false;

        var zona = Path.GetFullPath(ZonaSegura).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var objetivo = Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !objetivo.Equals(zona, StringComparison.OrdinalIgnoreCase);
    }

    public string CrearCarpetaSegura(string carpetaActual, string nombre)
    {
        ValidarNombre(nombre);
        if (!EsRutaEnZonaSegura(carpetaActual))
            throw new InvalidOperationException("La creación de carpetas está habilitada únicamente dentro de MiniOS_Sandbox.");

        var destino = Path.Combine(carpetaActual, nombre.Trim());
        if (!EsRutaEnZonaSegura(destino))
            throw new InvalidOperationException("La operación debe permanecer dentro de MiniOS_Sandbox.");
        Directory.CreateDirectory(destino);
        return destino;
    }

    public string CrearArchivoTextoSeguro(string carpetaActual, string nombre)
    {
        ValidarNombre(nombre);
        if (!EsRutaEnZonaSegura(carpetaActual))
            throw new InvalidOperationException("La creación de archivos está habilitada únicamente dentro de MiniOS_Sandbox.");

        var limpio = nombre.Trim();
        if (string.IsNullOrWhiteSpace(Path.GetExtension(limpio)))
            limpio += ".txt";

        var destino = Path.Combine(carpetaActual, limpio);
        if (!EsRutaEnZonaSegura(destino))
            throw new InvalidOperationException("La operación debe permanecer dentro de MiniOS_Sandbox.");
        if (File.Exists(destino))
            throw new InvalidOperationException("Ya existe un archivo con ese nombre.");

        File.WriteAllText(destino, "Archivo creado desde MiniOS Simulator." + Environment.NewLine);
        return destino;
    }

    public string RenombrarSeguro(string ruta, string nuevoNombre)
    {
        ValidarNombre(nuevoNombre);
        if (!EsElementoEditable(ruta))
            throw new InvalidOperationException("Solo se pueden renombrar elementos contenidos en MiniOS_Sandbox.");

        var padre = Path.GetDirectoryName(ruta) ?? throw new InvalidOperationException("No se pudo determinar la carpeta contenedora.");
        var destino = Path.Combine(padre, nuevoNombre.Trim());
        if (!EsRutaEnZonaSegura(destino))
            throw new InvalidOperationException("La operación debe permanecer dentro de MiniOS_Sandbox.");

        if (Directory.Exists(ruta))
            Directory.Move(ruta, destino);
        else if (File.Exists(ruta))
            File.Move(ruta, destino);
        else
            throw new FileNotFoundException("El elemento seleccionado ya no existe.");

        return destino;
    }

    public void EliminarSeguro(string ruta)
    {
        if (!EsElementoEditable(ruta))
            throw new InvalidOperationException("Solo se pueden eliminar elementos contenidos en MiniOS_Sandbox.");

        if (Directory.Exists(ruta))
            Directory.Delete(ruta, recursive: true);
        else if (File.Exists(ruta))
            File.Delete(ruta);
        else
            throw new FileNotFoundException("El elemento seleccionado ya no existe.");
    }

    private static void ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("Ingrese un nombre válido.");

        var limpio = nombre.Trim();
        if (limpio is "." or "..")
            throw new InvalidOperationException("Ese nombre no es válido para un archivo o carpeta.");

        if (limpio.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || limpio.Contains(Path.DirectorySeparatorChar) || limpio.Contains(Path.AltDirectorySeparatorChar))
            throw new InvalidOperationException("El nombre contiene caracteres no permitidos por Windows.");
    }

    private static bool PuedeContenerDirectorios(string ruta)
    {
        try
        {
            return Directory.EnumerateDirectories(ruta).Take(1).Any();
        }
        catch
        {
            return false;
        }
    }

    private static void AgregarMarcadorExpansion(TreeNode nodo)
    {
        nodo.Nodes.Add(new TreeNode("...") { Tag = NodoPendiente });
    }
}
