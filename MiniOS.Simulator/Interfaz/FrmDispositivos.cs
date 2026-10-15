namespace MiniOS.Simulator;

public sealed class FrmDispositivos : Form
{
    private readonly MonitorDispositivos monitor;
    private readonly TextBox buscar = new() { Dock = DockStyle.Fill, PlaceholderText = "Nombre, fabricante o controlador..." };
    private readonly ComboBox categoria = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox auxiliares = new() { Text = "Mostrar interfaces auxiliares (USB, HID, volúmenes)", AutoSize = true };
    private readonly Label resumen = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly DataGridView tabla = CrearTabla();
    private readonly DataGridView ficha = CrearTabla();
    private readonly DataGridView actividad = CrearTabla();
    private Dispositivo[] visibles = [];
    private int cantidadEventos = -1;
    private EventoDispositivo? ultimoEvento;

    public FrmDispositivos(MonitorDispositivos monitor, string? seleccionado = null)
    {
        this.monitor = monitor;
        Text = "Administración de dispositivos de E/S";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 780);
        MinimumSize = new Size(900, 680);
        TemaMiniOS.Aplicar(this);
        ConfigurarColumnas();
        var contenido = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var encabezado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaMiniOS.VerdeClaro, Padding = new Padding(10, 4, 0, 4) };
        encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        encabezado.Controls.Add(new Label
        {
            Text = "DISPOSITIVOS DE ENTRADA / SALIDA",
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = TemaMiniOS.VerdeOscuro, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        encabezado.Controls.Add(new Label { Text = "Inventario del equipo y controladores asociados", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        contenido.Controls.Add(encabezado, 0, 0);
        var herramientas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        herramientas.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        herramientas.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        herramientas.Controls.Add(buscar, 0, 0);
        categoria.Items.Add("Todas las categorías");
        foreach (string tipo in new[] { "Almacenamiento", "Entrada", "Salida", "Audio", "Audio / video", "Bluetooth", "Dispositivo portátil", "Entrada / salida", "Puertos", "Red", "USB" }) categoria.Items.Add(tipo);
        categoria.SelectedIndex = 0;
        herramientas.Controls.Add(categoria, 1, 0);
        var actualizar = new Button { Text = "Actualizar", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = TemaMiniOS.VerdeClaro };
        actualizar.Click += (_, _) => monitor.SolicitarActualizacion(recargarInformacion: true);
        herramientas.Controls.Add(actualizar, 2, 0);
        herramientas.Controls.Add(auxiliares, 0, 1);
        herramientas.SetColumnSpan(auxiliares, 3);
        contenido.Controls.Add(herramientas, 0, 1);
        contenido.Controls.Add(tabla, 0, 2);
        var pestanas = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
        var detalle = new TabPage("Dispositivo y controlador") { Padding = new Padding(4) };
        detalle.Controls.Add(ficha);
        var historial = new TabPage("Actividad de conexiones") { Padding = new Padding(4) };
        historial.Controls.Add(actividad);
        pestanas.TabPages.Add(detalle);
        pestanas.TabPages.Add(historial);
        contenido.Controls.Add(pestanas, 0, 3);
        contenido.Controls.Add(resumen, 0, 4);
        Controls.Add(contenido);
        buscar.TextChanged += (_, _) => Refrescar(true);
        categoria.SelectedIndexChanged += (_, _) => Refrescar(true);
        auxiliares.CheckedChanged += (_, _) => Refrescar(true);
        tabla.CurrentCellChanged += (_, _) => MostrarFicha();
        monitor.Actualizado += AlActualizar;
        FormClosed += (_, _) => monitor.Actualizado -= AlActualizar;
        Refrescar(true);
        if (seleccionado is not null)
        {
            if (monitor.Catalogo.Dispositivos.Any(d => d.Id == seleccionado && d.EsSoporte)) auxiliares.Checked = true;
            Seleccionar(seleccionado);
        }
    }

    private void AlActualizar() => Refrescar(false);
    private void Refrescar(bool forzar)
    {
        if (IsDisposed) return;
        string filtro = buscar.Text.Trim();
        var nuevos = monitor.Catalogo.Dispositivos.Where(d =>
            (auxiliares.Checked || !d.EsSoporte)
            && (categoria.SelectedIndex == 0 || d.Tipo == categoria.Text)
            && (filtro.Length == 0 || $"{d.Nombre} {d.Fabricante} {d.Id} {d.ProveedorDriver} {d.VersionDriver}".Contains(filtro, StringComparison.CurrentCultureIgnoreCase))).ToArray();
        if (forzar || !visibles.SequenceEqual(nuevos))
        {
            string? id = (tabla.CurrentRow?.Tag as Dispositivo)?.Id;
            int primera = tabla.FirstDisplayedScrollingRowIndex;
            visibles = nuevos;
            tabla.SuspendLayout();
            tabla.Rows.Clear();
            foreach (var d in visibles)
            {
                int fila = tabla.Rows.Add(d.Nombre, d.Tipo, d.Conexion, d.Estado);
                tabla.Rows[fila].Tag = d;
                if (d.Conectado == false) tabla.Rows[fila].DefaultCellStyle.ForeColor = Color.DimGray;
            }
            if (id is not null) Seleccionar(id);
            if (primera >= 0 && primera < tabla.Rows.Count) tabla.FirstDisplayedScrollingRowIndex = primera;
            tabla.ResumeLayout();
            MostrarFicha();
        }
        var eventos = monitor.Catalogo.Historial;
        if (cantidadEventos != eventos.Count || ultimoEvento != eventos.LastOrDefault())
        {
            actividad.Rows.Clear();
            foreach (var e in eventos.Reverse()) actividad.Rows.Add(e.Hora.ToString("HH:mm:ss"), e.Accion, e.Nombre, e.Detalle);
            cantidadEventos = eventos.Count;
            ultimoEvento = eventos.LastOrDefault();
        }
        resumen.Text = monitor.Error is not null ? monitor.Error
            : monitor.UltimaConsulta is null ? "Consultando dispositivos de Windows..."
            : $"{visibles.Length} registros visibles · Actualización automática cada 2 s · Última consulta: {monitor.UltimaConsulta:HH:mm:ss}";
        resumen.ForeColor = monitor.Error is null ? TemaMiniOS.VerdeOscuro : Color.DarkRed;
    }
    private void Seleccionar(string id)
    {
        foreach (DataGridViewRow fila in tabla.Rows)
            if (fila.Tag is Dispositivo d && d.Id == id) { tabla.CurrentCell = fila.Cells[0]; break; }
    }
    private void MostrarFicha()
    {
        ficha.Rows.Clear();
        if (tabla.CurrentRow?.Tag is not Dispositivo d)
        {
            ficha.Rows.Add("Selección", "No hay dispositivos que coincidan con el filtro.");
            return;
        }
        void Dato(string nombre, string valor) => ficha.Rows.Add(nombre, string.IsNullOrWhiteSpace(valor) ? "No informado por Windows" : valor);
        Dato("Nombre", d.Nombre);
        Dato("Proveedor del controlador", d.ProveedorDriver);
        Dato("Versión del controlador", d.VersionDriver);
        Dato("Fecha del controlador", d.FechaDriver);
        Dato("Archivo de instalación (INF)", d.ArchivoInf);
        Dato("Descripción del controlador", d.DescripcionDriver);
        Dato("Servicio del controlador", d.Servicio);
        Dato("Tipo / clase", $"{d.Tipo} / {d.Clase}");
        Dato("Conexión / bus", d.Conexion);
        Dato("Estado", d.Estado);
        Dato("Fabricante del dispositivo", d.Fabricante);
        Dato("Ubicación", d.Ubicacion);
        Dato("Identificador PnP", d.Id);
        Dato("Acerca del listado", "Una fila es una función reconocida por Windows. Un periférico puede generar varias filas. Incluye dispositivos internos y virtuales.");
        if (d.Conexion == "USB") Dato("USB / tipo C", "Windows informa el bus USB; no siempre permite distinguir la forma del conector. El cable solo no es un dispositivo.");
        if (d.Conectado is null) Dato("Estado Bluetooth", "Registrado no confirma una conexión activa. El estado de algunos dispositivos Bluetooth LE depende de lo que exponga Windows.");
    }
    private void ConfigurarColumnas()
    {
        Columna(tabla, "Nombre del dispositivo", 39);
        Columna(tabla, "Tipo", 17);
        Columna(tabla, "Conexión", 12);
        Columna(tabla, "Estado", 32);
        Columna(ficha, "Propiedad", 26);
        Columna(ficha, "Información", 74);
        ficha.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        ficha.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        Columna(actividad, "Hora", 10);
        Columna(actividad, "Evento", 14);
        Columna(actividad, "Dispositivo", 32);
        Columna(actividad, "Detalle", 44);
    }
    private static void Columna(DataGridView tabla, string titulo, float peso) => tabla.Columns.Add(new DataGridViewTextBoxColumn
    {
        HeaderText = titulo, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        FillWeight = peso, MinimumWidth = 65, SortMode = DataGridViewColumnSortMode.NotSortable
    });
    private static DataGridView CrearTabla() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, MultiSelect = false, AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 32,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = TemaMiniOS.VerdeClaro, ForeColor = TemaMiniOS.VerdeOscuro, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
        DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9), ForeColor = TemaMiniOS.VerdeOscuro, SelectionBackColor = TemaMiniOS.VerdeAzulado, SelectionForeColor = Color.White, Padding = new Padding(3) },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(242, 245, 239) }
    };
}
