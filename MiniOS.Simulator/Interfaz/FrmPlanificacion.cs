namespace MiniOS.Simulator;

public sealed class FrmPlanificacion : Form
{
    private readonly Kernel kernel;
    private readonly SimuladorPlanificacion simulador = new(new CPU());
    private readonly List<Proceso> procesosConfigurados = [];
    private readonly System.Windows.Forms.Timer temporizador = new() { Interval = 650 };
    private int siguienteId = 1;

    private readonly ComboBox cboAlgoritmo = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 205
    };

    private readonly TextBox txtNombre = new() { Text = "Proceso", Width = 125 };
    private readonly NumericUpDown numLlegada = new() { Minimum = 0, Maximum = 99, Width = 72 };
    private readonly NumericUpDown numRafaga = new() { Minimum = 1, Maximum = 50, Value = 4, Width = 78 };
    private readonly NumericUpDown numPrioridad = new() { Minimum = 1, Maximum = 10, Value = 1, Width = 72 };
    private readonly NumericUpDown numCola = new() { Minimum = 1, Maximum = 3, Value = 1, Width = 62 };
    private readonly NumericUpDown numQuantum = new()
    {
        Minimum = 1,
        Maximum = 10,
        Value = 2,
        Width = 66,
        Enabled = false
    };
    private readonly NumericUpDown numCupoRam = new()
    {
        Minimum = 1,
        Maximum = 6,
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

    public FrmPlanificacion(Kernel kernel)
    {
        this.kernel = kernel;
        Text = "AMS.OS - Planificación de procesos";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1120, 760);
        ClientSize = new Size(1280, 820);
        TemaMiniOS.Aplicar(this);

        cboAlgoritmo.Items.Add("FCFS - First Come, First Served");
        cboAlgoritmo.Items.Add("SJF - Shortest Job First");
        cboAlgoritmo.Items.Add("Round Robin");
        cboAlgoritmo.Items.Add("Prioridad (1 = más alta)");
        cboAlgoritmo.Items.Add("Colas múltiples (C1 > C2 > C3)");
        cboAlgoritmo.Items.Add("Garantizada (cuota 1/n de CPU)");
        cboAlgoritmo.Items.Add("Dos niveles (RAM + suspendidos)");
        cboAlgoritmo.SelectedIndex = 0;

        ConfigurarTabla();
        btnEjecutar = BotonPrincipal("▶  Ejecutar", TemaMiniOS.Verde, EjecutarPausar);
        Controls.Add(ConstruirInterfaz());

        simulador.Algoritmo = AlgoritmoPlanificacion.FCFS;
        simulador.Quantum = (int)numQuantum.Value;
        simulador.CupoResidentes = (int)numCupoRam.Value;
        simulador.EventoGenerado += Registrar;
        temporizador.Tick += (_, _) => EjecutarTickAutomatico();
        cboAlgoritmo.SelectedIndexChanged += (_, _) => CambiarAlgoritmo();
        numQuantum.ValueChanged += (_, _) => CambiarQuantum();
        numCupoRam.ValueChanged += (_, _) => CambiarCupoRam();

        CargarDesdeKernel();
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
        flujo.Controls.Add(Campo("Cupo RAM", numCupoRam, 88));

        var agregar = BotonSecundario("＋ Agregar", AgregarProceso);
        agregar.Width = 145;
        agregar.Height = 34;
        agregar.Margin = new Padding(8, 23, 4, 4);
        flujo.Controls.Add(agregar);

        var ejemplo = BotonSecundario("Cargar ejemplo", CargarEjemplo);
        ejemplo.Width = 135;
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
            ColumnCount = 1,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 29));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 21));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 23));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 27));

        panel.Controls.Add(Tarjeta("CPU", PanelEtiquetas(lblTiempo, lblCpu, lblRestante, lblQuantum)), 0, 0);
        panel.Controls.Add(Tarjeta("LISTOS / CRITERIO", PanelEtiquetas(lblCola, lblRegla)), 0, 1);
        panel.Controls.Add(Tarjeta("MÉTRICAS", PanelEtiquetas(lblEspera, lblRespuesta, lblRetorno)), 0, 2);
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
            3 => AlgoritmoPlanificacion.Prioridad,
            4 => AlgoritmoPlanificacion.ColasMultiples,
            5 => AlgoritmoPlanificacion.Garantizada,
            6 => AlgoritmoPlanificacion.DosNiveles,
            _ => AlgoritmoPlanificacion.FCFS
        };

        numQuantum.Enabled = simulador.Algoritmo is AlgoritmoPlanificacion.RoundRobin or AlgoritmoPlanificacion.DosNiveles;
        numCupoRam.Enabled = simulador.Algoritmo == AlgoritmoPlanificacion.DosNiveles;
        simulador.Quantum = (int)numQuantum.Value;
        simulador.CupoResidentes = (int)numCupoRam.Value;

        if (procesosConfigurados.Count > 0)
            simulador.CargarProcesos(procesosConfigurados);

        rtbLog.Clear();
        Registrar($"Algoritmo seleccionado: {simulador.NombreAlgoritmo}. Escenario reiniciado.");
        ActualizarVista();
    }

    private void CambiarQuantum()
    {
        simulador.Quantum = (int)numQuantum.Value;
        if (simulador.Algoritmo is not (AlgoritmoPlanificacion.RoundRobin or AlgoritmoPlanificacion.DosNiveles))
            return;

        DetenerAutomatico();
        if (procesosConfigurados.Count > 0)
            simulador.ReiniciarEjecucion();

        rtbLog.Clear();
        Registrar($"Quantum establecido en {simulador.Quantum} para {simulador.NombreAlgoritmo}. Simulación reiniciada.");
        ActualizarVista();
    }

    private void CambiarCupoRam()
    {
        simulador.CupoResidentes = (int)numCupoRam.Value;
        if (simulador.Algoritmo != AlgoritmoPlanificacion.DosNiveles)
            return;

        DetenerAutomatico();
        if (procesosConfigurados.Count > 0)
            simulador.ReiniciarEjecucion();

        rtbLog.Clear();
        Registrar($"Cupo de residentes en RAM establecido en {simulador.CupoResidentes}. Simulación reiniciada.");
        ActualizarVista();
    }

    private void AgregarProceso()
    {
        DetenerAutomatico();

        var nombre = string.IsNullOrWhiteSpace(txtNombre.Text)
            ? $"Proceso {siguienteId}"
            : txtNombre.Text.Trim();

        var rafaga = (int)numRafaga.Value;

        var procesoKernel = kernel.CrearProcesoPlanificacion(
            nombre,
            64,
            (int)numLlegada.Value,
            rafaga,
            (int)numPrioridad.Value,
            (int)numCola.Value);

        if (procesoKernel is null)
        {
            MessageBox.Show(
                this,
                "No hay memoria suficiente para agregar el proceso.",
                "AMS.OS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        procesosConfigurados.Add(ClonarProceso(procesoKernel));

        siguienteId = procesoKernel.Id + 1;

        simulador.CargarProcesos(procesosConfigurados);

        rtbLog.Clear();
        Registrar(
            $"Escenario actualizado. Se agregó P{procesoKernel.Id:00} - {procesoKernel.Nombre}.");

        ActualizarVista();

        txtNombre.Clear();
    }

    private void CargarDesdeKernel()
    {
        DetenerAutomatico();

        procesosConfigurados.Clear();

        foreach (var p in kernel.Procesos.OrderBy(p => p.Id))
        {
            procesosConfigurados.Add(ClonarProceso(p));
        }

        siguienteId = procesosConfigurados.Count == 0
            ? 5
            : procesosConfigurados.Max(p => p.Id) + 1;

        simulador.CargarProcesos(procesosConfigurados);

        rtbLog.Clear();
        Registrar("Procesos de AMS.OS cargados en planificación.");

        ActualizarVista();
    }

    private static Proceso ClonarProceso(Proceso p)
    {
        return new Proceso
        {
            Id = p.Id,
            Nombre = p.Nombre,
            MemoriaMB = p.MemoriaMB,

            TiempoLlegada = p.TiempoLlegada,
            RafagaCPU = p.RafagaCPU,
            TiempoRestante = p.RafagaCPU,
            Prioridad = p.Prioridad,
            Cola = p.Cola,

            Estado = EstadoProceso.Nuevo
        };
    }
    private void CargarEjemplo()
    {
        DetenerAutomatico();

        kernel.RestaurarProcesosBase();

        CargarDesdeKernel();

        rtbLog.Clear();
        Registrar(
            $"Procesos base cargados para {simulador.NombreAlgoritmo}. " +
            "P01-P04 permanecen fijos.");

        ActualizarVista();
    }


    private void EjecutarPaso()
    {
        DetenerAutomatico();
        if (!HayProcesos()) return;

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
        if (!HayProcesos()) return;

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
        if (!HayProcesos()) return;

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
        if (simulador.Procesos.Count > 0) return true;

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

        lblCpu.Text = actual is null ? "CPU: Libre" : $"CPU: P{actual.Id:00} - {actual.Nombre}";
        lblCpu.ForeColor = actual is null ? TemaMiniOS.Verde : TemaMiniOS.VerdeOscuro;
        lblRestante.Text = actual is null ? "Restante: -" : $"Restante: {actual.TiempoRestante} / {actual.RafagaCPU}";

        lblQuantum.Text = simulador.Algoritmo switch
        {
            AlgoritmoPlanificacion.RoundRobin => actual is null
                ? $"Quantum: {simulador.Quantum} (CPU libre)"
                : $"Quantum restante: {simulador.QuantumRestante} / {simulador.Quantum}",
            AlgoritmoPlanificacion.Prioridad => actual is null
                ? "Prioridad: - (1 = más alta)"
                : $"Prioridad actual: {actual.Prioridad} (1 = más alta)",
            AlgoritmoPlanificacion.ColasMultiples => actual is null
                ? "Cola actual: - (C1 es superior)"
                : $"Cola actual: C{actual.Cola} (C1 > C2 > C3)",
            AlgoritmoPlanificacion.Garantizada => actual is null
                ? $"Garantía equitativa entre {Math.Max(1, simulador.ProcesosActivos)} activo(s)"
                : $"CPU={actual.TiempoCpuRecibido} | ideal={simulador.TiempoIdealGarantizado(actual):F2} | r={simulador.RatioGarantizado(actual):F2}",
            AlgoritmoPlanificacion.DosNiveles => actual is null
                ? $"RAM: {simulador.ResidentesDosNiveles.Count}/{simulador.CupoResidentes} | q={simulador.Quantum}"
                : $"RAM: {simulador.ResidentesDosNiveles.Count}/{simulador.CupoResidentes} | q restante={simulador.QuantumRestante}/{simulador.Quantum}",
            _ => "Quantum: no aplica"
        };

        if (simulador.Algoritmo == AlgoritmoPlanificacion.DosNiveles)
        {
            var residentes = simulador.ResidentesDosNiveles
                .Select(p => p == actual ? $"P{p.Id:00}*" : $"P{p.Id:00}")
                .ToList();
            var suspendidos = simulador.SuspendidosDosNiveles.Select(p => $"P{p.Id:00}").ToList();
            lblCola.Text = $"RAM: {(residentes.Count == 0 ? "-" : string.Join("→", residentes))}   Disco: {(suspendidos.Count == 0 ? "-" : string.Join("→", suspendidos))}";
        }
        else if (simulador.ColaListos.Count == 0)
        {
            lblCola.Text = "Listos: vacía";
        }
        else if (simulador.Algoritmo == AlgoritmoPlanificacion.Prioridad)
        {
            lblCola.Text = "Listos: " + string.Join("  →  ",
                simulador.ColaListos.Select(p => $"P{p.Id:00}(pr={p.Prioridad})"));
        }
        else if (simulador.Algoritmo == AlgoritmoPlanificacion.ColasMultiples)
        {
            lblCola.Text = $"C1: {IdsDeCola(1)}   C2: {IdsDeCola(2)}   C3: {IdsDeCola(3)}";
        }
        else if (simulador.Algoritmo == AlgoritmoPlanificacion.Garantizada)
        {
            lblCola.Text = "Listos: " + string.Join("  →  ",
                simulador.ColaListos.Select(p => $"P{p.Id:00}(r={simulador.RatioGarantizado(p):F2})"));
        }
        else
        {
            lblCola.Text = "Listos: " + string.Join("  →  ",
                simulador.ColaListos.Select(p => $"P{p.Id:00}"));
        }

        lblRegla.Text = simulador.Algoritmo switch
        {
            AlgoritmoPlanificacion.SJF => "SJF: menor ráfaga disponible, no apropiativo.",
            AlgoritmoPlanificacion.RoundRobin => $"RR: FIFO, apropiativo, quantum={simulador.Quantum}.",
            AlgoritmoPlanificacion.Prioridad => "Prioridad: 1 es la más alta; apropiativo.",
            AlgoritmoPlanificacion.ColasMultiples => "Colas: C1 > C2 > C3; FIFO por cola; apropiativo entre niveles.",
            AlgoritmoPlanificacion.Garantizada => "Garantizada: acumula cuota 1/n por tick y ejecuta el menor ratio CPU/ideal.",
            AlgoritmoPlanificacion.DosNiveles => "Superior: RAM↔disco. Inferior: RR solo entre residentes.",
            _ => "FCFS: FIFO, no apropiativo."
        };

        lblEspera.Text = $"Espera: {simulador.EsperaPromedio:F2}";
        lblRespuesta.Text = $"Respuesta: {simulador.RespuestaPromedio:F2}";
        lblRetorno.Text = $"Retorno: {simulador.RetornoPromedio:F2}";

        ActualizarTabla();
        ActualizarGantt();
    }

    private string IdsDeCola(int cola)
    {
        var ids = simulador.ColaListos
            .Where(p => p.Cola == cola)
            .Select(p => $"P{p.Id:00}")
            .ToList();
        return ids.Count == 0 ? "-" : string.Join("→", ids);
    }

    private void ActualizarTabla()
    {
        dgvProcesos.Rows.Clear();
        foreach (var p in simulador.Procesos.OrderBy(p => p.Id))
        {
            dgvProcesos.Rows.Add(
                $"P{p.Id:00}", p.Nombre, p.TiempoLlegada, p.RafagaCPU, p.TiempoRestante,
                p.Prioridad, p.Cola, p.Estado,
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
            var procesoSegmento = esCpuLibre
                ? null
                : simulador.Procesos.FirstOrDefault(p => p.Id == segmento.ProcesoId);

            var texto = esCpuLibre
                ? $"LIBRE\n{segmento.Inicio} - {segmento.Fin}"
                : simulador.Algoritmo == AlgoritmoPlanificacion.ColasMultiples && procesoSegmento is not null
                    ? $"P{segmento.ProcesoId:00} [C{procesoSegmento.Cola}]\n{segmento.Inicio} - {segmento.Fin}"
                    : simulador.Algoritmo == AlgoritmoPlanificacion.Garantizada
                        ? $"P{segmento.ProcesoId:00} [G]\n{segmento.Inicio} - {segmento.Fin}"
                        : simulador.Algoritmo == AlgoritmoPlanificacion.DosNiveles
                            ? $"P{segmento.ProcesoId:00} [2N]\n{segmento.Inicio} - {segmento.Fin}"
                            : $"P{segmento.ProcesoId:00}\n{segmento.Inicio} - {segmento.Fin}";

            pnlGantt.Controls.Add(new Label
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
            });
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

    private static TableLayoutPanel PanelEtiquetas(params Label[] etiquetas)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = etiquetas.Length,
            Padding = new Padding(3, 1, 2, 1),
            Margin = Padding.Empty
        };

        for (var i = 0; i < etiquetas.Length; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / etiquetas.Length));
            etiquetas[i].AutoSize = false;
            etiquetas[i].Dock = DockStyle.Fill;
            etiquetas[i].TextAlign = ContentAlignment.MiddleLeft;
            etiquetas[i].Margin = Padding.Empty;
            panel.Controls.Add(etiquetas[i], 0, i);
        }

        return panel;
    }

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
