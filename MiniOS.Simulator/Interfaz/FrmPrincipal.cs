namespace MiniOS.Simulator;

public sealed class FrmPrincipal : Form
{
    private readonly Kernel kernel = new();
    private readonly System.Windows.Forms.Timer reloj = new() { Interval = 1000 };

    private readonly Label lblHora = Texto("00:00:00", 11, true);
    private readonly Label lblKernel = Texto("DETENIDO ●", 10, true, TemaMiniOS.VerdeOscuro);

    private readonly Label lblCpuEstado = Texto("Libre", 12, true, TemaMiniOS.Verde);
    private readonly Label lblProcesoActual = Texto("Proceso actual: Ninguno");
    private readonly Label lblEstadoProceso = Texto("Estado del proceso: -");
    private readonly Label lblUsoCpu = Texto("Uso: 0%");
    private readonly ProgressBar prgCpu = new() { Dock = DockStyle.Top, Height = 12 };

    private readonly DataGridView dgvProcesos = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoGenerateColumns = false,
        BackgroundColor = TemaMiniOS.Blanco,
        BorderStyle = BorderStyle.None
    };
    private readonly Label lblResumenProcesos = Texto("Total: 0 · Ejecutando: 0 · Listos: 0", 8.5f, true);

    private readonly Label lblMemTotal = Texto("Total: 4096 MB");
    private readonly Label lblMemUsada = Texto("Usada: 0 MB");
    private readonly Label lblMemDisponible = Texto("Disponible: 4096 MB");
    private readonly Label lblUsoMemoria = Texto("Uso: 0%");
    private readonly ProgressBar prgMemoria = new() { Dock = DockStyle.Top, Height = 12 };

    private readonly TreeView tvArchivos = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = TemaMiniOS.Blanco,
        ForeColor = TemaMiniOS.VerdeOscuro,
        HideSelection = false
    };
    private readonly ListBox lstDispositivos = Lista();
    private readonly RichTextBox rtbLog = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = TemaMiniOS.VerdeOscuro,
        ForeColor = TemaMiniOS.Fondo,
        Font = new Font("Consolas", 9),
        BorderStyle = BorderStyle.None
    };

    private Button btnIniciar = null!;
    private Button btnDetener = null!;

    public FrmPrincipal()
    {
        Text = "MiniOS Simulator";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 720);
        ClientSize = new Size(1180, 800);
        TemaMiniOS.Aplicar(this);

        ConfigurarTablaProcesos();
        Controls.Add(ConstruirInterfaz());

        tvArchivos.Nodes.Add(kernel.Archivos.CrearArbol());
        tvArchivos.BeforeExpand += (_, e) => kernel.Archivos.CargarHijos(e.Node);

        foreach (var d in kernel.Dispositivos)
            lstDispositivos.Items.Add($"●  {d.Nombre} ({d.Tipo}) - {(d.Disponible ? "OK" : "No disponible")}");

        reloj.Tick += (_, _) =>
        {
            kernel.AvanzarSegundo();
            Actualizar();
        };

        Registrar("Simulador preparado. Presione Iniciar SO.");
        Actualizar();
    }

    private Control ConstruirInterfaz()
    {
        var principal = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = BackColor
        };
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        principal.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        principal.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));

        var titulo = Texto("⚙  SIMULADOR DE SISTEMA OPERATIVO", 20, true, TemaMiniOS.VerdeOscuro);
        titulo.Dock = DockStyle.Fill;
        titulo.TextAlign = ContentAlignment.MiddleCenter;
        principal.Controls.Add(titulo, 0, 0);

        var estado = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            BackColor = TemaMiniOS.VerdeClaro,
            Padding = new Padding(12, 7, 12, 5)
        };
        estado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        estado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        estado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        estado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        estado.Controls.Add(Texto("Hora del sistema:", 10, true), 0, 0);
        estado.Controls.Add(lblHora, 1, 0);
        estado.Controls.Add(Texto("Estado del Kernel:", 10, true), 2, 0);
        estado.Controls.Add(lblKernel, 3, 0);
        principal.Controls.Add(estado, 0, 1);

        var centro = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(0, 8, 0, 4) };
        for (var i = 0; i < 3; i++)
            centro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        centro.Controls.Add(Tarjeta("CPU", PanelCpu()), 0, 0);
        centro.Controls.Add(Tarjeta("PROCESOS", PanelProcesos()), 1, 0);
        centro.Controls.Add(Tarjeta("MEMORIA", PanelMemoria()), 2, 0);
        principal.Controls.Add(centro, 0, 2);

        var inferior = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(0, 4, 0, 8) };
        inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        inferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        inferior.Controls.Add(Tarjeta("SISTEMA DE ARCHIVOS", ConBoton(tvArchivos, "Abrir explorador real", AbrirExploradorArchivos)), 0, 0);
        inferior.Controls.Add(Tarjeta("DISPOSITIVOS DE E/S", ConBoton(lstDispositivos, "Ver detalles", () => AbrirDetalle("Dispositivos de E/S", kernel.Dispositivos))), 1, 0);
        inferior.Controls.Add(Tarjeta("LOG DEL SISTEMA", rtbLog), 2, 0);
        principal.Controls.Add(inferior, 0, 3);

        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(10, 6, 0, 0)
        };
        btnIniciar = Boton("▶  [ Iniciar SO ]", TemaMiniOS.Verde, Iniciar);
        btnDetener = Boton("■  [ Detener ]", TemaMiniOS.VerdeOscuro, Detener);
        botones.Controls.Add(btnIniciar);
        botones.Controls.Add(btnDetener);
        botones.Controls.Add(Boton("↻  [ Reiniciar ]", TemaMiniOS.VerdeAzulado, Reiniciar));
        botones.Controls.Add(Boton("»  [ Avanzar reloj ]", TemaMiniOS.VerdeAzulado, Avanzar));
        botones.Controls.Add(Boton("⚙  [ Configuración ]", TemaMiniOS.VerdeOscuro, () => AbrirDetalle(
            "Configuración",
            new[]
            {
                "Memoria total: 4096 MB",
                "Reloj automático: 1 segundo",
                "Planificación: 7 algoritmos interactivos con ejecución paso a paso",
                "Sistema de archivos: exploración real del equipo; escritura limitada a MiniOS_Sandbox"
            })));
        principal.Controls.Add(Tarjeta("BOTONES PRINCIPALES", botones), 0, 4);
        return principal;
    }

    private Control PanelCpu()
    {
        var p = Vertical();
        p.Controls.Add(Texto("⚙", 27, false, TemaMiniOS.VerdeAzulado));
        p.Controls.Add(lblCpuEstado);
        p.Controls.Add(lblProcesoActual);
        p.Controls.Add(lblEstadoProceso);
        p.Controls.Add(lblUsoCpu);
        p.Controls.Add(prgCpu);
        p.Controls.Add(BotonSecundario("Ver detalles", () => AbrirDetalle(
            "CPU",
            kernel.CPU.ProcesoActual is null
                ? new[] { "CPU libre", "Proceso actual: Ninguno", "Uso: 0%" }
                : new[]
                {
                    $"PID: P{kernel.CPU.ProcesoActual.Id:00}",
                    $"Proceso: {kernel.CPU.ProcesoActual.Nombre}",
                    $"Estado: {kernel.CPU.ProcesoActual.Estado}",
                    $"Uso: {kernel.CPU.Uso}%"
                })));
        return p;
    }

    private Control PanelProcesos()
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5 };
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        p.Controls.Add(dgvProcesos, 0, 0);
        p.Controls.Add(lblResumenProcesos, 0, 1);
        p.Controls.Add(BotonSecundario("＋ Nuevo proceso", NuevoProceso), 0, 2);
        p.Controls.Add(BotonSecundario("⚙ Abrir simulador de planificación", AbrirPlanificacion), 0, 3);
        p.Controls.Add(BotonSecundario("Ver detalles", () => AbrirDetalle("Procesos", kernel.Procesos)), 0, 4);
        return p;
    }

    private Control PanelMemoria()
    {
        var p = Vertical();
        p.Controls.Add(Texto("▣  ▣  ▣  ▣", 22, false, TemaMiniOS.VerdeAzulado));
        p.Controls.Add(lblMemTotal);
        p.Controls.Add(lblMemUsada);
        p.Controls.Add(lblMemDisponible);
        p.Controls.Add(lblUsoMemoria);
        p.Controls.Add(prgMemoria);
        p.Controls.Add(BotonSecundario("Ver detalles", () => AbrirDetalle(
            "Memoria",
            new[] { lblMemTotal.Text, lblMemUsada.Text, lblMemDisponible.Text, lblUsoMemoria.Text })));
        return p;
    }

    private void ConfigurarTablaProcesos()
    {
        dgvProcesos.EnableHeadersVisualStyles = false;
        dgvProcesos.ColumnHeadersDefaultCellStyle.BackColor = TemaMiniOS.VerdeClaro;
        dgvProcesos.ColumnHeadersDefaultCellStyle.ForeColor = TemaMiniOS.VerdeOscuro;
        dgvProcesos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        dgvProcesos.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
        dgvProcesos.DefaultCellStyle.SelectionBackColor = TemaMiniOS.VerdeAzulado;
        dgvProcesos.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvProcesos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgvProcesos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PID", FillWeight = 20 });
        dgvProcesos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Proceso", FillWeight = 45 });
        dgvProcesos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MB", FillWeight = 20 });
        dgvProcesos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", FillWeight = 35 });
    }

    private void Iniciar()
    {
        if (kernel.Estado == EstadoKernel.Ejecutando)
            return;
        kernel.Iniciar();
        reloj.Start();
        Registrar("Kernel inicializado correctamente.");
        Actualizar();
    }

    private void Detener()
    {
        if (kernel.Estado == EstadoKernel.Detenido)
            return;
        kernel.Detener();
        reloj.Stop();
        Registrar("Kernel detenido y CPU liberada.");
        Actualizar();
    }

    private void Reiniciar()
    {
        kernel.Reiniciar();
        reloj.Start();
        Registrar("Sistema reiniciado; recursos restaurados.");
        Actualizar();
    }

    private void Avanzar()
    {
        if (kernel.Estado != EstadoKernel.Ejecutando)
        {
            Registrar("No se puede avanzar: el kernel está detenido.", TemaMiniOS.VerdeClaro);
            return;
        }
        kernel.AvanzarSegundo();
        Registrar("Reloj avanzado manualmente.");
        Actualizar();
    }

    private void NuevoProceso()
    {
        if (kernel.Estado != EstadoKernel.Ejecutando)
        {
            Registrar("Inicie el SO antes de crear procesos.", TemaMiniOS.VerdeClaro);
            return;
        }

        using var dialogo = new FrmNuevoProceso();
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;

        var p = kernel.CrearProceso(dialogo.NombreProceso, dialogo.MemoriaMB);
        if (p is null)
            Registrar("Memoria insuficiente para crear el proceso.", TemaMiniOS.VerdeClaro);
        else
            Registrar($"Proceso {p.Nombre} (P{p.Id:00}) creado con {p.MemoriaMB} MB.");
        Actualizar();
    }

    private void AbrirPlanificacion()
    {
        var relojEstabaActivo = reloj.Enabled;
        if (relojEstabaActivo)
            reloj.Stop();

        Registrar("Simulador de planificación abierto. Algoritmos disponibles: 7.");
        using var ventana = new FrmPlanificacion();
        ventana.ShowDialog(this);

        if (relojEstabaActivo && kernel.Estado == EstadoKernel.Ejecutando)
            reloj.Start();

        Registrar("Simulador de planificación cerrado.");
        Actualizar();
    }

    private void AbrirExploradorArchivos()
    {
        Registrar("Explorador del sistema de archivos real abierto.");
        using var ventana = new FrmExploradorArchivos(kernel.Archivos);
        ventana.ShowDialog(this);
        Registrar("Explorador del sistema de archivos cerrado.");

        tvArchivos.Nodes.Clear();
        tvArchivos.Nodes.Add(kernel.Archivos.CrearArbol());
    }

    private void Actualizar()
    {
        lblHora.Text = kernel.Tiempo.ToString(@"hh\:mm\:ss");
        var ejecutandoKernel = kernel.Estado == EstadoKernel.Ejecutando;
        lblKernel.Text = ejecutandoKernel ? "EJECUTANDO ●" : "DETENIDO ●";
        lblKernel.ForeColor = ejecutandoKernel ? TemaMiniOS.Verde : TemaMiniOS.VerdeOscuro;

        var actual = kernel.CPU.ProcesoActual;
        lblCpuEstado.Text = actual is null ? "Libre" : "Ocupada";
        lblCpuEstado.ForeColor = actual is null ? TemaMiniOS.Verde : TemaMiniOS.VerdeOscuro;
        lblProcesoActual.Text = actual is null
            ? "Proceso actual: Ninguno"
            : $"Proceso actual: P{actual.Id:00} - {actual.Nombre}";
        lblEstadoProceso.Text = $"Estado del proceso: {actual?.Estado.ToString() ?? "-"}";
        lblUsoCpu.Text = $"Uso: {kernel.CPU.Uso}%";
        prgCpu.Value = kernel.CPU.Uso;

        dgvProcesos.Rows.Clear();
        foreach (var p in kernel.Procesos)
            dgvProcesos.Rows.Add($"P{p.Id:00}", p.Nombre, p.MemoriaMB, p.Estado);

        var cantidadEjecutando = kernel.Procesos.Count(p => p.Estado == EstadoProceso.Ejecutando);
        var cantidadListos = kernel.Procesos.Count(p => p.Estado == EstadoProceso.Listo);
        lblResumenProcesos.Text = $"Total: {kernel.Procesos.Count} · Ejecutando: {cantidadEjecutando} · Listos: {cantidadListos}";

        lblMemUsada.Text = $"Usada: {kernel.Memoria.UsadaMB} MB";
        lblMemDisponible.Text = $"Disponible: {kernel.Memoria.DisponibleMB} MB";
        lblUsoMemoria.Text = $"Uso: {kernel.Memoria.Porcentaje}%";
        prgMemoria.Value = kernel.Memoria.Porcentaje;

        btnIniciar.Enabled = !ejecutandoKernel;
        btnDetener.Enabled = ejecutandoKernel;
    }

    private void Registrar(string mensaje, Color? color = null)
    {
        kernel.Registro.Agregar(kernel.Tiempo, mensaje);
        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionColor = color ?? TemaMiniOS.Fondo;
        rtbLog.AppendText($"[{kernel.Tiempo:hh\\:mm\\:ss}] {mensaje}{Environment.NewLine}");
        rtbLog.ScrollToCaret();
    }

    private void AbrirDetalle(string titulo, object datos) => new FrmDetalle(titulo, datos).ShowDialog(this);

    private static GroupBox Tarjeta(string titulo, Control contenido)
    {
        var g = new GroupBox
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            BackColor = TemaMiniOS.Blanco,
            ForeColor = TemaMiniOS.VerdeOscuro,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(10),
            Margin = new Padding(5)
        };
        contenido.Font = new Font("Segoe UI", 9.2f);
        g.Controls.Add(contenido);
        return g;
    }

    private static FlowLayoutPanel Vertical() => new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(14, 5, 5, 5)
    };

    private static Label Texto(string texto, float size = 9.2f, bool bold = false, Color? color = null) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? TemaMiniOS.VerdeOscuro,
        Margin = new Padding(4)
    };

    private static ListBox Lista() => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = TemaMiniOS.Blanco,
        ForeColor = TemaMiniOS.VerdeOscuro,
        IntegralHeight = false
    };

    private static Button Boton(string texto, Color color, Action accion)
    {
        var b = new Button
        {
            Text = texto,
            AutoEllipsis = false,
            Size = new Size(195, 46),
            BackColor = color,
            ForeColor = TemaMiniOS.Blanco,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(6)
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => accion();
        return b;
    }

    private static Button BotonSecundario(string texto, Action accion)
    {
        var b = new Button
        {
            Text = texto,
            Dock = DockStyle.Top,
            Height = 31,
            BackColor = TemaMiniOS.VerdeClaro,
            ForeColor = TemaMiniOS.VerdeOscuro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(4)
        };
        b.FlatAppearance.BorderColor = TemaMiniOS.VerdeAzulado;
        b.Click += (_, _) => accion();
        return b;
    }

    private static Control ConBoton(Control contenido, string texto, Action accion)
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        p.Controls.Add(contenido, 0, 0);
        p.Controls.Add(BotonSecundario(texto, accion), 0, 1);
        return p;
    }
}
