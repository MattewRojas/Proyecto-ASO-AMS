namespace MiniOS.Simulator;

public sealed class FrmDispositivos : Form
{
    private readonly MonitorDispositivos monitor;
    private readonly TextBox buscar = new() { Dock = DockStyle.Fill, PlaceholderText = "Buscar un dispositivo...", AccessibleName = "Buscar dispositivo" };
    private readonly ComboBox categoria = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox tecnica = new() { Text = "Vista técnica: incluir funciones, dispositivos virtuales y controladores", AutoSize = true };
    private readonly Label resumen = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Label seleccion = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoEllipsis = true };
    private readonly DataGridView tabla = CrearTabla();
    private readonly DataGridView ficha = CrearTabla();
    private readonly DataGridView controlador = CrearTabla();
    private readonly DataGridView actividad = CrearTabla();
    private readonly ComboBox funciones = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Dispositivo.Nombre) };
    private readonly TabControl pestanas = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
    private readonly Label ayudaDriver = new() { Text = "Controlador de la función seleccionada:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private Periferico[] visibles = [];
    private EventoDispositivo[] eventosVisibles = [];
    private bool cambiandoFicha;
    private string? seleccionadoAnterior;

    public FrmDispositivos(MonitorDispositivos monitor, string? seleccionado = null)
    {
        this.monitor = monitor;
        Text = "Dispositivos de E/S";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 800);
        MinimumSize = new Size(900, 700);
        TemaMiniOS.Aplicar(this);
        ConfigurarColumnas();
        var contenido = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6 };
        contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 47));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 53));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var encabezado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaMiniOS.VerdeClaro, Padding = new Padding(10, 4, 0, 4) };
        encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        encabezado.Controls.Add(new Label { Text = "DISPOSITIVOS DEL EQUIPO", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        encabezado.Controls.Add(new Label { Text = "Selecciona un dispositivo para consultar sus datos o su controlador.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        contenido.Controls.Add(encabezado, 0, 0);
        var herramientas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        herramientas.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        herramientas.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        herramientas.Controls.Add(buscar, 0, 0);
        categoria.Items.Add("Todas las categorías");
        foreach (string tipo in new[] { "Entrada", "Imagen", "Audio", "Salida", "Almacenamiento", "Red", "Bluetooth", "Dispositivo portátil", "Audio / video", "Entrada / salida", "Puertos", "USB" }) categoria.Items.Add(tipo);
        categoria.SelectedIndex = 0;
        herramientas.Controls.Add(categoria, 1, 0);
        var actualizar = new Button { Text = "Actualizar", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = TemaMiniOS.VerdeClaro };
        actualizar.Click += (_, _) => monitor.SolicitarActualizacion(recargarInformacion: true);
        herramientas.Controls.Add(actualizar, 2, 0);
        herramientas.Controls.Add(tecnica, 0, 1);
        herramientas.SetColumnSpan(tecnica, 3);
        contenido.Controls.Add(herramientas, 0, 1);
        contenido.Controls.Add(tabla, 0, 2);
        contenido.Controls.Add(seleccion, 0, 3);
        var dispositivo = new TabPage("Dispositivo") { Padding = new Padding(5) };
        dispositivo.Controls.Add(ficha);
        var driver = new TabPage("Controlador") { Padding = new Padding(5) };
        var panelDriver = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        panelDriver.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panelDriver.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panelDriver.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panelDriver.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panelDriver.Controls.Add(ayudaDriver, 0, 0);
        panelDriver.Controls.Add(funciones, 0, 1);
        panelDriver.Controls.Add(controlador, 0, 2);
        driver.Controls.Add(panelDriver);
        var historial = new TabPage("Conexiones y desconexiones") { Padding = new Padding(5) };
        historial.Controls.Add(actividad);
        pestanas.TabPages.AddRange(new[] { dispositivo, driver, historial });
        contenido.Controls.Add(pestanas, 0, 4);
        contenido.Controls.Add(resumen, 0, 5);
        Controls.Add(contenido);
        buscar.TextChanged += (_, _) => Refrescar(true);
        categoria.SelectedIndexChanged += (_, _) => Refrescar(true);
        tecnica.CheckedChanged += (_, _) => Refrescar(true);
        tabla.CurrentCellChanged += (_, _) => MostrarFicha();
        funciones.SelectedIndexChanged += (_, _) => { if (!cambiandoFicha) MostrarControlador(); };
        monitor.Actualizado += AlActualizar;
        FormClosed += (_, _) => monitor.Actualizado -= AlActualizar;
        Refrescar(true);
        if (seleccionado is not null) Seleccionar(seleccionado);
    }

    private void AlActualizar() => Refrescar(false);
    private static bool Iguales(Periferico a, Periferico b) => a.Id == b.Id && a.Nombre == b.Nombre && a.Categoria == b.Categoria
        && a.Conexion == b.Conexion && a.Estado == b.Estado && a.Funciones.SequenceEqual(b.Funciones);
    private void Refrescar(bool forzar)
    {
        if (IsDisposed) return;
        string filtro = buscar.Text.Trim();
        IEnumerable<Periferico> origen = tecnica.Checked ? monitor.Catalogo.Dispositivos.Select(PresentacionDispositivos.Tecnico) : monitor.Catalogo.Perifericos;
        var nuevos = origen.Where(p => (categoria.SelectedIndex == 0 || p.Categoria == categoria.Text)
            && (filtro.Length == 0 || p.Nombre.Contains(filtro, StringComparison.CurrentCultureIgnoreCase)
                || p.Funciones.Any(d => $"{d.Nombre} {d.Fabricante} {d.Id} {d.ProveedorDriver}".Contains(filtro, StringComparison.CurrentCultureIgnoreCase)))).ToArray();
        if (forzar || visibles.Length != nuevos.Length || !visibles.Zip(nuevos).All(par => Iguales(par.First, par.Second)))
        {
            string? id = (tabla.CurrentRow?.Tag as Periferico)?.Id;
            string? funcion = (tabla.CurrentRow?.Tag as Periferico)?.Principal.Id;
            int primera = tabla.FirstDisplayedScrollingRowIndex;
            visibles = nuevos;
            cambiandoFicha = true;
            tabla.SuspendLayout();
            tabla.Rows.Clear();
            foreach (var p in visibles)
            {
                int fila = tabla.Rows.Add(p.Nombre, p.Categoria, p.Conexion, p.Estado);
                tabla.Rows[fila].Tag = p;
            }
            if (id is not null) Seleccionar(id);
            if (tabla.CurrentRow?.Tag is Periferico actual && actual.Id != id && funcion is not null) Seleccionar(funcion);
            if (primera >= 0 && primera < tabla.Rows.Count) tabla.FirstDisplayedScrollingRowIndex = primera;
            tabla.ResumeLayout();
            cambiandoFicha = false;
            MostrarFicha();
        }
        var eventos = (tecnica.Checked ? monitor.Catalogo.Historial : monitor.Catalogo.HistorialDispositivos).Reverse().ToArray();
        if (!eventosVisibles.SequenceEqual(eventos))
        {
            actividad.Rows.Clear();
            foreach (var e in eventos) actividad.Rows.Add(e.Hora.ToString("HH:mm:ss"), e.Accion, e.Nombre, e.Detalle);
            eventosVisibles = eventos;
        }
        resumen.Text = monitor.Error is not null ? monitor.Error
            : monitor.UltimaConsulta is null ? "Consultando dispositivos de Windows..."
            : $"{visibles.Length} {(tecnica.Checked ? "funciones de Windows" : "dispositivos · incluye integrados")} · Actualización automática · {monitor.UltimaConsulta:HH:mm:ss}";
        resumen.ForeColor = monitor.Error is null ? TemaMiniOS.VerdeOscuro : Color.DarkRed;
    }
    private void Seleccionar(string id)
    {
        foreach (DataGridViewRow fila in tabla.Rows)
            if (fila.Tag is Periferico p && (p.Id == id || p.Funciones.Any(d => d.Id == id)))
            { tabla.CurrentCell = fila.Cells[0]; break; }
    }
    private static void Dato(DataGridView destino, string nombre, string valor) => destino.Rows.Add(nombre, string.IsNullOrWhiteSpace(valor) ? "No informado por Windows" : valor);
    private void MostrarFicha()
    {
        if (cambiandoFicha) return;
        cambiandoFicha = true;
        string? funcion = (funciones.SelectedItem as Dispositivo)?.Id;
        ficha.Rows.Clear();
        funciones.Items.Clear();
        if (tabla.CurrentRow?.Tag is not Periferico p)
        {
            seleccion.Text = "Selecciona un dispositivo";
            Dato(ficha, "Listado", "No hay dispositivos que coincidan con el filtro.");
            controlador.Rows.Clear();
            cambiandoFicha = false;
            return;
        }
        seleccion.Text = p.Nombre;
        var d = p.Principal;
        Dato(ficha, "Dispositivo", p.Nombre);
        Dato(ficha, "Tipo", p.Categoria);
        Dato(ficha, "Conexión", p.Conexion);
        Dato(ficha, "Estado", p.Estado);
        Dato(ficha, "Nombre en Windows", d.Nombre);
        Dato(ficha, "Fabricante", d.Fabricante);
        if (tecnica.Checked)
        {
            Dato(ficha, "Clase", d.Clase);
            Dato(ficha, "Ubicación", d.Ubicacion);
            Dato(ficha, "Identificador PnP", d.Id);
            if (d.Conectado is null) Dato(ficha, "Conexión Bluetooth", "Windows registra este dispositivo, pero no se ha confirmado su conexión activa.");
        }
        foreach (var f in p.Funciones) funciones.Items.Add(f);
        funciones.SelectedIndex = 0;
        if (seleccionadoAnterior == p.Id && funcion is not null)
            for (int i = 0; i < funciones.Items.Count; i++) if (funciones.Items[i] is Dispositivo f && f.Id == funcion) funciones.SelectedIndex = i;
        seleccionadoAnterior = p.Id;
        ayudaDriver.Text = p.Funciones.Count > 1 ? "Este dispositivo tiene varias funciones. Elige cuál consultar:" : "Controlador asociado al dispositivo:";
        cambiandoFicha = false;
        MostrarControlador();
    }
    private void MostrarControlador()
    {
        controlador.Rows.Clear();
        if (funciones.SelectedItem is not Dispositivo d) return;
        Dato(controlador, "Proveedor", d.ProveedorDriver);
        Dato(controlador, "Versión", d.VersionDriver);
        Dato(controlador, "Fecha", d.FechaDriver);
        Dato(controlador, "Archivo INF", d.ArchivoInf);
        Dato(controlador, "Descripción", d.DescripcionDriver);
        Dato(controlador, "Servicio", d.Servicio);
        Dato(controlador, "Función / clase", d.Clase);
        Dato(controlador, "Identificador", d.Id);
    }
    private void ConfigurarColumnas()
    {
        Columna(tabla, "Dispositivo", 44); Columna(tabla, "Tipo", 18); Columna(tabla, "Conexión", 16); Columna(tabla, "Estado", 22);
        foreach (var detalle in new[] { ficha, controlador })
        {
            Columna(detalle, "Dato", 25); Columna(detalle, "Información", 75);
            detalle.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            detalle.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }
        Columna(actividad, "Hora", 11); Columna(actividad, "Evento", 16); Columna(actividad, "Dispositivo", 37); Columna(actividad, "Detalle", 36);
    }
    private static void Columna(DataGridView tabla, string titulo, float peso) => tabla.Columns.Add(new DataGridViewTextBoxColumn
    { HeaderText = titulo, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = peso, MinimumWidth = 65, SortMode = DataGridViewColumnSortMode.NotSortable });
    private static DataGridView CrearTabla() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, MultiSelect = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White, BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 32,
        RowTemplate = { Height = 30 },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = TemaMiniOS.VerdeClaro, ForeColor = TemaMiniOS.VerdeOscuro, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
        DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9), ForeColor = TemaMiniOS.VerdeOscuro, SelectionBackColor = TemaMiniOS.VerdeAzulado, SelectionForeColor = Color.White, Padding = new Padding(3) },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(242, 245, 239) }
    };
}
