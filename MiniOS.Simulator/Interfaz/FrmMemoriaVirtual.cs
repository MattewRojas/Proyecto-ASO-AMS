using System.Text;

namespace MiniOS.Simulator;

public sealed class FrmMemoriaVirtual : Form
{
    private const string Ejemplo = "2, 3, 2, 1*, 5, 2, 4, 5*, 3, 2, 5, 2";
    private readonly ComboBox cboAlgoritmo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280, DropDownWidth = 320 };
    private readonly NumericUpDown numMarcos = new() { Minimum = 1, Maximum = SimuladorMemoriaVirtual.MaxMarcos, Value = 3, Width = 85 };
    private readonly NumericUpDown numLimpieza = new() { Minimum = 0, Maximum = 200, Value = 4, Width = 120 };
    private readonly TextBox txtCadena = new() { Text = Ejemplo, Dock = DockStyle.Fill, MaxLength = 2400 };
    private readonly DataGridView tabla = CrearTabla();
    private readonly DataGridView comparacion = CrearTabla();
    private readonly Label resumen = Etiqueta("", 10, true);
    private readonly Label regla = Etiqueta("", 9);
    private readonly Label estadoComparacion = Etiqueta("Pulse Comparar los 4 para calcular la cadena completa con la misma configuración.", 10);
    private readonly RichTextBox detalle = new()
    {
        ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
        BackColor = TemaMiniOS.VerdeOscuro, ForeColor = Color.White,
        Font = new Font("Segoe UI", 10), ScrollBars = RichTextBoxScrollBars.Vertical
    };
    private readonly TabControl pestanas = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer temporizador = new() { Interval = 700 };
    private readonly Button btnAutomatico;
    private readonly Button btnSiguiente;
    private readonly Button btnTodo;
    private readonly Button btnExportar;
    private readonly Action<string>? registrar;
    private SimuladorMemoriaVirtual? simulador;
    private ReferenciaPagina[] referencias = [];
    private bool configurando;
    private int filaResultado;
    private int filaVictima;
    private int filaFallos;
    private int filaLimpieza;

    private AlgoritmoReemplazo Algoritmo => (AlgoritmoReemplazo)cboAlgoritmo.SelectedIndex;

    public FrmMemoriaVirtual(AlgoritmoReemplazo algoritmo = AlgoritmoReemplazo.OPT,
        Action<string>? registrar = null)
    {
        this.registrar = registrar;
        Text = "AMS.OS - Memoria virtual";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1080, 760);
        ClientSize = new Size(1280, 840);
        WindowState = FormWindowState.Maximized;
        TemaMiniOS.Aplicar(this);
        cboAlgoritmo.Items.AddRange(["OPT · Óptimo", "NRU · No usada recientemente", "FIFO · Primera en entrar", "LRU · Menos usada recientemente"]);
        cboAlgoritmo.SelectedIndex = (int)algoritmo;
        btnAutomatico = Boton("Reproducir", AlternarAutomatico, true);
        btnSiguiente = Boton("Siguiente paso", Avanzar);
        btnTodo = Boton("Ejecutar todo", EjecutarTodo);
        btnExportar = Boton("Exportar simulación CSV", Exportar);
        Controls.Add(ConstruirInterfaz());
        cboAlgoritmo.SelectedIndexChanged += (_, _) => Invalidar();
        numMarcos.ValueChanged += (_, _) => Invalidar();
        numLimpieza.ValueChanged += (_, _) => Invalidar();
        txtCadena.TextChanged += (_, _) => Invalidar();
        temporizador.Tick += (_, _) => Avanzar();
        tabla.CellClick += (_, e) =>
        {
            if (e.ColumnIndex > 0 && simulador is not null && e.ColumnIndex <= simulador.Procesadas)
                MostrarDetalle(simulador.Pasos[e.ColumnIndex - 1]);
        };
        ConfigurarComparacion();
        Reiniciar();
    }

    private Control ConstruirInterfaz()
    {
        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(16) };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int alto in new[] { 52, 146, 46, 42 }) raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        var titulo = Etiqueta("MEMORIA VIRTUAL  /  Reemplazo de páginas", 19, true);
        titulo.TextAlign = ContentAlignment.MiddleLeft;
        raiz.Controls.Add(titulo, 0, 0);

        var configuracion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.White, Padding = new Padding(10, 4, 10, 4) };
        configuracion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int alto in new[] { 61, 22, 30, 23 }) configuracion.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        var ajustes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        ajustes.Controls.Add(Campo("Algoritmo", cboAlgoritmo, 300));
        ajustes.Controls.Add(Campo("Marcos", numMarcos, 110));
        ajustes.Controls.Add(Campo("NRU: limpiar R cada N pasos", numLimpieza, 230));
        var ejemplo = Boton("Cargar ejemplo", CargarEjemplo);
        ejemplo.Margin = new Padding(8, 20, 0, 0);
        ajustes.Controls.Add(ejemplo);
        configuracion.Controls.Add(ajustes, 0, 0);
        configuracion.Controls.Add(Etiqueta("Cadena de referencias a páginas", 9, true), 0, 1);
        configuracion.Controls.Add(txtCadena, 0, 2);
        configuracion.Controls.Add(Etiqueta("Separe con comas o espacios. * = escritura (ej.: 5*). Páginas 0–999999; hasta 200 referencias. NRU: 0 = sin limpieza de R.", 8.5f), 0, 3);
        raiz.Controls.Add(configuracion, 0, 1);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        acciones.Controls.AddRange([btnAutomatico, btnSiguiente, btnTodo, Boton("Reiniciar", Reiniciar), Boton("Comparar los 4", Comparar), btnExportar]);
        raiz.Controls.Add(acciones, 0, 2);
        resumen.BackColor = TemaMiniOS.VerdeClaro;
        resumen.TextAlign = ContentAlignment.MiddleLeft;
        resumen.Padding = new Padding(10, 0, 0, 0);
        raiz.Controls.Add(resumen, 0, 3);

        var tabSimulacion = new TabPage("Tabla de simulación") { BackColor = Color.White, Padding = new Padding(8) };
        var contenido = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        contenido.Controls.Add(regla, 0, 0);
        contenido.Controls.Add(tabla, 0, 1);
        contenido.Controls.Add(Etiqueta("Amarillo: página cargada. Verde: acierto. F: fallo. —: marco vacío. Seleccione una columna calculada para ver su explicación.", 8.5f), 0, 2);
        tabSimulacion.Controls.Add(contenido);
        var tabComparacion = new TabPage("Comparación de algoritmos") { BackColor = Color.White, Padding = new Padding(12) };
        var panelComparacion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        panelComparacion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panelComparacion.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        panelComparacion.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panelComparacion.Controls.Add(estadoComparacion, 0, 0);
        panelComparacion.Controls.Add(comparacion, 0, 1);
        tabComparacion.Controls.Add(panelComparacion);
        pestanas.TabPages.AddRange([tabSimulacion, tabComparacion]);
        raiz.Controls.Add(pestanas, 0, 4);
        var grupo = new GroupBox { Text = "Explicación del paso", Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 6) };
        grupo.Controls.Add(detalle);
        raiz.Controls.Add(grupo, 0, 5);
        return raiz;
    }

    private void Invalidar()
    {
        if (configurando) return;
        Pausar();
        simulador = null;
        tabla.Columns.Clear();
        comparacion.Rows.Clear();
        estadoComparacion.Text = "Pulse Comparar los 4 para calcular la cadena completa con la misma configuración.";
        numLimpieza.Enabled = Algoritmo == AlgoritmoReemplazo.NRU;
        ActualizarRegla();
        ActualizarResumen();
        detalle.Text = "Configuración modificada. Pulse Siguiente paso o Reproducir para iniciar una nueva simulación desde memoria vacía.";
    }

    private bool LeerConfiguracion()
    {
        if (SimuladorMemoriaVirtual.IntentarLeerReferencias(txtCadena.Text, out referencias, out string error)) return true;
        Pausar();
        MessageBox.Show(this, error, "Revise la cadena", MessageBoxButtons.OK, MessageBoxIcon.Information);
        txtCadena.Focus();
        return false;
    }

    private bool Preparar()
    {
        if (simulador is not null) return true;
        if (!LeerConfiguracion()) return false;
        simulador = new SimuladorMemoriaVirtual(referencias, (int)numMarcos.Value, Algoritmo, (int)numLimpieza.Value);
        CrearCuadricula();
        ActualizarResumen();
        return true;
    }

    private void Reiniciar()
    {
        Pausar();
        simulador = null;
        numLimpieza.Enabled = Algoritmo == AlgoritmoReemplazo.NRU;
        ActualizarRegla();
        if (!Preparar()) return;
        pestanas.SelectedIndex = 0;
        detalle.Text = "Memoria vacía. Pulse Siguiente paso para atender la primera referencia. Cada columna conservará el estado al finalizar ese paso.";
    }

    private void CargarEjemplo()
    {
        configurando = true;
        txtCadena.Text = Ejemplo;
        numMarcos.Value = 3;
        numLimpieza.Value = 4;
        configurando = false;
        Invalidar();
        Reiniciar();
    }

    private void ActualizarRegla()
    {
        regla.Text = Algoritmo switch
        {
            AlgoritmoReemplazo.OPT => "OPT reemplaza la página cuyo próximo uso está más lejos en el futuro. Si no vuelve a aparecer, tiene prioridad para salir. Empates: primer marco. Es una referencia teórica para comparar resultados.",
            AlgoritmoReemplazo.FIFO => "FIFO reemplaza la página que entró primero en memoria. Un acierto no modifica el orden de llegada.",
            AlgoritmoReemplazo.LRU => "LRU reemplaza la página cuyo último uso está más lejos en el pasado. Cada acceso, incluso un acierto, actualiza ese último uso.",
            _ => "NRU elige la menor clase: 0=(R0,M0), 1=(R0,M1), 2=(R1,M0), 3=(R1,M1). Empates: primer marco. R se limpia al FINAL de cada N pasos; la tabla muestra ese estado final. M se conserva hasta reemplazar la página."
        };
    }

    private void CrearCuadricula()
    {
        tabla.Columns.Clear();
        tabla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Concepto", HeaderText = "Tiempo / paso", Width = 150, Frozen = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        for (int i = 0; i < referencias.Length; i++)
            tabla.Columns.Add(new DataGridViewTextBoxColumn { Name = $"Paso{i + 1}", HeaderText = (i + 1).ToString(), Width = 78, SortMode = DataGridViewColumnSortMode.NotSortable });
        tabla.Rows.Add("Página solicitada");
        tabla.Rows.Add("Operación");
        for (int i = 0; i < numMarcos.Value; i++)
        {
            tabla.Rows.Add($"Marco {i + 1}");
            if (Algoritmo == AlgoritmoReemplazo.NRU)
            {
                tabla.Rows.Add("    R (referencia)");
                tabla.Rows.Add("    M (modificada)");
                tabla.Rows.Add("    Clase (2R + M)");
            }
        }
        filaResultado = tabla.Rows.Add("Resultado");
        filaVictima = tabla.Rows.Add("Página que sale");
        filaFallos = tabla.Rows.Add("Fallos acumulados");
        filaLimpieza = Algoritmo == AlgoritmoReemplazo.NRU ? tabla.Rows.Add("Limpieza de R") : -1;
        foreach (DataGridViewRow fila in tabla.Rows)
        {
            fila.Height = Algoritmo == AlgoritmoReemplazo.NRU ? 24 : 28;
            fila.Cells[0].Style.BackColor = TemaMiniOS.VerdeClaro;
            fila.Cells[0].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }
        tabla.Rows[0].Frozen = true;
        tabla.Rows[1].Frozen = true;
        for (int i = 0; i < referencias.Length; i++)
        {
            tabla.Rows[0].Cells[i + 1].Value = referencias[i].Pagina;
            tabla.Rows[0].Cells[i + 1].Style.BackColor = Color.FromArgb(233, 239, 229);
            tabla.Rows[1].Cells[i + 1].Value = referencias[i].Escritura ? "Escritura*" : "Lectura";
        }
        tabla.ClearSelection();
    }

    private void Avanzar()
    {
        if (!Preparar()) return;
        var paso = simulador!.Avanzar();
        if (paso is null) { Pausar(); return; }
        pestanas.SelectedIndex = 0;
        PintarPaso(paso);
        MostrarDetalle(paso);
        ActualizarResumen();
        if (simulador.Terminado) Finalizar();
    }

    private void PintarPaso(PasoMemoriaVirtual paso)
    {
        int columna = paso.Tiempo;
        int altoMarco = Algoritmo == AlgoritmoReemplazo.NRU ? 4 : 1;
        for (int i = 0; i < paso.Marcos.Count; i++)
        {
            int fila = 2 + i * altoMarco;
            var marco = paso.Marcos[i];
            tabla.Rows[fila].Cells[columna].Value = marco.Pagina?.ToString() ?? "—";
            if (altoMarco == 4)
            {
                tabla.Rows[fila + 1].Cells[columna].Value = marco.Pagina.HasValue ? (marco.R ? "1" : "0") : "—";
                tabla.Rows[fila + 2].Cells[columna].Value = marco.Pagina.HasValue ? (marco.M ? "1" : "0") : "—";
                tabla.Rows[fila + 3].Cells[columna].Value = marco.Pagina.HasValue ? marco.Clase.ToString() : "—";
            }
            if (i == paso.MarcoAccedido)
                tabla.Rows[fila].Cells[columna].Style.BackColor = paso.Fallo ? Color.FromArgb(255, 238, 173) : Color.FromArgb(211, 234, 209);
        }
        var resultado = tabla.Rows[filaResultado].Cells[columna];
        resultado.Value = paso.Fallo ? "F" : "OK";
        resultado.Style.BackColor = paso.Fallo ? Color.FromArgb(249, 217, 211) : Color.FromArgb(211, 234, 209);
        tabla.Rows[filaVictima].Cells[columna].Value = paso.PaginaReemplazada?.ToString() ?? "—";
        tabla.Rows[filaFallos].Cells[columna].Value = paso.FallosAcumulados;
        if (filaLimpieza >= 0) tabla.Rows[filaLimpieza].Cells[columna].Value = paso.LimpiezaR ? "Sí (final)" : "No";
        if (!tabla.Columns[columna].Displayed) tabla.FirstDisplayedScrollingColumnIndex = columna;
        tabla.ClearSelection();
    }

    private void MostrarDetalle(PasoMemoriaVirtual paso) => detalle.Text =
        $"Paso {paso.Tiempo} · {Algoritmo} · Página {paso.Referencia}\n{paso.Explicacion}";

    private void AlternarAutomatico()
    {
        if (temporizador.Enabled) { Pausar(); return; }
        if (!Preparar() || simulador!.Terminado) return;
        temporizador.Start();
        btnAutomatico.Text = "Pausar";
    }

    private void Pausar()
    {
        temporizador.Stop();
        btnAutomatico.Text = "Reproducir";
    }

    private void EjecutarTodo()
    {
        Pausar();
        if (!Preparar() || simulador!.Terminado) return;
        pestanas.SelectedIndex = 0;
        tabla.SuspendLayout();
        try
        {
            while (!simulador.Terminado) PintarPaso(simulador.Avanzar()!);
        }
        finally { tabla.ResumeLayout(); }
        MostrarDetalle(simulador.Pasos[^1]);
        ActualizarResumen();
        Finalizar();
    }

    private void Finalizar()
    {
        Pausar();
        registrar?.Invoke($"Memoria virtual {Algoritmo}: {simulador!.TotalReferencias} referencias, {simulador.CantidadMarcos} marcos, {simulador.Fallos} fallos y {simulador.Aciertos} aciertos.");
        detalle.AppendText("\nSimulación terminada. Puede revisar las columnas o comparar los cuatro algoritmos.");
    }

    private void ActualizarResumen()
    {
        int procesadas = simulador?.Procesadas ?? 0;
        string frecuencia = procesadas > 0 ? simulador!.FrecuenciaFallos.ToString("P1") : "—";
        string rendimiento = procesadas > 0 ? simulador!.Rendimiento.ToString("P1") : "—";
        resumen.Text = $"{Algoritmo} · Pasos: {procesadas}/{simulador?.TotalReferencias ?? 0}     Fallos: {simulador?.Fallos ?? 0}     Aciertos: {simulador?.Aciertos ?? 0}     Frecuencia de fallos: {frecuencia}     Rendimiento (aciertos): {rendimiento}";
        bool terminado = simulador?.Terminado ?? false;
        btnAutomatico.Enabled = btnSiguiente.Enabled = btnTodo.Enabled = !terminado;
        btnExportar.Enabled = procesadas > 0;
    }

    private void ConfigurarComparacion()
    {
        foreach (string nombre in new[] { "Algoritmo", "Referencias", "Marcos", "Fallos", "Aciertos", "Frecuencia de fallos", "Rendimiento", "Reemplazos" })
            comparacion.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = nombre, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
        comparacion.RowTemplate.Height = 42;
        comparacion.Columns[5].DefaultCellStyle.Format = "P1";
        comparacion.Columns[6].DefaultCellStyle.Format = "P1";
    }

    private void Comparar()
    {
        Pausar();
        if (!LeerConfiguracion()) return;
        comparacion.Rows.Clear();
        foreach (var algoritmo in Enum.GetValues<AlgoritmoReemplazo>())
        {
            var prueba = new SimuladorMemoriaVirtual(referencias, (int)numMarcos.Value, algoritmo, (int)numLimpieza.Value);
            prueba.EjecutarTodo();
            comparacion.Rows.Add(algoritmo.ToString(), prueba.TotalReferencias, prueba.CantidadMarcos, prueba.Fallos,
                prueba.Aciertos, prueba.FrecuenciaFallos, prueba.Rendimiento, prueba.Reemplazos);
        }
        estadoComparacion.Text = $"Cadena completa: {referencias.Length} referencias y {numMarcos.Value} marcos inicialmente vacíos. NRU: limpieza R cada {numLimpieza.Value} pasos (0 = nunca); empate por primer marco.\nFrecuencia = fallos / referencias. Rendimiento = aciertos / referencias. Menos fallos es mejor para esta cadena.";
        comparacion.ClearSelection();
        pestanas.SelectedIndex = 1;
    }

    private void Exportar()
    {
        Pausar();
        if (simulador is null || simulador.Procesadas == 0) return;
        pestanas.SelectedIndex = 0;
        using var dialogo = new SaveFileDialog { Filter = "Tabla CSV (*.csv)|*.csv", FileName = $"MemoriaVirtual-{Algoritmo}.csv", AddExtension = true };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        static string Celda(object? valor) => "\"" + (valor?.ToString() ?? "").Replace("\"", "\"\"") + "\"";
        string separador = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var lineas = new List<string>
        {
            string.Join(separador, new[] { "Algoritmo", Algoritmo.ToString(), "Marcos", numMarcos.Value.ToString(), "Limpieza R cada N pasos", numLimpieza.Value.ToString() }.Select(Celda)),
            string.Join(separador, Enumerable.Range(0, simulador.Procesadas + 1).Select(i => Celda(tabla.Columns[i].HeaderText)))
        };
        foreach (DataGridViewRow fila in tabla.Rows)
            lineas.Add(string.Join(separador, Enumerable.Range(0, simulador.Procesadas + 1).Select(i => Celda(fila.Cells[i].Value))));
        try
        {
            File.WriteAllLines(dialogo.FileName, lineas, new UTF8Encoding(true));
            detalle.Text = $"Tabla exportada: {simulador.Procesadas} pasos calculados de {simulador.TotalReferencias}. Puede abrir el archivo CSV con Excel.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"No se pudo guardar el archivo. Cierre el CSV si está abierto en Excel o elija otra ubicación.\n{ex.Message}", "Exportar tabla", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) temporizador.Dispose();
        base.Dispose(disposing);
    }

    private static Label Etiqueta(string texto, float tamano = 9.5f, bool negrita = false) => new()
    {
        Text = texto, Dock = DockStyle.Fill, AutoSize = false, Margin = new Padding(3),
        Font = new Font("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = TemaMiniOS.VerdeOscuro, TextAlign = ContentAlignment.MiddleLeft
    };

    private static Control Campo(string nombre, Control control, int ancho)
    {
        control.AccessibleName = nombre;
        var panel = new Panel { Width = ancho, Height = 57, Margin = new Padding(0, 0, 8, 0) };
        var etiqueta = Etiqueta(nombre, 8.5f, true);
        etiqueta.Dock = DockStyle.Top;
        etiqueta.Height = 21;
        control.Location = new Point(3, 24);
        panel.Controls.Add(control);
        panel.Controls.Add(etiqueta);
        return panel;
    }

    private static Button Boton(string texto, Action accion, bool principal = false)
    {
        var boton = new Button
        {
            Text = texto, AutoSize = true, MinimumSize = new Size(135, 34), Height = 34,
            Margin = new Padding(3, 6, 6, 3), Padding = new Padding(8, 0, 8, 0),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = principal ? TemaMiniOS.VerdeOscuro : TemaMiniOS.VerdeClaro,
            ForeColor = principal ? Color.White : TemaMiniOS.VerdeOscuro,
            UseVisualStyleBackColor = false
        };
        boton.FlatAppearance.BorderColor = TemaMiniOS.VerdeAzulado;
        boton.Click += (_, _) => accion();
        return boton;
    }

    private static DataGridView CrearTabla()
    {
        var tabla = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect, BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 36, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            GridColor = Color.FromArgb(210, 218, 207), ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
        };
        tabla.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = TemaMiniOS.VerdeOscuro, ForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        tabla.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White, ForeColor = TemaMiniOS.VerdeOscuro,
            SelectionBackColor = TemaMiniOS.VerdeAzulado, SelectionForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9)
        };
        tabla.RowTemplate.Height = 28;
        return tabla;
    }
}
