using System.Text;

namespace MiniOS.Simulator;

public sealed class FrmMemoriaVirtual : Form
{
    private const string Ejemplo = "2, 3, 2, 1, 5, 2, 4, 5, 3, 2, 5, 2";
    private const string EjemploNru = "2, 3, 2, 1*, 5, 2, 4, 5*, 3, 2, 5, 2";
    private readonly ComboBox cboAlgoritmo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280, DropDownWidth = 320 };
    private readonly NumericUpDown numMarcos = new() { Minimum = 1, Maximum = SimuladorMemoriaVirtual.MaxMarcos, Value = 3, Width = 85 };
    private readonly NumericUpDown numLimpieza = new() { Minimum = 0, Maximum = 200, Value = 4, Width = 120 };
    private readonly TextBox txtCadena = new() { Dock = DockStyle.Fill, MaxLength = 2400, AccessibleName = "Cadena de referencias a páginas" };
    private readonly DataGridView tabla = CrearTabla();
    private readonly DataGridView comparacion = CrearTabla();
    private readonly Label resumen = Etiqueta("", 10, true);
    private readonly Label regla = Etiqueta("", 9);
    private readonly Label ayudaCadena = Etiqueta("", 8.5f);
    private readonly Label estadoVacio = Etiqueta("", 12);
    private Control campoLimpieza = null!;
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
    private readonly Button btnComparar;
    private readonly Action<string>? registrar;
    private SimuladorMemoriaVirtual? simulador;
    private ReferenciaPagina[] referencias = [];
    private bool configurando;
    private int filaResultado;
    private int filaVictima;
    private int filaFallos;
    private int filaLimpieza;

    private AlgoritmoReemplazo Algoritmo => (AlgoritmoReemplazo)cboAlgoritmo.SelectedIndex;
    private bool EsNru => Algoritmo == AlgoritmoReemplazo.NRU;
    private int ColumnasPorPaso => EsNru ? 3 : 1;

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
        btnComparar = Boton("Comparar los 4", Comparar);
        Controls.Add(ConstruirInterfaz());
        cboAlgoritmo.SelectedIndexChanged += (_, _) => CambiarAlgoritmo();
        numMarcos.ValueChanged += (_, _) => Invalidar();
        numLimpieza.ValueChanged += (_, _) => Invalidar();
        txtCadena.TextChanged += (_, _) => Invalidar();
        txtCadena.KeyPress += (_, e) =>
        {
            if (e.KeyChar == '*' && !EsNru)
            {
                e.Handled = true;
                ayudaCadena.Text = "La escritura (*) solo se utiliza en NRU. Para este algoritmo, escriba únicamente números de página.";
            }
        };
        temporizador.Tick += (_, _) => Avanzar();
        tabla.CellClick += (_, e) =>
        {
            int paso = (e.ColumnIndex - 1) / ColumnasPorPaso;
            if (e.ColumnIndex > 0 && simulador is not null && paso < simulador.Procesadas)
                MostrarDetalle(simulador.Pasos[paso]);
        };
        tabla.CellPainting += PintarEncabezado;
        tabla.Paint += PintarGrupos;
        tabla.Scroll += (_, _) => tabla.Invalidate();
        ConfigurarComparacion();
        Invalidar();
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
        var encabezado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        var volver = Boton("← Volver", () => { Pausar(); Close(); }, true);
        volver.BackColor = TemaMiniOS.Verde;
        volver.AccessibleName = "Volver a la ventana anterior";
        CancelButton = volver;
        encabezado.Controls.Add(titulo, 0, 0);
        encabezado.Controls.Add(volver, 1, 0);
        raiz.Controls.Add(encabezado, 0, 0);

        var configuracion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.White, Padding = new Padding(10, 4, 10, 4) };
        configuracion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int alto in new[] { 61, 22, 30, 23 }) configuracion.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        var ajustes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        ajustes.Controls.Add(Campo("Algoritmo", cboAlgoritmo, 300));
        ajustes.Controls.Add(Campo("Marcos", numMarcos, 110));
        campoLimpieza = Campo("NRU: limpiar R cada N pasos", numLimpieza, 230);
        ajustes.Controls.Add(campoLimpieza);
        var ejemplo = Boton("Cargar ejemplo", CargarEjemplo);
        ejemplo.Margin = new Padding(8, 20, 0, 0);
        ajustes.Controls.Add(ejemplo);
        configuracion.Controls.Add(ajustes, 0, 0);
        configuracion.Controls.Add(Etiqueta("Cadena de referencias a páginas", 9, true), 0, 1);
        configuracion.Controls.Add(txtCadena, 0, 2);
        configuracion.Controls.Add(ayudaCadena, 0, 3);
        raiz.Controls.Add(configuracion, 0, 1);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        acciones.Controls.AddRange([btnAutomatico, btnSiguiente, btnTodo, Boton("Reiniciar", Reiniciar), btnComparar, btnExportar]);
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
        var superficie = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        estadoVacio.TextAlign = ContentAlignment.MiddleCenter;
        superficie.Controls.Add(tabla);
        superficie.Controls.Add(estadoVacio);
        contenido.Controls.Add(superficie, 0, 1);
        contenido.Controls.Add(Etiqueta("Amarillo: página cargada. Verde: acierto. F: fallo. —: marco vacío. Seleccione un paso calculado para ver su explicación.", 8.5f), 0, 2);
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
        referencias = [];
        tabla.Columns.Clear();
        tabla.Visible = false;
        estadoVacio.Visible = true;
        comparacion.Rows.Clear();
        estadoComparacion.Text = "Pulse Comparar los 4 para calcular la cadena completa con la misma configuración.";
        campoLimpieza.Visible = EsNru;
        numLimpieza.Enabled = EsNru;
        txtCadena.PlaceholderText = EsNru ? "Escriba las páginas; use * para una escritura. Ej.: 2, 3, 1*" : "Escriba las páginas. Ej.: 2, 3, 1";
        ayudaCadena.Text = EsNru
            ? "Separe con comas o espacios. * = escritura (ej.: 5*). Páginas 0–999999; hasta 200 referencias. NRU: 0 = sin limpieza de R."
            : "Separe con comas o espacios. Solo números de página (sin marcas de escritura). Páginas 0–999999; hasta 200 referencias.";
        bool vacia = string.IsNullOrWhiteSpace(txtCadena.Text);
        bool valida = SimuladorMemoriaVirtual.IntentarLeerReferencias(txtCadena.Text, Algoritmo, out _, out string error);
        ayudaCadena.ForeColor = !vacia && !valida ? Color.Firebrick : TemaMiniOS.VerdeOscuro;
        txtCadena.BackColor = !vacia && !valida ? Color.FromArgb(255, 243, 240) : Color.White;
        if (!vacia && !valida) ayudaCadena.Text = error;
        estadoVacio.Text = vacia ? "Escriba una cadena de páginas para comenzar.\nTambién puede usar «Cargar ejemplo»."
            : valida ? "Cadena lista.\nPulse «Siguiente paso», «Reproducir» o «Ejecutar todo»."
            : "Revise la cadena de páginas.\nLa indicación aparece debajo del campo de entrada.";
        ActualizarRegla();
        ActualizarResumen();
        detalle.Text = vacia ? "La simulación comienza con memoria vacía. Ingrese las páginas y elija el número de marcos."
            : "Cada paso mostrará el estado de la memoria después de atender una referencia. Seleccione un paso calculado para consultar su explicación.";
    }

    private void CambiarAlgoritmo()
    {
        bool quitarEscrituras = !EsNru && txtCadena.Text.Contains('*') &&
            SimuladorMemoriaVirtual.IntentarLeerReferencias(txtCadena.Text, out _, out _);
        if (quitarEscrituras)
        {
            configurando = true;
            txtCadena.Text = txtCadena.Text.Replace("*", "");
            configurando = false;
        }
        Invalidar();
        if (quitarEscrituras)
            detalle.Text = $"Se quitaron las marcas de escritura al cambiar a {Algoritmo}. Se conservan las mismas páginas y su orden; la escritura solo se configura en NRU.";
    }

    private bool LeerConfiguracion()
    {
        if (SimuladorMemoriaVirtual.IntentarLeerReferencias(txtCadena.Text, Algoritmo, out referencias, out string error)) return true;
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
        Invalidar();
        if (string.IsNullOrWhiteSpace(txtCadena.Text)) return;
        if (!Preparar()) return;
        pestanas.SelectedIndex = 0;
        detalle.Text = "Memoria vacía. Pulse Siguiente paso para atender la primera referencia. Cada paso conservará el estado final de sus marcos.";
    }

    private void CargarEjemplo()
    {
        configurando = true;
        txtCadena.Text = EsNru ? EjemploNru : Ejemplo;
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
        tabla.ColumnHeadersHeight = 56;
        tabla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Concepto", HeaderText = "Tiempo / paso", Width = 150, Frozen = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        for (int i = 0; i < referencias.Length; i++)
        {
            int anchoPagina = EsNru ? Math.Max(64, TextRenderer.MeasureText(referencias[i].ToString(),
                tabla.ColumnHeadersDefaultCellStyle.Font, Size.Empty, TextFormatFlags.NoPadding).Width + 16) : 85;
            for (int parte = 0; parte < ColumnasPorPaso; parte++)
            {
                string dato = parte == 0 ? "Página" : parte == 1 ? "R" : "M";
                tabla.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = $"Paso{i + 1}_{dato}",
                    HeaderText = EsNru ? $"Paso {i + 1} · ref. {referencias[i]} · {dato}" : $"Paso {i + 1} · ref. {referencias[i].Pagina}",
                    ToolTipText = $"Paso {i + 1}: página solicitada {referencias[i]}" + (EsNru ? $" · {(referencias[i].Escritura ? "Escritura" : "Lectura")} · {dato}" : ""),
                    Width = parte == 0 ? anchoPagina : 34,
                    Resizable = DataGridViewTriState.False, SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }
        }
        for (int i = 0; i < numMarcos.Value; i++) tabla.Rows.Add($"Marco {i + 1}");
        filaResultado = tabla.Rows.Add("Resultado");
        filaVictima = tabla.Rows.Add("Página que sale");
        filaFallos = tabla.Rows.Add("Fallos acumulados");
        filaLimpieza = EsNru ? tabla.Rows.Add("Limpieza de R") : -1;
        foreach (DataGridViewRow fila in tabla.Rows)
        {
            fila.Height = 32;
            fila.Cells[0].Style.BackColor = TemaMiniOS.VerdeClaro;
            fila.Cells[0].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            for (int col = 1; col < tabla.ColumnCount; col++)
                fila.Cells[col].Style.BackColor = ((col - 1) / ColumnasPorPaso) % 2 == 0 ? Color.White : Color.FromArgb(245, 248, 242);
        }
        tabla.ClearSelection();
        estadoVacio.Visible = false;
        tabla.Visible = true;
    }

    // El paso y la referencia se leen en filas separadas; NRU conserva R y M al lado de la página.
    private void PintarEncabezado(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.Graphics is null) return;
        e.PaintBackground(e.ClipBounds, false);
        int paso = e.ColumnIndex == 0 ? -1 : (e.ColumnIndex - 1) / ColumnasPorPaso;
        int parte = e.ColumnIndex == 0 ? 0 : (e.ColumnIndex - 1) % ColumnasPorPaso;
        var superior = new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width, 28);
        var inferior = new Rectangle(e.CellBounds.X, e.CellBounds.Y + 28, e.CellBounds.Width, e.CellBounds.Height - 28);
        var estado = e.Graphics.Save();
        e.Graphics.SetClip(e.ClipBounds);
        try
        {
            using var fondo = new SolidBrush(e.ColumnIndex == 0 || parte > 0 ? TemaMiniOS.VerdeClaro : Color.FromArgb(233, 239, 229));
            using var borde = new Pen(tabla.GridColor);
            e.Graphics.FillRectangle(fondo, inferior);
            e.Graphics.DrawRectangle(borde, inferior.X, inferior.Y, inferior.Width - 1, inferior.Height - 1);
            var formato = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping;
            if (e.ColumnIndex == 0 || !EsNru)
            {
                TextRenderer.DrawText(e.Graphics, e.ColumnIndex == 0 ? "Tiempo / paso" : (paso + 1).ToString(),
                    tabla.ColumnHeadersDefaultCellStyle.Font, superior, Color.White, formato);
                e.Graphics.DrawRectangle(borde, superior.X, superior.Y, superior.Width - 1, superior.Height - 1);
            }
            string texto = e.ColumnIndex == 0 ? "Página solicitada" : parte == 0 ? referencias[paso].ToString() : parte == 1 ? "R" : "M";
            TextRenderer.DrawText(e.Graphics, texto, e.ColumnIndex == 0 ? tabla.DefaultCellStyle.Font : tabla.ColumnHeadersDefaultCellStyle.Font,
                inferior, TemaMiniOS.VerdeOscuro, formato);
        }
        finally { e.Graphics.Restore(estado); }
        e.Handled = true;
    }

    private void PintarGrupos(object? sender, PaintEventArgs e)
    {
        if (!EsNru || tabla.ColumnCount < 4) return;
        Rectangle fijo = tabla.GetCellDisplayRectangle(0, -1, true);
        var zona = new Rectangle(fijo.Right, fijo.Top, Math.Max(0, tabla.ClientSize.Width - fijo.Right), 28);
        var estado = e.Graphics.Save();
        e.Graphics.SetClip(zona);
        try
        {
            int x = fijo.Right - tabla.HorizontalScrollingOffset;
            using var fondo = new SolidBrush(TemaMiniOS.VerdeOscuro);
            using var borde = new Pen(tabla.GridColor);
            for (int paso = 0; paso < referencias.Length; paso++)
            {
                int col = 1 + paso * 3;
                int ancho = tabla.Columns[col].Width + tabla.Columns[col + 1].Width + tabla.Columns[col + 2].Width;
                var rectangulo = new Rectangle(x, fijo.Top, ancho, 28);
                if (rectangulo.IntersectsWith(zona))
                {
                    e.Graphics.FillRectangle(fondo, rectangulo);
                    e.Graphics.DrawRectangle(borde, x, fijo.Top, ancho - 1, 27);
                    TextRenderer.DrawText(e.Graphics, (paso + 1).ToString(),
                        tabla.ColumnHeadersDefaultCellStyle.Font, rectangulo, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
                }
                x += ancho;
            }
        }
        finally { e.Graphics.Restore(estado); }
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
        int columna = 1 + (paso.Tiempo - 1) * ColumnasPorPaso;
        for (int i = 0; i < paso.Marcos.Count; i++)
        {
            var marco = paso.Marcos[i];
            tabla.Rows[i].Cells[columna].Value = marco.Pagina?.ToString() ?? "—";
            if (EsNru)
            {
                tabla.Rows[i].Cells[columna + 1].Value = marco.Pagina.HasValue ? (marco.R ? "1" : "0") : "—";
                tabla.Rows[i].Cells[columna + 2].Value = marco.Pagina.HasValue ? (marco.M ? "1" : "0") : "—";
                for (int parte = 0; parte < 3; parte++)
                    tabla.Rows[i].Cells[columna + parte].ToolTipText = marco.Pagina.HasValue
                        ? $"Marco {i + 1}: página {marco.Pagina}, R={(marco.R ? 1 : 0)}, M={(marco.M ? 1 : 0)}. Clase {marco.Clase} (2R + M). Estado al final del paso {paso.Tiempo}."
                        : "Marco vacío";
            }
            if (i == paso.MarcoAccedido)
                tabla.Rows[i].Cells[columna].Style.BackColor = paso.Fallo ? Color.FromArgb(255, 238, 173) : Color.FromArgb(211, 234, 209);
        }
        var resultado = tabla.Rows[filaResultado].Cells[columna];
        resultado.Value = paso.Fallo ? "F" : "OK";
        for (int parte = 0; parte < ColumnasPorPaso; parte++)
            tabla.Rows[filaResultado].Cells[columna + parte].Style.BackColor = paso.Fallo ? Color.FromArgb(249, 217, 211) : Color.FromArgb(211, 234, 209);
        tabla.Rows[filaVictima].Cells[columna].Value = paso.PaginaReemplazada?.ToString() ?? "—";
        tabla.Rows[filaFallos].Cells[columna].Value = paso.FallosAcumulados;
        if (filaLimpieza >= 0) tabla.Rows[filaLimpieza].Cells[columna].Value = paso.LimpiezaR ? "Sí" : "No";
        int ultima = columna + ColumnasPorPaso - 1;
        Rectangle borde = tabla.GetCellDisplayRectangle(ultima, -1, false);
        if (!tabla.Columns[ultima].Displayed || borde.Right > tabla.ClientSize.Width - SystemInformation.VerticalScrollBarWidth)
            tabla.FirstDisplayedScrollingColumnIndex = columna;
        tabla.ClearSelection();
    }

    private void MostrarDetalle(PasoMemoriaVirtual paso)
    {
        detalle.Text = $"Paso {paso.Tiempo} · {Algoritmo} · Página {paso.Referencia}\n{paso.Explicacion}";
        if (EsNru)
            detalle.AppendText("\nClases al final del paso: " + string.Join(" · ", paso.Marcos.Select((marco, i) =>
                $"Marco {i + 1}: {(marco.Pagina.HasValue ? marco.Clase.ToString() : "vacío")}")));
    }

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
        bool valida = SimuladorMemoriaVirtual.IntentarLeerReferencias(txtCadena.Text, Algoritmo, out _, out _);
        btnAutomatico.Enabled = btnSiguiente.Enabled = btnTodo.Enabled = valida && !terminado;
        btnComparar.Enabled = valida;
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
            var cadena = algoritmo == AlgoritmoReemplazo.NRU ? referencias : referencias.Select(r => new ReferenciaPagina(r.Pagina));
            var prueba = new SimuladorMemoriaVirtual(cadena, (int)numMarcos.Value, algoritmo, (int)numLimpieza.Value);
            prueba.EjecutarTodo();
            comparacion.Rows.Add(algoritmo.ToString(), prueba.TotalReferencias, prueba.CantidadMarcos, prueba.Fallos,
                prueba.Aciertos, prueba.FrecuenciaFallos, prueba.Rendimiento, prueba.Reemplazos);
        }
        estadoComparacion.Text = $"Mismas {referencias.Length} páginas y {numMarcos.Value} marcos vacíos. Solo NRU utiliza las escrituras; limpieza R cada {numLimpieza.Value} pasos (0 = nunca), empate por primer marco.\nFrecuencia = fallos / referencias. Rendimiento = aciertos / referencias. Menos fallos es mejor para esta cadena.";
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
        var metadatos = new List<string> { "Algoritmo", Algoritmo.ToString(), "Marcos", numMarcos.Value.ToString() };
        if (EsNru) metadatos.AddRange(["Limpieza R cada N pasos", numLimpieza.Value.ToString()]);
        int columnas = 1 + simulador.Procesadas * ColumnasPorPaso;
        var lineas = new List<string>
        {
            string.Join(separador, metadatos.Select(Celda)),
            string.Join(separador, Enumerable.Range(0, columnas).Select(i => Celda(i == 0 ? "Tiempo / paso" :
                (i - 1) % ColumnasPorPaso == 0 ? ((i - 1) / ColumnasPorPaso + 1).ToString() : ""))),
            string.Join(separador, Enumerable.Range(0, columnas).Select(i => Celda(i == 0 ? "Página solicitada" :
                (i - 1) % ColumnasPorPaso == 0 ? referencias[(i - 1) / ColumnasPorPaso].ToString() :
                (i - 1) % ColumnasPorPaso == 1 ? "R" : "M")))
        };
        foreach (DataGridViewRow fila in tabla.Rows)
            lineas.Add(string.Join(separador, Enumerable.Range(0, columnas).Select(i => Celda(fila.Cells[i].Value))));
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
            WrapMode = DataGridViewTriState.True,
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
