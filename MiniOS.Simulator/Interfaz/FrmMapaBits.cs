using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MiniOS.Simulator;

public sealed class FrmMapaBits : Form
{
    private readonly Kernel kernel;

    // =========================================================
    // RESUMEN
    // =========================================================

    private readonly Label lblTotal;
    private readonly Label lblBloque;
    private readonly Label lblBloques;
    private readonly Label lblUsada;
    private readonly Label lblLibre;
    private readonly Label lblFragmentacion;

    // =========================================================
    // UNIDAD DE ASIGNACIÓN
    // =========================================================

    private readonly NumericUpDown numUnidadAsignacion =
        new()
        {
            Minimum = 1,
            Maximum = 1024,
            Value = 4,
            Width = 90,
            Height = 32
        };

    private readonly Button btnAplicarUnidad =
        new()
        {
            Text = "Aplicar",
            Width = 110,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

    // =========================================================
    // AÑADIR PROCESO
    // =========================================================

    private readonly TextBox txtNombreProceso =
        new()
        {
            Width = 150
        };

    private readonly NumericUpDown numTamanoProceso =
        new()
        {
            Minimum = 1,
            Maximum = 1024,
            Value = 100,
            Width = 110
        };

    private readonly Label lblCasillasNecesarias =
        new()
        {
            AutoSize = true,
            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold
            ),
            ForeColor = Color.DarkSlateGray
        };

    // =========================================================
    // MAPA
    // =========================================================

    private readonly TableLayoutPanel tablaBloques;

    private readonly TextBox txtMapaBits;

    private readonly DataGridView dgvProcesos;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public FrmMapaBits(Kernel kernel)
    {
        this.kernel = kernel;

        Text =
            "AMS.OS - Administración de Memoria";

        StartPosition =
            FormStartPosition.CenterParent;

        MinimumSize =
            new Size(1100, 700);

        ClientSize =
            new Size(1250, 780);

        WindowState =
            FormWindowState.Maximized;

        TemaMiniOS.Aplicar(this);

        // =====================================================
        // RESUMEN
        // =====================================================

        lblTotal = CrearValor();
        lblBloque = CrearValor();
        lblBloques = CrearValor();
        lblUsada = CrearValor();
        lblLibre = CrearValor();
        lblFragmentacion = CrearValor();

        // =====================================================
        // CUADRÍCULA
        // =====================================================

        tablaBloques =
            new TableLayoutPanel
            {
                AutoSize = true,

                AutoSizeMode =
                    AutoSizeMode.GrowAndShrink,

                Dock =
                    DockStyle.Top,

                Padding =
                    new Padding(8),

                BackColor =
                    TemaMiniOS.Blanco
            };

        // =====================================================
        // MAPA BINARIO
        // =====================================================

        txtMapaBits =
            new TextBox
            {
                Dock =
                    DockStyle.Fill,

                ReadOnly =
                    true,

                Font =
                    new Font(
                        "Consolas",
                        10,
                        FontStyle.Bold
                    ),

                BackColor =
                    TemaMiniOS.Blanco,

                ForeColor =
                    TemaMiniOS.VerdeOscuro,

                ScrollBars =
                    ScrollBars.Horizontal,

                WordWrap =
                    false
            };

        // =====================================================
        // TABLA DE PROCESOS
        // =====================================================

        dgvProcesos =
            CrearTablaProcesos();

        // =====================================================
        // CONFIGURACIÓN DE UNIDAD
        // =====================================================

        numUnidadAsignacion.Value =
            kernel.Memoria.UnidadAsignacionMB;

        btnAplicarUnidad.BackColor =
            TemaMiniOS.VerdeClaro;

        btnAplicarUnidad.ForeColor =
            TemaMiniOS.VerdeOscuro;

        btnAplicarUnidad
            .FlatAppearance
            .BorderColor =
            TemaMiniOS.VerdeAzulado;

        btnAplicarUnidad.Click +=
            (_, _) =>
                AplicarUnidadAsignacion();

        numUnidadAsignacion.ValueChanged +=
            (_, _) =>
                ActualizarCasillasNecesarias();

        // =====================================================
        // NUEVO PROCESO
        // =====================================================

        txtNombreProceso.Text =
            ObtenerNombreSugerido();

        numTamanoProceso.ValueChanged +=
            (_, _) =>
                ActualizarCasillasNecesarias();

        // =====================================================

        Controls.Add(
            ConstruirInterfaz()
        );

        ActualizarCasillasNecesarias();

        ActualizarVista();
    }

    // =========================================================
    // INTERFAZ PRINCIPAL
    // =========================================================

    private Control ConstruirInterfaz()
    {
        var principal =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 6,
                BackColor = BackColor
            };

        // Encabezado
        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                65
            )
        );

        // Resumen
        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                105
            )
        );

        // Unidad de asignación
        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                55
            )
        );

        // Añadir proceso
        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                120
            )
        );

        // Zona central
        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100
            )
        );

        // Botones
        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                65
            )
        );

        principal.Controls.Add(
            CrearEncabezado(),
            0,
            0
        );

        principal.Controls.Add(
            CrearResumen(),
            0,
            1
        );

        principal.Controls.Add(
            CrearConfiguracionUnidad(),
            0,
            2
        );

        principal.Controls.Add(
            CrearPanelAgregarProceso(),
            0,
            3
        );

        principal.Controls.Add(
            CrearZonaCentral(),
            0,
            4
        );

        principal.Controls.Add(
            CrearBotones(),
            0,
            5
        );

        return principal;
    }

    // =========================================================
    // ENCABEZADO
    // =========================================================

    private Control CrearEncabezado()
    {
        var panel =
            new Panel
            {
                Dock =
                    DockStyle.Fill,

                BackColor =
                    TemaMiniOS.VerdeOscuro,

                Padding =
                    new Padding(
                        18,
                        10,
                        18,
                        10
                    )
            };

        var titulo =
            new Label
            {
                Text =
                    "▦  ADMINISTRACIÓN DE MEMORIA - MAPA DE BITS",

                Dock =
                    DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleLeft,

                ForeColor =
                    Color.White,

                Font =
                    new Font(
                        "Segoe UI",
                        16,
                        FontStyle.Bold
                    )
            };

        panel.Controls.Add(
            titulo
        );

        return panel;
    }

    // =========================================================
    // RESUMEN
    // =========================================================

    private Control CrearResumen()
    {
        var resumen =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    6,

                RowCount =
                    1,

                Padding =
                    new Padding(5),

                BackColor =
                    TemaMiniOS.Blanco
            };

        for (int i = 0; i < 6; i++)
        {
            resumen.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    16.66f
                )
            );
        }

        resumen.Controls.Add(
            CrearDato(
                "Memoria total",
                lblTotal
            ),
            0,
            0
        );

        resumen.Controls.Add(
            CrearDato(
                "Unidad de asignación",
                lblBloque
            ),
            1,
            0
        );

        resumen.Controls.Add(
            CrearDato(
                "Casillas",
                lblBloques
            ),
            2,
            0
        );

        resumen.Controls.Add(
            CrearDato(
                "Memoria usada",
                lblUsada
            ),
            3,
            0
        );

        resumen.Controls.Add(
            CrearDato(
                "Memoria libre",
                lblLibre
            ),
            4,
            0
        );

        resumen.Controls.Add(
            CrearDato(
                "Fragmentación",
                lblFragmentacion
            ),
            5,
            0
        );

        return resumen;
    }

    private Control CrearDato(
        string titulo,
        Label valor)
    {
        var panel =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                RowCount =
                    2,

                Padding =
                    new Padding(5)
            };

        panel.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                45
            )
        );

        panel.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                55
            )
        );

        var lblTitulo =
            new Label
            {
                Text =
                    titulo,

                Dock =
                    DockStyle.Fill,

                TextAlign =
                    ContentAlignment.BottomCenter,

                Font =
                    new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Regular
                    ),

                ForeColor =
                    TemaMiniOS.VerdeOscuro
            };

        panel.Controls.Add(
            lblTitulo,
            0,
            0
        );

        panel.Controls.Add(
            valor,
            0,
            1
        );

        return panel;
    }

    private static Label CrearValor()
    {
        return new Label
        {
            Dock =
                DockStyle.Fill,

            TextAlign =
                ContentAlignment.TopCenter,

            Font =
                new Font(
                    "Segoe UI",
                    13,
                    FontStyle.Bold
                )
        };
    }

    // =========================================================
    // CONFIGURACIÓN DE UNIDAD
    // =========================================================

    private Control CrearConfiguracionUnidad()
    {
        var panel =
            new FlowLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    false,

                Padding =
                    new Padding(
                        15,
                        8,
                        15,
                        5
                    ),

                BackColor =
                    TemaMiniOS.Blanco
            };

        var titulo =
            new Label
            {
                Text =
                    "Unidad de asignación:",

                AutoSize =
                    true,

                Margin =
                    new Padding(
                        0,
                        7,
                        10,
                        0
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Bold
                    ),

                ForeColor =
                    TemaMiniOS.VerdeOscuro
            };

        var unidad =
            new Label
            {
                Text =
                    "MB",

                AutoSize =
                    true,

                Margin =
                    new Padding(
                        5,
                        7,
                        15,
                        0
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        9
                    ),

                ForeColor =
                    TemaMiniOS.VerdeOscuro
            };

        panel.Controls.Add(
            titulo
        );

        panel.Controls.Add(
            numUnidadAsignacion
        );

        panel.Controls.Add(
            unidad
        );

        panel.Controls.Add(
            btnAplicarUnidad
        );

        return panel;
    }

    // =========================================================
    // AÑADIR PROCESO
    // =========================================================

    private Control CrearPanelAgregarProceso()
    {
        var grupo =
            new GroupBox
            {
                Text = "Añadir proceso",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = TemaMiniOS.VerdeOscuro,
                BackColor = TemaMiniOS.Blanco,
                Padding = new Padding(12, 10, 12, 10)
            };

        var layout =
            new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 2,
                BackColor = TemaMiniOS.Blanco,
                Margin = new Padding(0)
            };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // lbl Proceso
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));  // txt Proceso
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // lbl Tamaño
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));  // num Tamaño
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));   // MB
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Casillas
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));  // Botón

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var lblProceso =
            new Label
            {
                Text = "Proceso",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 8, 0)
            };

        var lblTamano =
            new Label
            {
                Text = "Tamaño",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 8, 0)
            };

        txtNombreProceso.Dock = DockStyle.Fill;
        txtNombreProceso.Margin = new Padding(0, 4, 12, 4);
        txtNombreProceso.Font = new Font("Segoe UI", 9);

        numTamanoProceso.Dock = DockStyle.Fill;
        numTamanoProceso.Margin = new Padding(0, 4, 6, 4);
        numTamanoProceso.Font = new Font("Segoe UI", 9);

        var lblMb =
            new Label
            {
                Text = "MB",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 6, 8, 0)
            };

        lblCasillasNecesarias.Dock = DockStyle.Fill;
        lblCasillasNecesarias.TextAlign = ContentAlignment.MiddleLeft;
        lblCasillasNecesarias.Margin = new Padding(8, 8, 8, 0);
        lblCasillasNecesarias.MinimumSize = new Size(320, 0);
        lblCasillasNecesarias.AutoEllipsis = true;
        lblCasillasNecesarias.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        var btnAgregar =
            new Button
            {
                Text = "+ Añadir proceso",
                Dock = DockStyle.Fill,
                BackColor = TemaMiniOS.VerdeClaro,
                ForeColor = TemaMiniOS.VerdeOscuro,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 3, 0, 3)
            };

        btnAgregar.FlatAppearance.BorderColor =
            TemaMiniOS.VerdeAzulado;

        btnAgregar.Click +=
            (_, _) => AgregarProcesoDesdeMapa();

        layout.Controls.Add(lblProceso, 0, 0);
        layout.Controls.Add(lblTamano, 2, 0);

        layout.Controls.Add(txtNombreProceso, 1, 1);
        layout.Controls.Add(numTamanoProceso, 3, 1);
        layout.Controls.Add(lblMb, 4, 1);
        layout.Controls.Add(lblCasillasNecesarias, 5, 1);
        layout.Controls.Add(btnAgregar, 6, 1);

        grupo.Controls.Add(layout);

        return grupo;
    }

    // =========================================================
    // ZONA CENTRAL
    // =========================================================

    private Control CrearZonaCentral()
    {
        var split =
            new SplitContainer
            {
                Dock =
                    DockStyle.Fill,

                Orientation =
                    Orientation.Vertical,

                SplitterWidth =
                    6,

                BackColor =
                    TemaMiniOS.Fondo
            };

        split.Panel1.Padding =
            new Padding(
                0,
                8,
                5,
                0
            );

        split.Panel2.Padding =
            new Padding(
                5,
                8,
                0,
                0
            );

        split.Panel1.Controls.Add(
            CrearPanelMapa()
        );

        split.Panel2.Controls.Add(
            CrearPanelProcesos()
        );

        split.SizeChanged +=
            (_, _) =>
            {
                if (
                    split.ClientSize.Width <=
                    0)
                {
                    return;
                }

                int distancia =
                    (int)(
                        split.ClientSize.Width *
                        0.45
                    );

                int minimoIzquierdo =
                    500;

                int minimoDerecho =
                    500;

                int maximo =
                    split.ClientSize.Width -
                    minimoDerecho -
                    split.SplitterWidth;

                if (
                    maximo <
                    minimoIzquierdo)
                {
                    return;
                }

                distancia =
                    Math.Clamp(
                        distancia,
                        minimoIzquierdo,
                        maximo
                    );

                split.SplitterDistance =
                    distancia;
            };

        return split;
    }

    // =========================================================
    // MAPA
    // =========================================================

    private Control CrearPanelMapa()
    {
        var grupo =
            new GroupBox
            {
                Text =
                    "Mapa de bits",

                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        10,
                        FontStyle.Bold
                    ),

                ForeColor =
                    TemaMiniOS.VerdeOscuro,

                BackColor =
                    TemaMiniOS.Blanco,

                Padding =
                    new Padding(10)
            };

        var layout =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                RowCount =
                    2,

                ColumnCount =
                    1
            };

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100
            )
        );

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                70
            )
        );

        var scroll =
            new Panel
            {
                Dock =
                    DockStyle.Fill,

                AutoScroll =
                    true,

                BackColor =
                    TemaMiniOS.Blanco
            };

        scroll.Controls.Add(
            tablaBloques
        );

        var bitsPanel =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                RowCount =
                    2,

                Padding =
                    new Padding(4)
            };

        bitsPanel.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                23
            )
        );

        bitsPanel.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100
            )
        );

        bitsPanel.Controls.Add(
            new Label
            {
                Text =
                    "Representación binaria: 0 = libre | 1 = ocupado",

                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Bold
                    ),

                ForeColor =
                    TemaMiniOS.VerdeOscuro
            },
            0,
            0
        );

        bitsPanel.Controls.Add(
            txtMapaBits,
            0,
            1
        );

        layout.Controls.Add(
            scroll,
            0,
            0
        );

        layout.Controls.Add(
            bitsPanel,
            0,
            1
        );

        grupo.Controls.Add(
            layout
        );

        return grupo;
    }

    // =========================================================
    // PROCESOS EN MEMORIA
    // =========================================================

    private Control CrearPanelProcesos()
    {
        var grupo =
            new GroupBox
            {
                Text =
                    "Procesos en memoria",

                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        10,
                        FontStyle.Bold
                    ),

                ForeColor =
                    TemaMiniOS.VerdeOscuro,

                BackColor =
                    TemaMiniOS.Blanco,

                Padding =
                    new Padding(10)
            };

        grupo.Controls.Add(
            dgvProcesos
        );

        return grupo;
    }

    private DataGridView CrearTablaProcesos()
    {
        var tabla =
            new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = TemaMiniOS.Blanco,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.Gainsboro,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 30 }
            };

        tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
        tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        tabla.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9.5f, FontStyle.Bold);
        tabla.ColumnHeadersDefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

        tabla.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);
        tabla.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(0, 120, 215);
        tabla.DefaultCellStyle.SelectionForeColor =
            Color.White;

        tabla.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(248, 248, 248);

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "PID",
                FillWeight = 15
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Proceso",
                FillWeight = 28
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Solicitada",
                FillWeight = 22
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Asignada",
                FillWeight = 22
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Casillas",
                FillWeight = 32
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Fragmentación",
                FillWeight = 24
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Estado",
                FillWeight = 24
            }
        );

        return tabla;
    }

    // =========================================================
    // BOTONES
    // =========================================================

    private Control CrearBotones()
    {
        var botones =
            new FlowLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                FlowDirection =
                    FlowDirection.LeftToRight,

                Padding =
                    new Padding(
                        5,
                        8,
                        5,
                        5
                    )
            };

        botones.Controls.Add(
            CrearBoton(
                "↻ Actualizar",
                TemaMiniOS.VerdeAzulado,
                ActualizarVista
            )
        );

        botones.Controls.Add(
            CrearBoton(
                "■ Finalizar proceso seleccionado",
                TemaMiniOS.VerdeOscuro,
                FinalizarSeleccionado
            )
        );

        botones.Controls.Add(
            CrearBoton(
                "← Volver",
                TemaMiniOS.Verde,
                Close
            )
        );

        return botones;
    }

    private static Button CrearBoton(
        string texto,
        Color color,
        Action accion)
    {
        var boton =
            new Button
            {
                Text =
                    texto,

                Size =
                    new Size(
                        220,
                        40
                    ),

                BackColor =
                    color,

                ForeColor =
                    Color.White,

                FlatStyle =
                    FlatStyle.Flat,

                Font =
                    new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Bold
                    ),

                Cursor =
                    Cursors.Hand,

                Margin =
                    new Padding(5)
            };

        boton.FlatAppearance.BorderSize =
            0;

        boton.Click +=
            (_, _) =>
                accion();

        return boton;
    }

    // =========================================================
    // AÑADIR PROCESO
    // =========================================================

    private void AgregarProcesoDesdeMapa()
    {
        // La unidad escrita arriba debe haber sido aplicada
        // antes de utilizarla para crear procesos.
        if (
            (int)numUnidadAsignacion.Value !=
            kernel.Memoria.UnidadAsignacionMB)
        {
            MessageBox.Show(
                this,

                "Has cambiado el valor de la unidad de asignación, " +
                "pero todavía no lo has aplicado.\n\n" +
                "Presiona primero el botón Aplicar.",

                "AMS.OS - Unidad de asignación",

                MessageBoxButtons.OK,

                MessageBoxIcon.Information
            );

            return;
        }

        if (
            kernel.Estado !=
            EstadoKernel.Ejecutando)
        {
            MessageBox.Show(
                this,

                "Debes iniciar el sistema antes de añadir un proceso.",

                "AMS.OS - Mapa de bits",

                MessageBoxButtons.OK,

                MessageBoxIcon.Information
            );

            return;
        }

        string nombre =
            txtNombreProceso.Text.Trim();

        if (
            string.IsNullOrWhiteSpace(
                nombre))
        {
            nombre =
                ObtenerNombreSugerido();
        }

        int tamano =
            (int)
            numTamanoProceso.Value;

        int unidad =
            kernel.Memoria
                .UnidadAsignacionMB;

        int casillasNecesarias =
            (int)Math.Ceiling(
                tamano /
                (double)unidad
            );

        int memoriaAsignada =
            casillasNecesarias *
            unidad;

        int fragmentacion =
            memoriaAsignada -
            tamano;

        var proceso =
            kernel.CrearProceso(
                nombre,
                tamano
            );

        if (proceso is null)
        {
            MessageBox.Show(
                this,

                "No fue posible añadir el proceso.\n\n" +
                "Verifica que exista suficiente memoria libre.",

                "AMS.OS - Mapa de bits",

                MessageBoxButtons.OK,

                MessageBoxIcon.Warning
            );

            return;
        }

        ActualizarVista();

        MessageBox.Show(
            this,

            $"Proceso añadido correctamente.\n\n" +
            $"PID: P{proceso.Id:00}\n" +
            $"Proceso: {proceso.Nombre}\n" +
            $"Tamaño solicitado: {tamano} MB\n" +
            $"Unidad de asignación: {unidad} MB\n" +
            $"Casillas requeridas: {casillasNecesarias}\n" +
            $"Memoria asignada: {memoriaAsignada} MB\n" +
            $"Fragmentación interna: {fragmentacion} MB",

            "AMS.OS - Proceso añadido",

            MessageBoxButtons.OK,

            MessageBoxIcon.Information
        );

        txtNombreProceso.Text =
            ObtenerNombreSugerido();

        numTamanoProceso.Value =
            Math.Min(
                100,
                numTamanoProceso.Maximum
            );

        ActualizarCasillasNecesarias();
    }

    private void ActualizarCasillasNecesarias()
    {
        int tamano =
            (int)numTamanoProceso.Value;

        int unidad =
            (int)numUnidadAsignacion.Value;

        if (unidad <= 0)
            unidad = 1;

        int casillas =
            (int)Math.Ceiling(
                tamano / (double)unidad
            );

        int asignada =
            casillas * unidad;

        int fragmentacion =
            asignada - tamano;

        lblCasillasNecesarias.Text =
            $"Casillas requeridas: {casillas}   |   " +
            $"Asignada: {asignada} MB   |   " +
            $"Fragmentación: {fragmentacion} MB";
    }

    private string ObtenerNombreSugerido()
    {
        int siguiente =
            kernel.Procesos.Count == 0
                ? 1
                : kernel.Procesos.Max(
                    p => p.Id
                ) + 1;

        return
            $"Proceso {siguiente}";
    }

    // =========================================================
    // ACTUALIZAR
    // =========================================================

    private void ActualizarVista()
    {
        lblTotal.Text =
            $"{kernel.Memoria.TotalMB} MB";

        lblBloque.Text =
            $"{kernel.Memoria.UnidadAsignacionMB} MB";

        lblBloques.Text =
            $"{kernel.Memoria.TotalBloques}";

        lblUsada.Text =
            $"{kernel.Memoria.UsadaMB} MB";

        lblLibre.Text =
            $"{kernel.Memoria.DisponibleMB} MB";

        lblFragmentacion.Text =
            $"{kernel.Memoria.FragmentacionInternaMB} MB";

        lblUsada.ForeColor =
            Color.Firebrick;

        lblLibre.ForeColor =
            Color.ForestGreen;

        lblFragmentacion.ForeColor =
            TemaMiniOS.VerdeOscuro;

        CrearBloques();

        ActualizarRepresentacionBinaria();

        ActualizarProcesos();

        ActualizarCasillasNecesarias();
    }

    // =========================================================
    // REPRESENTACIÓN BINARIA
    // =========================================================

    private void ActualizarRepresentacionBinaria()
    {
        txtMapaBits.Text =
            string.Join(
                " ",
                Enumerable
                    .Range(
                        0,
                        kernel.Memoria.TotalBloques
                    )
                    .Select(
                        i =>
                            kernel.Memoria.EstaOcupado(i)
                                ? "1"
                                : "0"
                    )
            );
    }

    // =========================================================
    // CUADRÍCULA DINÁMICA
    // =========================================================

    private void CrearBloques()
    {
        tablaBloques.SuspendLayout();

        tablaBloques.Controls.Clear();

        tablaBloques.ColumnStyles.Clear();
        tablaBloques.RowStyles.Clear();

        int total =
            kernel.Memoria.TotalBloques;

        // =========================================================
        // CANTIDAD DE COLUMNAS
        // =========================================================

        int columnas;

        if (total <= 64)
        {
            columnas = 8;
        }
        else if (total <= 256)
        {
            columnas = 16;
        }
        else
        {
            columnas = 32;
        }

        columnas =
            Math.Min(
                columnas,
                Math.Max(
                    1,
                    total
                )
            );

        int filas =
            (int)Math.Ceiling(
                total /
                (double)columnas
            );

        tablaBloques.ColumnCount =
            columnas;

        tablaBloques.RowCount =
            filas;

        tablaBloques.AutoSize =
            true;

        tablaBloques.AutoSizeMode =
            AutoSizeMode.GrowAndShrink;

        tablaBloques.Dock =
            DockStyle.Top;

        tablaBloques.Padding =
            new Padding(8);

        // =========================================================
        // TAMAÑO DE LAS COLUMNAS
        // =========================================================

        for (
            int columna = 0;
            columna < columnas;
            columna++)
        {
            tablaBloques.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    82
                )
            );
        }

        // =========================================================
        // TAMAÑO DE LAS FILAS
        // =========================================================

        for (
            int fila = 0;
            fila < filas;
            fila++)
        {
            tablaBloques.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    66
                )
            );
        }

        // =========================================================
        // CREAR CASILLAS
        // =========================================================

        for (
            int i = 0;
            i < total;
            i++)
        {
            bool ocupado =
                kernel.Memoria
                    .EstaOcupado(i);

            int? propietario =
                kernel.Memoria
                    .ObtenerPropietarioBloque(i);

            string nombre =
                "Libre";

            // =====================================================
            // BUSCAR EL PROCESO DUEÑO DE LA CASILLA
            // =====================================================

            if (propietario.HasValue)
            {
                var proceso =
                    kernel.Procesos
                        .FirstOrDefault(
                            p =>
                                p.Id ==
                                propietario.Value
                        );

                if (proceso is not null)
                {
                    nombre =
                        proceso.Nombre;
                }
                else
                {
                    nombre =
                        $"P{propietario.Value:00}";
                }
            }

            // =====================================================
            // CREAR EL CUADRO
            // =====================================================

            var bloque =
                new Label
                {
                    Dock =
                        DockStyle.Fill,

                    Margin =
                        new Padding(3),

                    Padding =
                        new Padding(2),

                    TextAlign =
                        ContentAlignment.MiddleCenter,

                    BorderStyle =
                        BorderStyle.FixedSingle,

                    Font =
                        new Font(
                            "Segoe UI",
                            7.5f,
                            FontStyle.Bold
                        ),

                    // IMPORTANTE:
                    // El nombre vuelve a estar en una línea aparte.
                    Text =
                        ocupado
                            ? $"{i}\n1 ·\n{nombre}"
                            : $"{i}\n0 · Libre",

                    BackColor =
                        ocupado
                            ? Color.MistyRose
                            : Color.Honeydew,

                    ForeColor =
                        ocupado
                            ? Color.DarkRed
                            : Color.DarkGreen
                };

            int fila =
                i / columnas;

            int columna =
                i % columnas;

            tablaBloques.Controls.Add(
                bloque,
                columna,
                fila
            );
        }

        tablaBloques.ResumeLayout();
    }


    // =========================================================
    // TABLA DE PROCESOS
    // =========================================================

    private void ActualizarProcesos()
    {
        dgvProcesos.Rows.Clear();

        foreach (
            var proceso in
            kernel.Procesos)
        {
            var bloques =
                kernel.Memoria
                    .ObtenerBloquesProceso(
                        proceso.Id
                    );

            if (bloques.Count == 0)
                continue;

            string textoBloques =
                FormatearBloques(
                    bloques
                );

            int memoriaAsignada =
                bloques.Count *
                kernel.Memoria
                    .UnidadAsignacionMB;

            int fragmentacion =
                Math.Max(
                    0,
                    memoriaAsignada -
                    proceso.MemoriaMB
                );

            int fila =
                dgvProcesos.Rows.Add(
                    $"P{proceso.Id:00}",
                    proceso.Nombre,
                    $"{proceso.MemoriaMB} MB",
                    $"{memoriaAsignada} MB",
                    textoBloques,
                    $"{fragmentacion} MB",
                    proceso.Estado
                );

            dgvProcesos
                .Rows[fila]
                .Tag =
                proceso.Id;
        }
    }

    // =========================================================
    // FORMATEAR CASILLAS
    // =========================================================

    private static string FormatearBloques(
        List<int> bloques)
    {
        if (bloques.Count == 0)
            return "-";

        if (bloques.Count == 1)
        {
            return
                bloques[0]
                    .ToString();
        }

        bool consecutivos =
            true;

        for (
            int i = 1;
            i < bloques.Count;
            i++)
        {
            if (
                bloques[i] !=
                bloques[i - 1] + 1)
            {
                consecutivos =
                    false;

                break;
            }
        }

        if (consecutivos)
        {
            return
                $"{bloques.First()}-{bloques.Last()}";
        }

        return string.Join(
            ", ",
            bloques
        );
    }

    // =========================================================
    // APLICAR UNIDAD DE ASIGNACIÓN
    // =========================================================

    private void AplicarUnidadAsignacion()
    {
        int unidad =
            (int)
            numUnidadAsignacion.Value;

        if (
            kernel.Memoria.TotalMB %
            unidad !=
            0)
        {
            MessageBox.Show(
                this,

                $"La unidad de asignación debe dividir exactamente " +
                $"los {kernel.Memoria.TotalMB} MB de memoria.",

                "AMS.OS - Unidad de asignación",

                MessageBoxButtons.OK,

                MessageBoxIcon.Warning
            );

            return;
        }

        bool aplicado =
            kernel.Memoria
                .ReconfigurarUnidadAsignacion(
                    unidad,
                    kernel.Procesos
                );

        if (!aplicado)
        {
            MessageBox.Show(
                this,

                "No es posible utilizar esa unidad de asignación " +
                "con los procesos que actualmente están en memoria.",

                "AMS.OS - Unidad de asignación",

                MessageBoxButtons.OK,

                MessageBoxIcon.Warning
            );

            return;
        }

        ActualizarVista();

        MessageBox.Show(
            this,

            $"Unidad de asignación establecida en {unidad} MB." +
            Environment.NewLine +
            Environment.NewLine +
            $"Memoria total: {kernel.Memoria.TotalMB} MB" +
            Environment.NewLine +
            $"Cantidad de casillas: {kernel.Memoria.TotalBloques}",

            "AMS.OS - Mapa de bits",

            MessageBoxButtons.OK,

            MessageBoxIcon.Information
        );
    }

    // =========================================================
    // FINALIZAR PROCESO
    // =========================================================

    private void FinalizarSeleccionado()
    {
        if (
            dgvProcesos
                .SelectedRows
                .Count ==
            0)
        {
            MessageBox.Show(
                this,

                "Seleccione un proceso.",

                "Mapa de bits",

                MessageBoxButtons.OK,

                MessageBoxIcon.Information
            );

            return;
        }

        var fila =
            dgvProcesos
                .SelectedRows[0];

        if (
            fila.Tag is not
            int procesoId)
        {
            return;
        }

        var proceso =
            kernel.Procesos
                .FirstOrDefault(
                    p =>
                        p.Id ==
                        procesoId
                );

        if (proceso is null)
            return;

        if (
            proceso.Estado ==
            EstadoProceso.Terminado)
        {
            MessageBox.Show(
                this,

                "Ese proceso ya está terminado.",

                "Mapa de bits",

                MessageBoxButtons.OK,

                MessageBoxIcon.Information
            );

            return;
        }

        var respuesta =
            MessageBox.Show(
                this,

                $"¿Finalizar {proceso.Nombre} y liberar " +
                $"sus casillas de memoria?",

                "Liberar memoria",

                MessageBoxButtons.YesNo,

                MessageBoxIcon.Question
            );

        if (
            respuesta !=
            DialogResult.Yes)
        {
            return;
        }

        kernel.FinalizarProceso(
            procesoId
        );

        ActualizarVista();

        txtNombreProceso.Text =
            ObtenerNombreSugerido();
    }
}