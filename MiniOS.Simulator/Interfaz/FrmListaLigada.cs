using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOS.Simulator;

public sealed class FrmListaLigada : Form
{
    private readonly MemoriaListaLigada memoria =
        new(1024);

    private int siguienteProcesoId = 1;

    // =========================================================
    // CONTROLES
    // =========================================================

    private readonly ComboBox cboAlgoritmo = new()
    {
        DropDownStyle =
            ComboBoxStyle.DropDownList,

        Dock =
            DockStyle.Fill
    };

    private readonly TextBox txtNombre = new()
    {
        Text = "P01",
        Dock = DockStyle.Fill
    };

    private readonly NumericUpDown numTamano = new()
    {
        Minimum = 1,
        Maximum = 1024,
        Value = 100,
        Dock = DockStyle.Fill
    };

    private readonly Label lblTotal =
        new();

    private readonly Label lblUsada =
        new();

    private readonly Label lblLibre =
        new();

    private readonly Label lblHuecos =
        new();

    private readonly Panel pnlRepresentacion =
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

    private readonly DataGridView dgvBloques =
        new()
        {
            Dock = DockStyle.Fill,

            ReadOnly = true,

            AllowUserToAddRows = false,

            AllowUserToDeleteRows = false,

            RowHeadersVisible = false,

            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,

            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill
        };

    private readonly DataGridView dgvProcesos =
        new()
        {
            Dock = DockStyle.Fill,

            ReadOnly = true,

            AllowUserToAddRows = false,

            AllowUserToDeleteRows = false,

            RowHeadersVisible = false,

            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,

            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill
        };

    private readonly RichTextBox rtbLog =
        new()
        {
            Dock = DockStyle.Fill,

            ReadOnly = true,

            BackColor =
                Color.FromArgb(52, 72, 49),

            ForeColor =
                Color.White,

            Font =
                new Font(
                    "Consolas",
                    9f
                )
        };

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public FrmListaLigada()
    {
        Text =
            "AMS.OS - Administración de Memoria - Lista Ligada";

        StartPosition =
            FormStartPosition.CenterParent;

        WindowState =
            FormWindowState.Maximized;

        MinimumSize =
            new Size(1100, 700);

        TemaMiniOS.Aplicar(this);

        ConfigurarControles();

        Controls.Add(
            CrearInterfaz()
        );

        pnlRepresentacion.Paint +=
            DibujarMemoria;

        cboAlgoritmo.Items.AddRange(
            new object[]
            {
                "First Fit",
                "Next Fit",
                "Best Fit",
                "Worst Fit"
            }
        );

        cboAlgoritmo.SelectedIndex = 0;

        ActualizarVista();

        Registrar(
            "Memoria inicializada con 1024 MB libres."
        );
    }

    // =========================================================
    // CONFIGURACIÓN DE TABLAS
    // =========================================================

    private void ConfigurarControles()
    {
        dgvBloques.Columns.Add(
            "Inicio",
            "Inicio"
        );

        dgvBloques.Columns.Add(
            "Tamano",
            "Tamaño"
        );

        dgvBloques.Columns.Add(
            "Estado",
            "Estado"
        );

        dgvBloques.Columns.Add(
            "Proceso",
            "Proceso"
        );

        dgvProcesos.Columns.Add(
            "PID",
            "PID"
        );

        dgvProcesos.Columns.Add(
            "Nombre",
            "Proceso"
        );

        dgvProcesos.Columns.Add(
            "Tamano",
            "Tamaño"
        );
    }

    // =========================================================
    // INTERFAZ PRINCIPAL
    // =========================================================

    private Control CrearInterfaz()
    {
        var raiz =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,

                ColumnCount = 1,

                RowCount = 6,

                Padding =
                    new Padding(18),

                BackColor =
                    TemaMiniOS.Fondo
            };

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                60
            )
        );

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                85
            )
        );

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                105
            )
        );

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                150
            )
        );

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100
            )
        );

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                55
            )
        );

        // TÍTULO
        raiz.Controls.Add(
            CrearTitulo(),
            0,
            0
        );

        // MÉTRICAS
        raiz.Controls.Add(
            CrearResumen(),
            0,
            1
        );

        // CONFIGURACIÓN
        raiz.Controls.Add(
            CrearConfiguracion(),
            0,
            2
        );

        // REPRESENTACIÓN VISUAL
        raiz.Controls.Add(
            CrearPanelVisual(),
            0,
            3
        );

        // TABLAS
        raiz.Controls.Add(
            CrearZonaTablas(),
            0,
            4
        );

        // BOTONES INFERIORES
        raiz.Controls.Add(
            CrearBotonera(),
            0,
            5
        );

        return raiz;
    }

    // =========================================================
    // TÍTULO
    // =========================================================

    private Control CrearTitulo()
    {
        var panel =
            new Panel
            {
                Dock = DockStyle.Fill,

                BackColor =
                    TemaMiniOS.VerdeOscuro
            };

        var titulo =
            new Label
            {
                Text =
                    "☷  ADMINISTRACIÓN DE MEMORIA - LISTA LIGADA",

                Dock =
                    DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Padding =
                    new Padding(20, 0, 0, 0),

                ForeColor =
                    Color.White,

                Font =
                    new Font(
                        "Segoe UI",
                        16f,
                        FontStyle.Bold
                    )
            };

        panel.Controls.Add(titulo);

        return panel;
    }

    // =========================================================
    // RESUMEN
    // =========================================================

    private Control CrearResumen()
    {
        var panel =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,

                ColumnCount = 4,

                RowCount = 1,

                BackColor =
                    TemaMiniOS.Blanco,

                Padding =
                    new Padding(15)
            };

        for (int i = 0; i < 4; i++)
        {
            panel.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    25
                )
            );
        }

        PrepararMetrica(lblTotal);
        PrepararMetrica(lblUsada);
        PrepararMetrica(lblLibre);
        PrepararMetrica(lblHuecos);

        panel.Controls.Add(
            lblTotal,
            0,
            0
        );

        panel.Controls.Add(
            lblUsada,
            1,
            0
        );

        panel.Controls.Add(
            lblLibre,
            2,
            0
        );

        panel.Controls.Add(
            lblHuecos,
            3,
            0
        );

        return panel;
    }

    private static void PrepararMetrica(
        Label label)
    {
        label.Dock =
            DockStyle.Fill;

        label.TextAlign =
            ContentAlignment.MiddleCenter;

        label.Font =
            new Font(
                "Segoe UI",
                11f,
                FontStyle.Bold
            );
    }

    // =========================================================
    // CONFIGURACIÓN
    // =========================================================

    private Control CrearConfiguracion()
    {
        var panel =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,

                ColumnCount = 7,

                RowCount = 2,

                BackColor =
                    TemaMiniOS.Blanco,

                Padding =
                    new Padding(15)
            };

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                18
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                18
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                15
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                17
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                14
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                8
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                10
            )
        );

        panel.Controls.Add(
            CrearEtiqueta("Algoritmo"),
            0,
            0
        );

        panel.Controls.Add(
            CrearEtiqueta("Proceso"),
            1,
            0
        );

        panel.Controls.Add(
            CrearEtiqueta("Tamaño"),
            2,
            0
        );

        panel.Controls.Add(
            cboAlgoritmo,
            0,
            1
        );

        panel.Controls.Add(
            txtNombre,
            1,
            1
        );

        panel.Controls.Add(
            numTamano,
            2,
            1
        );

        panel.Controls.Add(
            CrearEtiqueta("MB"),
            3,
            1
        );

        var agregar =
            CrearBoton(
                "+ Añadir proceso",
                AgregarProceso
            );

        panel.Controls.Add(
            agregar,
            4,
            1
        );

        return panel;
    }

    private static Label CrearEtiqueta(
        string texto)
    {
        return new Label
        {
            Text = texto,

            Dock =
                DockStyle.Fill,

            TextAlign =
                ContentAlignment.MiddleLeft,

            Font =
                new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold
                )
        };
    }

    // =========================================================
    // REPRESENTACIÓN VISUAL
    // =========================================================

    private Control CrearPanelVisual()
    {
        var grupo =
            new GroupBox
            {
                Text =
                    "Representación de memoria",

                Dock =
                    DockStyle.Fill,

                BackColor =
                    TemaMiniOS.Blanco,

                Font =
                    new Font(
                        "Segoe UI",
                        9f,
                        FontStyle.Bold
                    )
            };

        grupo.Controls.Add(
            pnlRepresentacion
        );

        return grupo;
    }

    // =========================================================
    // TABLAS
    // =========================================================

    private Control CrearZonaTablas()
    {
        var zona =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,

                ColumnCount = 2,

                RowCount = 1,

                BackColor =
                    TemaMiniOS.Fondo
            };

        zona.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                65
            )
        );

        zona.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                35
            )
        );

        var grupoBloques =
            new GroupBox
            {
                Text =
                    "Lista ligada de bloques",

                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        9f,
                        FontStyle.Bold
                    )
            };

        grupoBloques.Controls.Add(
            dgvBloques
        );

        var derecha =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,

                ColumnCount = 1,

                RowCount = 2
            };

        derecha.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                55
            )
        );

        derecha.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                45
            )
        );

        var procesos =
            new GroupBox
            {
                Text =
                    "Procesos en memoria",

                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        9f,
                        FontStyle.Bold
                    )
            };

        procesos.Controls.Add(
            dgvProcesos
        );

        var log =
            new GroupBox
            {
                Text =
                    "Registro",

                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        9f,
                        FontStyle.Bold
                    )
            };

        log.Controls.Add(
            rtbLog
        );

        derecha.Controls.Add(
            procesos,
            0,
            0
        );

        derecha.Controls.Add(
            log,
            0,
            1
        );

        zona.Controls.Add(
            grupoBloques,
            0,
            0
        );

        zona.Controls.Add(
            derecha,
            1,
            0
        );

        return zona;
    }

    // =========================================================
    // BOTONERA
    // =========================================================

    private Control CrearBotonera()
    {
        var panel =
            new FlowLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                FlowDirection =
                    FlowDirection.LeftToRight,

                Padding =
                    new Padding(5),

                BackColor =
                    TemaMiniOS.Fondo
            };

        panel.Controls.Add(
            CrearBoton(
                "↻ Reiniciar",
                ReiniciarSimulacion
            )
        );

        panel.Controls.Add(
            CrearBoton(
                "■ Liberar proceso",
                LiberarProceso
            )
        );

        panel.Controls.Add(
            CrearBoton(
                "← Volver",
                Close
            )
        );

        return panel;
    }

    private Button CrearBoton(
        string texto,
        Action accion)
    {
        var boton =
            new Button
            {
                Text = texto,

                Width = 180,

                Height = 36,

                FlatStyle =
                    FlatStyle.Flat,

                BackColor =
                    TemaMiniOS.VerdeClaro,

                ForeColor =
                    TemaMiniOS.VerdeOscuro,

                Font =
                    new Font(
                        "Segoe UI",
                        9f,
                        FontStyle.Bold
                    ),

                Cursor =
                    Cursors.Hand
            };

        boton.Click +=
            (_, _) => accion();

        return boton;
    }

    // =========================================================
    // AGREGAR PROCESO
    // =========================================================

    private void AgregarProceso()
    {
        string nombre =
            txtNombre.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre =
                $"P{siguienteProcesoId:00}";
        }

        int tamano =
            (int)numTamano.Value;

        var algoritmo =
            ObtenerAlgoritmo();

        bool asignado =
            memoria.AsignarProceso(
                siguienteProcesoId,
                nombre,
                tamano,
                algoritmo
            );

        if (!asignado)
        {
            MessageBox.Show(
                this,
                "No existe un bloque libre adecuado para asignar este proceso.",
                "AMS.OS - Lista ligada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            Registrar(
                $"No se pudo asignar {nombre} ({tamano} MB) usando {cboAlgoritmo.Text}."
            );

            return;
        }

        Registrar(
            $"P{siguienteProcesoId:00} - {nombre} ({tamano} MB) asignado mediante {cboAlgoritmo.Text}."
        );

        siguienteProcesoId++;

        txtNombre.Text =
            $"P{siguienteProcesoId:00}";

        ActualizarVista();
    }

    // =========================================================
    // LIBERAR PROCESO
    // =========================================================

    private void LiberarProceso()
    {
        if (dgvProcesos.CurrentRow is null ||
            dgvProcesos.CurrentRow.Tag is not int procesoId)
        {
            MessageBox.Show(
                this,
                "Seleccione un proceso para liberarlo.",
                "AMS.OS - Lista ligada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            return;
        }

        bool liberado =
            memoria.LiberarProceso(
                procesoId
            );

        if (!liberado)
            return;

        Registrar(
            $"P{procesoId:00} liberado. El bloque queda libre sin fusionarse con bloques vecinos."
        );

        ActualizarVista();
    }

    // =========================================================
    // REINICIAR
    // =========================================================

    private void ReiniciarSimulacion()
    {
        memoria.Reiniciar();

        siguienteProcesoId = 1;

        txtNombre.Text = "P01";

        rtbLog.Clear();

        Registrar(
            "Simulación reiniciada. Memoria: 1024 MB libres."
        );

        ActualizarVista();
    }

    // =========================================================
    // OBTENER ALGORITMO
    // =========================================================

    private AlgoritmoAsignacionMemoria ObtenerAlgoritmo()
    {
        return cboAlgoritmo.SelectedIndex switch
        {
            0 =>
                AlgoritmoAsignacionMemoria.FirstFit,

            1 =>
                AlgoritmoAsignacionMemoria.NextFit,

            2 =>
                AlgoritmoAsignacionMemoria.BestFit,

            3 =>
                AlgoritmoAsignacionMemoria.WorstFit,

            _ =>
                AlgoritmoAsignacionMemoria.FirstFit
        };
    }

    // =========================================================
    // ACTUALIZAR INTERFAZ
    // =========================================================

    private void ActualizarVista()
    {
        lblTotal.Text =
            $"Memoria total\n{memoria.MemoriaTotalMB} MB";

        lblUsada.Text =
            $"Memoria usada\n{memoria.MemoriaUsadaMB} MB";

        lblLibre.Text =
            $"Memoria libre\n{memoria.MemoriaLibreMB} MB";

        lblHuecos.Text =
            $"Huecos libres\n{memoria.CantidadHuecosLibres}";

        ActualizarTablaBloques();

        ActualizarTablaProcesos();

        pnlRepresentacion.Invalidate();
    }

    private void ActualizarTablaBloques()
    {
        dgvBloques.Rows.Clear();

        foreach (var bloque in memoria.Bloques)
        {
            dgvBloques.Rows.Add(
                $"{bloque.InicioMB} MB",

                $"{bloque.TamanoMB} MB",

                bloque.Libre
                    ? "Libre"
                    : "Ocupado",

                bloque.Libre
                    ? "-"
                    : $"P{bloque.ProcesoId:00} - {bloque.NombreProceso}"
            );
        }
    }

    private void ActualizarTablaProcesos()
    {
        dgvProcesos.Rows.Clear();

        foreach (var bloque in
                 memoria.Bloques.Where(
                     b => !b.Libre))
        {
            int fila =
                dgvProcesos.Rows.Add(
                    $"P{bloque.ProcesoId:00}",

                    bloque.NombreProceso,

                    $"{bloque.TamanoMB} MB"
                );

            dgvProcesos
                .Rows[fila]
                .Tag =
                bloque.ProcesoId!.Value;
        }
    }

    // =========================================================
    // DIBUJAR MEMORIA
    // =========================================================

    private void DibujarMemoria(
        object? sender,
        PaintEventArgs e)
    {
        e.Graphics.Clear(
            Color.White
        );

        if (memoria.Bloques.Count == 0)
            return;

        var area =
            new Rectangle(
                15,
                20,
                Math.Max(
                    1,
                    pnlRepresentacion.ClientSize.Width - 30
                ),
                Math.Max(
                    1,
                    pnlRepresentacion.ClientSize.Height - 40
                )
            );

        foreach (var bloque in memoria.Bloques)
        {
            float porcentajeInicio =
                (float)bloque.InicioMB /
                memoria.MemoriaTotalMB;

            float porcentajeTamano =
                (float)bloque.TamanoMB /
                memoria.MemoriaTotalMB;

            int x =
                area.Left +
                (int)(
                    area.Width *
                    porcentajeInicio
                );

            int ancho =
                Math.Max(
                    2,
                    (int)(
                        area.Width *
                        porcentajeTamano
                    )
                );

            var rectangulo =
                new Rectangle(
                    x,
                    area.Top,
                    ancho,
                    area.Height
                );

            Color color =
                bloque.Libre
                    ? Color.FromArgb(
                        235,
                        249,
                        235
                    )
                    : Color.FromArgb(
                        248,
                        218,
                        213
                    );

            using var fondo =
                new SolidBrush(color);

            e.Graphics.FillRectangle(
                fondo,
                rectangulo
            );

            using var borde =
                new Pen(
                    TemaMiniOS.VerdeOscuro,
                    1
                );

            e.Graphics.DrawRectangle(
                borde,
                rectangulo
            );

            string texto;

            if (bloque.Libre)
            {
                texto =
                    $"Libre\n{bloque.TamanoMB} MB";
            }
            else
            {
                texto =
                    $"P{bloque.ProcesoId:00}\n{bloque.TamanoMB} MB";
            }

            // Solo dibujamos texto completo cuando existe espacio.
            if (ancho >= 45)
            {
                using var fuente =
                    new Font(
                        "Segoe UI",
                        8f,
                        FontStyle.Bold
                    );

                using var pincel =
                    new SolidBrush(
                        TemaMiniOS.VerdeOscuro
                    );

                var formato =
                    new StringFormat
                    {
                        Alignment =
                            StringAlignment.Center,

                        LineAlignment =
                            StringAlignment.Center
                    };

                e.Graphics.DrawString(
                    texto,
                    fuente,
                    pincel,
                    rectangulo,
                    formato
                );
            }
        }
    }

    // =========================================================
    // LOG
    // =========================================================

    private void Registrar(
        string mensaje)
    {
        rtbLog.AppendText(
            $"[{DateTime.Now:HH:mm:ss}] {mensaje}{Environment.NewLine}"
        );

        rtbLog.ScrollToCaret();
    }
}