using System.Diagnostics;
using System.IO;

namespace MiniOS.Simulator;

public sealed class FrmExploradorArchivos : Form
{
    private readonly SistemaArchivos sistema;
    private readonly TreeView tvRutas = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = TemaMiniOS.Blanco,
        ForeColor = TemaMiniOS.VerdeOscuro,
        HideSelection = false
    };

    private readonly DataGridView dgv = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoGenerateColumns = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        BackgroundColor = TemaMiniOS.Blanco,
        BorderStyle = BorderStyle.None
    };

    private readonly TextBox txtRuta = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly Label lblEstado = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Button btnNuevaCarpeta;
    private readonly Button btnNuevoArchivo;
    private readonly Button btnRenombrar;
    private readonly Button btnEliminar;
    private string? rutaActual;

    public FrmExploradorArchivos(SistemaArchivos sistema)
    {
        this.sistema = sistema;
        Text = "MiniOS - Sistema de archivos real";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 600);
        ClientSize = new Size(1120, 700);
        TemaMiniOS.Aplicar(this);

        ConfigurarTabla();

        btnNuevaCarpeta = Boton("＋ Carpeta", NuevaCarpeta);
        btnNuevoArchivo = Boton("＋ Archivo .txt", NuevoArchivo);
        btnRenombrar = Boton("Renombrar", Renombrar);
        btnEliminar = Boton("Eliminar", Eliminar);

        Controls.Add(ConstruirInterfaz());

        tvRutas.BeforeExpand += (_, e) => sistema.CargarHijos(e.Node);
        tvRutas.AfterSelect += (_, e) =>
        {
            if (e.Node.Tag is string ruta && Directory.Exists(ruta))
                Navegar(ruta);
        };
        dgv.SelectionChanged += (_, _) => ActualizarBotonesEdicion();
        dgv.CellDoubleClick += (_, _) => AbrirSeleccion();

        RecargarArbol();
    }

    private Control ConstruirInterfaz()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            RowCount = 4,
            ColumnCount = 1
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var titulo = new Label
        {
            Text = "🗂  SISTEMA DE ARCHIVOS DEL EQUIPO",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = TemaMiniOS.VerdeOscuro
        };
        raiz.Controls.Add(titulo, 0, 0);

        var superior = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        superior.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        superior.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        superior.Controls.Add(txtRuta, 0, 0);

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true
        };
        acciones.Controls.Add(Boton("Actualizar", ActualizarDirectorio));
        acciones.Controls.Add(Boton("Abrir", AbrirSeleccion));
        acciones.Controls.Add(Boton("Propiedades", VerPropiedades));
        acciones.Controls.Add(Boton("Zona segura", IrZonaSegura));
        acciones.Controls.Add(btnNuevaCarpeta);
        acciones.Controls.Add(btnNuevoArchivo);
        acciones.Controls.Add(btnRenombrar);
        acciones.Controls.Add(btnEliminar);
        superior.Controls.Add(acciones, 0, 1);
        raiz.Controls.Add(superior, 0, 1);

        var division = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 300,
            BackColor = TemaMiniOS.Fondo
        };
        division.Panel1.Padding = new Padding(4);
        division.Panel2.Padding = new Padding(4);
        division.Panel1.Controls.Add(Tarjeta("UNIDADES Y CARPETAS", tvRutas));
        division.Panel2.Controls.Add(Tarjeta("CONTENIDO", dgv));
        raiz.Controls.Add(division, 0, 2);

        lblEstado.Text = "Explorador en modo lectura. Las operaciones de escritura solo se habilitan dentro de MiniOS_Sandbox.";
        raiz.Controls.Add(lblEstado, 0, 3);
        return raiz;
    }

    private void ConfigurarTabla()
    {
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = TemaMiniOS.VerdeClaro;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TemaMiniOS.VerdeOscuro;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgv.DefaultCellStyle.SelectionBackColor = TemaMiniOS.VerdeAzulado;
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;

        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre", Width = 300 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", Width = 110 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tamano", HeaderText = "Tamaño", Width = 110 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modificado", HeaderText = "Modificado", Width = 160 });
    }

    private void RecargarArbol()
    {
        tvRutas.Nodes.Clear();
        var raiz = sistema.CrearArbol();
        tvRutas.Nodes.Add(raiz);

        var primeraUnidad = raiz.Nodes.Cast<TreeNode>().FirstOrDefault();
        if (primeraUnidad?.Tag is string ruta)
        {
            tvRutas.SelectedNode = primeraUnidad;
            Navegar(ruta);
        }
    }

    private void Navegar(string ruta)
    {
        try
        {
            var elementos = sistema.ObtenerElementos(ruta);
            rutaActual = ruta;
            txtRuta.Text = ruta;
            dgv.Rows.Clear();

            foreach (var elemento in elementos)
            {
                var indice = dgv.Rows.Add(
                    elemento.Nombre,
                    elemento.Tipo,
                    elemento.EsDirectorio ? "-" : FormatearBytes(elemento.TamanoBytes ?? 0),
                    elemento.Modificado?.ToString("dd/MM/yyyy HH:mm") ?? "-");
                dgv.Rows[indice].Tag = elemento;
            }

            var modo = sistema.EsRutaEnZonaSegura(ruta)
                ? "Zona segura MiniOS: escritura habilitada."
                : "Exploración real en modo lectura.";
            lblEstado.Text = $"{elementos.Count} elemento(s). {modo}";
            ActualizarBotonesEdicion();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "MiniOS - Sistema de archivos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ActualizarDirectorio()
    {
        if (rutaActual is not null)
            Navegar(rutaActual);
        RecargarNodoSeleccionado();
    }

    private void RecargarNodoSeleccionado()
    {
        var nodo = tvRutas.SelectedNode;
        if (nodo?.Tag is not string ruta || !Directory.Exists(ruta))
            return;

        nodo.Nodes.Clear();
        nodo.Nodes.Add(new TreeNode("...") { Tag = "__MINIOS_PENDIENTE__" });
        sistema.CargarHijos(nodo);
    }

    private ElementoSistemaArchivo? Seleccionado()
        => dgv.SelectedRows.Count == 0 ? null : dgv.SelectedRows[0].Tag as ElementoSistemaArchivo;

    private void AbrirSeleccion()
    {
        var elemento = Seleccionado();
        if (elemento is null)
            return;

        try
        {
            if (elemento.EsDirectorio)
            {
                Navegar(elemento.Ruta);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = elemento.Ruta,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"No se pudo abrir el elemento.\n{ex.Message}", "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void VerPropiedades()
    {
        var elemento = Seleccionado();
        var ruta = elemento?.Ruta ?? rutaActual;
        if (string.IsNullOrWhiteSpace(ruta))
            return;

        try
        {
            string texto;
            if (Directory.Exists(ruta))
            {
                var info = new DirectoryInfo(ruta);
                texto = $"Nombre: {info.Name}\nTipo: Carpeta\nRuta: {info.FullName}\nModificado: {info.LastWriteTime:dd/MM/yyyy HH:mm}";
            }
            else
            {
                var info = new FileInfo(ruta);
                texto = $"Nombre: {info.Name}\nTipo: Archivo {info.Extension}\nRuta: {info.FullName}\nTamaño: {FormatearBytes(info.Length)}\nModificado: {info.LastWriteTime:dd/MM/yyyy HH:mm}";
            }

            MessageBox.Show(this, texto, "Propiedades", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void IrZonaSegura()
    {
        try
        {
            var zona = sistema.AsegurarZonaSegura();
            Navegar(zona);
            lblEstado.Text = $"Zona segura activa: {zona}. Aquí MiniOS puede crear, renombrar y eliminar archivos de prueba.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void NuevaCarpeta()
    {
        if (rutaActual is null)
            return;
        var nombre = PedirTexto("Nueva carpeta", "Nombre de la carpeta:", "Nueva carpeta");
        if (nombre is null)
            return;

        EjecutarOperacionSegura(() => sistema.CrearCarpetaSegura(rutaActual, nombre));
    }

    private void NuevoArchivo()
    {
        if (rutaActual is null)
            return;
        var nombre = PedirTexto("Nuevo archivo", "Nombre del archivo:", "archivo.txt");
        if (nombre is null)
            return;

        EjecutarOperacionSegura(() => sistema.CrearArchivoTextoSeguro(rutaActual, nombre));
    }

    private void Renombrar()
    {
        var elemento = Seleccionado();
        if (elemento is null)
            return;

        var nombre = PedirTexto("Renombrar", "Nuevo nombre:", elemento.Nombre);
        if (nombre is null)
            return;

        EjecutarOperacionSegura(() => sistema.RenombrarSeguro(elemento.Ruta, nombre));
    }

    private void Eliminar()
    {
        var elemento = Seleccionado();
        if (elemento is null)
            return;

        var confirmar = MessageBox.Show(
            this,
            $"¿Eliminar '{elemento.Nombre}' de la zona segura de MiniOS?",
            "Confirmar eliminación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirmar != DialogResult.Yes)
            return;

        try
        {
            sistema.EliminarSeguro(elemento.Ruta);
            ActualizarDirectorio();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void EjecutarOperacionSegura(Func<string> operacion)
    {
        try
        {
            operacion();
            ActualizarDirectorio();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ActualizarBotonesEdicion()
    {
        var enZonaSegura = sistema.EsRutaEnZonaSegura(rutaActual);
        var seleccionado = Seleccionado();
        btnNuevaCarpeta.Enabled = enZonaSegura;
        btnNuevoArchivo.Enabled = enZonaSegura;
        btnRenombrar.Enabled = seleccionado is not null && sistema.EsElementoEditable(seleccionado.Ruta);
        btnEliminar.Enabled = seleccionado is not null && sistema.EsElementoEditable(seleccionado.Ruta);
    }

    private static string FormatearBytes(long bytes)
    {
        string[] unidades = ["B", "KB", "MB", "GB", "TB"];
        double valor = bytes;
        var indice = 0;
        while (valor >= 1024 && indice < unidades.Length - 1)
        {
            valor /= 1024;
            indice++;
        }
        return $"{valor:0.##} {unidades[indice]}";
    }

    private static string? PedirTexto(string titulo, string etiqueta, string valorInicial)
    {
        using var dialogo = new Form
        {
            Text = titulo,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(390, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        TemaMiniOS.Aplicar(dialogo);

        var txt = new TextBox { Text = valorInicial, Dock = DockStyle.Fill };
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3, ColumnCount = 1 };
        tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        tabla.Controls.Add(new Label { Text = etiqueta, AutoSize = true }, 0, 0);
        tabla.Controls.Add(txt, 0, 1);

        var aceptar = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Width = 95 };
        var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 95 };
        var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        botones.Controls.Add(cancelar);
        botones.Controls.Add(aceptar);
        tabla.Controls.Add(botones, 0, 2);
        dialogo.Controls.Add(tabla);
        dialogo.AcceptButton = aceptar;
        dialogo.CancelButton = cancelar;

        return dialogo.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
    }

    private static GroupBox Tarjeta(string titulo, Control contenido)
    {
        var grupo = new GroupBox
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            BackColor = TemaMiniOS.Blanco,
            ForeColor = TemaMiniOS.VerdeOscuro,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(8)
        };
        contenido.Font = new Font("Segoe UI", 9.2f);
        grupo.Controls.Add(contenido);
        return grupo;
    }

    private static Button Boton(string texto, Action accion)
    {
        var boton = new Button
        {
            Text = texto,
            Width = 115,
            Height = 31,
            BackColor = TemaMiniOS.VerdeClaro,
            ForeColor = TemaMiniOS.VerdeOscuro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(3)
        };
        boton.FlatAppearance.BorderColor = TemaMiniOS.VerdeAzulado;
        boton.Click += (_, _) => accion();
        return boton;
    }
}
