namespace MiniOS.Simulator;

public sealed class FrmPlanificacion : Form
{
    private readonly SimuladorPlanificacion simulador = new(new CPU());
    private readonly List<Proceso> procesosConfigurados = [];
    private readonly System.Windows.Forms.Timer temporizador = new() { Interval = 650 };

    private int siguienteId = 1;

    private readonly ComboBox cboAlgoritmo = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 205
    };

    private readonly TextBox txtNombre = new()
    {
        Text = "Proceso",
        Width = 125
    };

    private readonly NumericUpDown numLlegada = new()
    {
        Minimum = 0,
        Maximum = 99,
        Width = 72
    };

    private readonly NumericUpDown numRafaga = new()
    {
        Minimum = 1,
        Maximum = 50,
        Value = 4,
        Width = 78
    };

    private readonly NumericUpDown numPrioridad = new()
    {
        Minimum = 1,
        Maximum = 10,
        Value = 1,
        Width = 72
    };

    private readonly NumericUpDown numCola = new()
    {
        Minimum = 1,
        Maximum = 3,
        Value = 1,
        Width = 62
    };

    private readonly NumericUpDown numQuantum = new()
    {
        Minimum = 1,
        Maximum = 10,
        Value = 2,
        Width = 66,
        Enabled = false
    };

    private readonly DataGridView dgvProcesos = new()
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
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None
    };

    private readonly Label lblTiempo = Etiqueta("Tiempo: 0", 12, true);
    private readonly Label lblCpu = Etiqueta("CPU: Libre", 9.5f, true, TemaMiniOS.Verde);
    private readonly Label lblRestante = Etiqueta("Restante: -", 8.5f);
    private readonly Label lblQuantum = Etiqueta("Quantum: no aplica", 8.5f);
    private readonly Label lblCola = Etiqueta("Listos: vacía", 8.5f);
    private readonly Label lblRegla = Etiqueta("FCFS: FIFO, no apropiativo.", 8.0f);
    private readonly Label lblEspera = Etiqueta("Espera: 0.00", 8.4f);
    private readonly Label lblRespuesta = Etiqueta("Respuesta: 0.00", 8.4f);
    private readonly Label lblRetorno = Etiqueta("Retorno: 0.00", 8.4f);

    private readonly RichTextBox rtbLog = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = TemaMiniOS.VerdeOscuro,
        ForeColor = TemaMiniOS.Fondo,
        Font = new Font("Consolas", 9),
        BorderStyle = BorderStyle.None
    };

    private readonly FlowLayoutPanel pnlGantt = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(6)
    };

    private readonly Button btnEjecutar;

    public FrmPlanificacion()
    {
        Text = "MiniOS - Planificación de procesos";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1120, 760);
        ClientSize = new Size(1280, 820);
        TemaMiniOS.Aplicar(this);

        cboAlgoritmo.Items.Add("FCFS - First Come, First Served");
        cboAlgoritmo.Items.Add("SJF - Shortest Job First");
        cboAlgoritmo.Items.Add("Round Robin");
        cboAlgoritmo.SelectedIndex = 0;

        ConfigurarTabla();
        btnEjecutar = BotonPrincipal("▶  Ejecutar", TemaMiniOS.Verde, EjecutarPausar);

        Controls.Add(ConstruirInterfaz());

        simulador.Algoritmo = AlgoritmoPlanificacion.FCFS;
        simulador.Quantum = (int)numQuantum.Value;
        simulador.EventoGenerado += Registrar;
        temporizador.Tick += (_, _) => EjecutarTickAutomatico();
        cboAlgoritmo.SelectedIndexChanged += (_, _) => CambiarAlgoritmo();
        numQuantum.ValueChanged += (_, _) => CambiarQuantum();

        CargarEjemplo();
    }

    private Control ConstruirInterfaz()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = BackColor
        };

        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));

        var titulo = Etiqueta("⚙  PLANIFICACIÓN DE PROCESOS", 20, true, TemaMiniOS.VerdeOscuro);
        titulo.Dock = DockStyle.Fill;
        titulo.TextAlign = ContentAlignment.MiddleCenter;
        raiz.Controls.Add(titulo, 0, 0);

        raiz.Controls.Add(ConstruirConfiguracion(), 0, 1);
        raiz.Controls.Add(ConstruirCentro(), 0, 2);
        raiz.Controls.Add(Tarjeta("LÍNEA DE TIEMPO / DIAGRAMA DE GANTT", pnlGantt), 0, 3);
        raiz.Controls.Add(ConstruirBotonera(), 0, 4);

        return raiz;
    }

    private Control ConstruirConfiguracion()
    {
        var flujo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(6, 0, 6, 0)
        };

        flujo.Controls.Add(Campo("Algoritmo", cboAlgoritmo, 220));
        flujo.Controls.Add(Campo("Nombre", txtNombre, 135));
        flujo.Controls.Add(Campo("Llegada", numLlegada, 82));
        flujo.Controls.Add(Campo("Ráfaga", numRafaga, 88));
        flujo.Controls.Add(Campo("Prioridad", numPrioridad, 82));
        flujo.Controls.Add(Campo("Cola", numCola, 72));
        flujo.Controls.Add(Campo("Quantum", numQuantum, 80));

        var agregar = BotonSecundario("＋ Agregar proceso", AgregarProceso);
        agregar.Width = 145;
        agregar.Height = 34;
        agregar.Margin = new Padding(8, 23, 4, 4);
        flujo.Controls.Add(agregar);

        var ejemplo = BotonSecundario("Cargar ejemplo", CargarEjemplo);
        ejemplo.Width = 125;
        ejemplo.Height = 34;
        ejemplo.Margin = new Padding(4, 23, 4, 4);
        flujo.Controls.Add(ejemplo);

        var tarjeta = Tarjeta("CONFIGURACIÓN DEL ESCENARIO", flujo);
        tarjeta.Padding = new Padding(8);
        return tarjeta;
    }

    private Control ConstruirCentro()
    {
        var centro = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 4)
        };

        centro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        centro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

        centro.Controls.Add(Tarjeta("PROCESOS", dgvProcesos), 0, 0);
        centro.Controls.Add(ConstruirPanelEstado(), 1, 0);

        return centro;
    }

    private Control ConstruirPanelEstado()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
        };

        // Más espacio para CPU y sus cuatro líneas; métricas compactas para
        // conservar un log suficientemente grande durante la defensa.
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cpu = Vertical();
        cpu.Controls.Add(lblTiempo);
        cpu.Controls.Add(lblCpu);
        cpu.Controls.Add(lblRestante);
        cpu.Controls.Add(lblQuantum);
        panel.Controls.Add(Tarjeta("CPU", cpu), 0, 0);

        var cola = Vertical();
        cola.Controls.Add(lblCola);
        cola.Controls.Add(lblRegla);
        panel.Controls.Add(Tarjeta("LISTOS / CRITERIO", cola), 0, 1);

        var metricas = Vertical();
        metricas.Controls.Add(lblEspera);
        metricas.Controls.Add(lblRespuesta);
        metricas.Controls.Add(lblRetorno);
        panel.Controls.Add(Tarjeta("MÉTRICAS", metricas), 0, 2);

        panel.Controls.Add(Tarjeta("LOG DE PLANIFICACIÓN", rtbLog), 0, 3);

        return panel;
    }

    private Control ConstruirBotonera()
    {
        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(8, 3, 0, 0)
        };

        botones.Controls.Add(BotonPrincipal("→  Paso", TemaMiniOS.VerdeAzulado, EjecutarPaso));
        botones.Controls.Add(btnEjecutar);
        botones.Controls.Add(BotonPrincipal("↻  Reiniciar", TemaMiniOS.VerdeAzulado, ReiniciarEjecucion));
        botones.Controls.Add(BotonPrincipal("⌫  Limpiar", TemaMiniOS.VerdeOscuro, Limpiar));
        botones.Controls.Add(BotonPrincipal("←  Volver", TemaMiniOS.VerdeOscuro, Close));

        return Tarjeta("CONTROLES DE SIMULACIÓN", botones);
    }

    private void ConfigurarTabla()
    {
        dgvProcesos.EnableHeadersVisualStyles = false;
        dgvProcesos.ColumnHeadersDefaultCellStyle.BackColor = TemaMiniOS.VerdeClaro;
        dgvProcesos.ColumnHeadersDefaultCellStyle.ForeColor = TemaMiniOS.VerdeOscuro;
        dgvProcesos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgvProcesos.DefaultCellStyle.SelectionBackColor = TemaMiniOS.VerdeAzulado;
        dgvProcesos.DefaultCellStyle.SelectionForeColor = Color.White;

        AgregarColumna("Id", "ID", 52);
        AgregarColumna("Nombre", "Nombre", 130);
        AgregarColumna("Llegada", "Llegada", 68);
        AgregarColumna("Rafaga", "Ráfaga", 68);
        AgregarColumna("Restante", "Restante", 72);
        AgregarColumna("Prioridad", "Prioridad", 70);
        AgregarColumna("Cola", "Cola", 52);
        AgregarColumna("Estado", "Estado", 82);
        AgregarColumna("Inicio", "Inicio", 60);
        AgregarColumna("Fin", "Fin", 60);
        AgregarColumna("Espera", "Espera", 66);
        AgregarColumna("Respuesta", "Respuesta", 82);
        AgregarColumna("Retorno", "Retorno", 72);
    }

    private void AgregarColumna(string nombre, string encabezado, int ancho)
    {
        dgvProcesos.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nombre,
            HeaderText = encabezado,
            Width = ancho,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void CambiarAlgoritmo()
    {
        DetenerAutomatico();

        simulador.Algoritmo = cboAlgoritmo.SelectedIndex switch
        {
            1 => AlgoritmoPlanificacion.SJF,
            2 => AlgoritmoPlanificacion.RoundRobin,
            _ => AlgoritmoPlanificacion.FCFS
        };

        numQuantum.Enabled = simulador.Algoritmo == AlgoritmoPlanificacion.RoundRobin;
        simulador.Quantum = (int)numQuantum.Value;

        if (procesosConfigurados.Count > 0)
            simulador.CargarProcesos(procesosConfigurados);

        rtbLog.Clear();
        Registrar($"Algoritmo seleccionado: {simulador.NombreAlgoritmo}. Escenario reiniciado.");
        ActualizarVista();
    }

    private void CambiarQuantum()
    {
        simulador.Quantum = (int)numQuantum.Value;

        if (simulador.Algoritmo != AlgoritmoPlanificacion.RoundRobin)
            return;

        DetenerAutomatico();

        if (procesosConfigurados.Count > 0)
            simulador.ReiniciarEjecucion();

        rtbLog.Clear();
        Registrar($"Quantum de Round Robin establecido en {simulador.Quantum}. Simulación reiniciada.");
        ActualizarVista();
    }

    private void AgregarProceso()
    {
        DetenerAutomatico();

        var nombre = string.IsNullOrWhiteSpace(txtNombre.Text)
            ? $"Proceso {siguienteId}"
            : txtNombre.Text.Trim();

        var rafaga = (int)numRafaga.Value;
        procesosConfigurados.Add(new Proceso
        {
            Id = siguienteId++,
            Nombre = nombre,
            MemoriaMB = 64,
            TiempoLlegada = (int)numLlegada.Value,
            RafagaCPU = rafaga,
            TiempoRestante = rafaga,
            Prioridad = (int)numPrioridad.Value,
            Cola = (int)numCola.Value
        });

        simulador.CargarProcesos(procesosConfigurados);
        rtbLog.Clear();
        Registrar($"Escenario actualizado. Se agregó {nombre}.");
        ActualizarVista();
    }

    private void CargarEjemplo()
    {
        DetenerAutomatico();
        procesosConfigurados.Clear();
        siguienteId = 1;

        AgregarProcesoEjemplo("Editor", 0, 5, 2, 1);
        AgregarProcesoEjemplo("Navegador", 1, 3, 1, 1);
        AgregarProcesoEjemplo("Compilador", 2, 4, 3, 2);
        AgregarProcesoEjemplo("Calculadora", 4, 2, 2, 1);

        simulador.CargarProcesos(procesosConfigurados);
        rtbLog.Clear();
        Registrar($"Escenario de ejemplo cargado para {simulador.NombreAlgoritmo}. Use Paso para observar cada decisión.");
        ActualizarVista();
    }

    private void AgregarProcesoEjemplo(string nombre, int llegada, int rafaga, int prioridad, int cola)
    {
        procesosConfigurados.Add(new Proceso
        {
            Id = siguienteId++,
            Nombre = nombre,
            MemoriaMB = 64,
            TiempoLlegada = llegada,
            RafagaCPU = rafaga,
            TiempoRestante = rafaga,
            Prioridad = prioridad,
            Cola = cola
        });
    }

    private void EjecutarPaso()
    {
        DetenerAutomatico();

        if (!HayProcesos())
            return;

        if (simulador.Finalizado)
        {
            MessageBox.Show(this, "La simulación ya finalizó. Presione Reiniciar para ejecutarla nuevamente.",
                "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        simulador.EjecutarPaso();
        ActualizarVista();
    }

    private void EjecutarPausar()
    {
        if (!HayProcesos())
            return;

        if (temporizador.Enabled)
        {
            DetenerAutomatico();
            Registrar("Ejecución automática pausada.");
            return;
        }

        if (simulador.Finalizado)
            ReiniciarEjecucion();

        temporizador.Start();
        btnEjecutar.Text = "Ⅱ  Pausar";
        Registrar($"Ejecución automática {simulador.NombreAlgoritmo} iniciada.");
    }

    private void EjecutarTickAutomatico()
    {
        simulador.EjecutarPaso();
        ActualizarVista();

        if (simulador.Finalizado)
            DetenerAutomatico();
    }

    private void ReiniciarEjecucion()
    {
        DetenerAutomatico();

        if (!HayProcesos())
            return;

        rtbLog.Clear();
        simulador.ReiniciarEjecucion();
        ActualizarVista();
    }

    private void Limpiar()
    {
        DetenerAutomatico();
        procesosConfigurados.Clear();
        siguienteId = 1;
        simulador.Reiniciar();
        rtbLog.Clear();
        Registrar("Escenario vacío. Agregue procesos para iniciar una nueva simulación.");
        ActualizarVista();
    }

    private bool HayProcesos()
    {
        if (simulador.Procesos.Count > 0)
            return true;

        MessageBox.Show(this, "Agregue al menos un proceso antes de ejecutar la simulación.",
            "MiniOS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private void DetenerAutomatico()
    {
        temporizador.Stop();
        btnEjecutar.Text = "▶  Ejecutar";
    }

    private void ActualizarVista()
    {
        lblTiempo.Text = $"Tiempo: {simulador.TiempoActual}";

        var actual = simulador.ProcesoActual;
        lblCpu.Text = actual is null
            ? "CPU: Libre"
            : $"CPU: P{actual.Id:00} - {actual.Nombre}";
        lblCpu.ForeColor = actual is null ? TemaMiniOS.Verde : TemaMiniOS.VerdeOscuro;
        lblRestante.Text = actual is null
            ? "Restante: -"
            : $"Restante: {actual.TiempoRestante} / {actual.RafagaCPU}";

        lblQuantum.Text = simulador.Algoritmo == AlgoritmoPlanificacion.RoundRobin
            ? actual is null
                ? $"Quantum: {simulador.Quantum} (CPU libre)"
                : $"Quantum restante: {simulador.QuantumRestante} / {simulador.Quantum}"
            : "Quantum: no aplica";

        lblCola.Text = simulador.ColaListos.Count == 0
            ? "Listos: vacía"
            : "Listos: " + string.Join("  →  ", simulador.ColaListos.Select(p => $"P{p.Id:00}"));

        lblRegla.Text = simulador.Algoritmo switch
        {
            AlgoritmoPlanificacion.SJF => "SJF: menor ráfaga disponible, no apropiativo.",
            AlgoritmoPlanificacion.RoundRobin => $"RR: FIFO, apropiativo, quantum={simulador.Quantum}.",
            _ => "FCFS: FIFO, no apropiativo."
        };

        lblEspera.Text = $"Espera: {simulador.EsperaPromedio:F2}";
        lblRespuesta.Text = $"Respuesta: {simulador.RespuestaPromedio:F2}";
        lblRetorno.Text = $"Retorno: {simulador.RetornoPromedio:F2}";

        ActualizarTabla();
        ActualizarGantt();
    }

    private void ActualizarTabla()
    {
        dgvProcesos.Rows.Clear();

        foreach (var p in simulador.Procesos.OrderBy(p => p.Id))
        {
            dgvProcesos.Rows.Add(
                $"P{p.Id:00}",
                p.Nombre,
                p.TiempoLlegada,
                p.RafagaCPU,
                p.TiempoRestante,
                p.Prioridad,
                p.Cola,
                p.Estado,
                p.TiempoInicio?.ToString() ?? "-",
                p.TiempoFinalizacion?.ToString() ?? "-",
                p.TiempoEspera,
                p.TiempoInicio is null ? "-" : p.TiempoRespuesta,
                p.TiempoFinalizacion is null ? "-" : p.TiempoRetorno);
        }
    }

    private void ActualizarGantt()
    {
        pnlGantt.SuspendLayout();
        pnlGantt.Controls.Clear();

        if (simulador.LineaTiempo.Count == 0)
        {
            pnlGantt.Controls.Add(Etiqueta("Sin ejecución todavía. Presione Paso o Ejecutar.", 9.5f));
            pnlGantt.ResumeLayout();
            return;
        }

        foreach (var segmento in simulador.LineaTiempo)
        {
            var duracion = segmento.Fin - segmento.Inicio;
            var esCpuLibre = segmento.ProcesoId == 0;
            var texto = esCpuLibre
                ? $"LIBRE\n{segmento.Inicio} - {segmento.Fin}"
                : $"P{segmento.ProcesoId:00}\n{segmento.Inicio} - {segmento.Fin}";

            var bloque = new Label
            {
                Text = texto,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Width = Math.Max(72, duracion * 36),
                Height = 54,
                Margin = new Padding(3),
                BackColor = esCpuLibre ? TemaMiniOS.Fondo : TemaMiniOS.VerdeClaro,
                ForeColor = TemaMiniOS.VerdeOscuro,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            pnlGantt.Controls.Add(bloque);
        }

        pnlGantt.ResumeLayout();
        pnlGantt.ScrollControlIntoView(pnlGantt.Controls[^1]);
    }

    private void Registrar(string mensaje)
    {
        rtbLog.AppendText($"[{simulador.TiempoActual:000}] {mensaje}{Environment.NewLine}");
        rtbLog.ScrollToCaret();
    }

    private static GroupBox Tarjeta(string titulo, Control contenido)
    {
        var tarjeta = new GroupBox
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            BackColor = TemaMiniOS.Blanco,
            ForeColor = TemaMiniOS.VerdeOscuro,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(8),
            Margin = new Padding(4)
        };

        contenido.Font = new Font("Segoe UI", 9.2f);
        tarjeta.Controls.Add(contenido);
        return tarjeta;
    }

    private static FlowLayoutPanel Vertical() => new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(4, 1, 1, 1)
    };

    private static Control Campo(string titulo, Control control, int ancho)
    {
        var panel = new TableLayoutPanel
        {
            Width = ancho,
            Height = 60,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(4)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.Controls.Add(Etiqueta(titulo, 8.6f, true), 0, 0);
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    private static Label Etiqueta(string texto, float size = 9.2f, bool bold = false, Color? color = null) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? TemaMiniOS.VerdeOscuro,
        Margin = new Padding(1)
    };

    private static Button BotonPrincipal(string texto, Color color, Action accion)
    {
        var boton = new Button
        {
            Text = texto,
            Size = new Size(170, 36),
            BackColor = color,
            ForeColor = TemaMiniOS.Blanco,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 2, 6, 2)
        };
        boton.FlatAppearance.BorderSize = 0;
        boton.Click += (_, _) => accion();
        return boton;
    }

    private static Button BotonSecundario(string texto, Action accion)
    {
        var boton = new Button
        {
            Text = texto,
            BackColor = TemaMiniOS.VerdeClaro,
            ForeColor = TemaMiniOS.VerdeOscuro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        boton.FlatAppearance.BorderColor = TemaMiniOS.VerdeAzulado;
        boton.Click += (_, _) => accion();
        return boton;
    }
}
