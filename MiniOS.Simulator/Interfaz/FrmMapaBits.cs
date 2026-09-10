using System.Drawing;
using System.Windows.Forms;

namespace MiniOS.Simulator;

public sealed class FrmMapaBits : Form
{
    private readonly Kernel kernel;

    private readonly Label lblTotal;
    private readonly Label lblBloque;
    private readonly Label lblBloques;
    private readonly Label lblUsada;
    private readonly Label lblLibre;
    private readonly Label lblFragmentacion;

    private readonly TableLayoutPanel tablaBloques;

    private readonly TextBox txtMapaBits;

    private readonly DataGridView dgvProcesos;

    public FrmMapaBits(Kernel kernel)
    {
        this.kernel = kernel;

        Text = "AMS.OS - Administración de Memoria";
        StartPosition = FormStartPosition.CenterParent;

        MinimumSize = new Size(1100, 700);
        ClientSize = new Size(1250, 780);

        TemaMiniOS.Aplicar(this);

        lblTotal = CrearValor();
        lblBloque = CrearValor();
        lblBloques = CrearValor();
        lblUsada = CrearValor();
        lblLibre = CrearValor();
        lblFragmentacion = CrearValor();

        tablaBloques = new TableLayoutPanel
        {
            ColumnCount = 8,
            RowCount = 8,

            AutoSize = true,
            AutoSizeMode =
                AutoSizeMode.GrowAndShrink,

            Dock = DockStyle.Top,

            Padding = new Padding(8),

            BackColor = TemaMiniOS.Blanco
        };

        txtMapaBits = new TextBox
        {
            Dock = DockStyle.Fill,

            ReadOnly = true,

            Font = new Font(
                "Consolas",
                10,
                FontStyle.Bold
            ),

            BackColor = TemaMiniOS.Blanco,

            ForeColor = TemaMiniOS.VerdeOscuro
        };

        dgvProcesos = CrearTablaProcesos();

        Controls.Add(ConstruirInterfaz());

        ActualizarVista();
    }

    // =========================================================
    // INTERFAZ PRINCIPAL
    // =========================================================

    private Control ConstruirInterfaz()
    {
        var principal = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            Padding = new Padding(18),

            ColumnCount = 1,
            RowCount = 4,

            BackColor = BackColor
        };

        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                65
            )
        );

        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                110
            )
        );

        principal.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100
            )
        );

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
            CrearZonaCentral(),
            0,
            2
        );

        principal.Controls.Add(
            CrearBotones(),
            0,
            3
        );

        return principal;
    }

    // =========================================================
    // ENCABEZADO
    // =========================================================

    private Control CrearEncabezado()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,

            BackColor =
                TemaMiniOS.VerdeOscuro,

            Padding =
                new Padding(18, 10, 18, 10)
        };

        var titulo = new Label
        {
            Text =
                "▦  ADMINISTRACIÓN DE MEMORIA - MAPA DE BITS",

            Dock = DockStyle.Fill,

            TextAlign =
                ContentAlignment.MiddleLeft,

            ForeColor = Color.White,

            Font = new Font(
                "Segoe UI",
                16,
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
        var resumen = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            ColumnCount = 6,
            RowCount = 1,

            Padding = new Padding(5),

            BackColor = TemaMiniOS.Blanco
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
                "Tamaño de bloque",
                lblBloque
            ),
            1,
            0
        );

        resumen.Controls.Add(
            CrearDato(
                "Bloques",
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
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            RowCount = 2,

            Padding = new Padding(5)
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

        var lblTitulo = new Label
        {
            Text = titulo,

            Dock = DockStyle.Fill,

            TextAlign =
                ContentAlignment.BottomCenter,

            Font = new Font(
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
            Dock = DockStyle.Fill,

            TextAlign =
                ContentAlignment.TopCenter,

            Font = new Font(
                "Segoe UI",
                13,
                FontStyle.Bold
            )
        };
    }

    // =========================================================
    // ZONA CENTRAL
    // =========================================================

    private Control CrearZonaCentral()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,

            Orientation =
                Orientation.Vertical,

            SplitterDistance = 760,

            BackColor = TemaMiniOS.Fondo
        };

        split.Panel1.Padding =
            new Padding(0, 8, 5, 0);

        split.Panel2.Padding =
            new Padding(5, 8, 0, 0);

        split.Panel1.Controls.Add(
            CrearPanelMapa()
        );

        split.Panel2.Controls.Add(
            CrearPanelProcesos()
        );

        return split;
    }

    // =========================================================
    // MAPA DE BITS
    // =========================================================

    private Control CrearPanelMapa()
    {
        var grupo = new GroupBox
        {
            Text = "Mapa de bits",

            Dock = DockStyle.Fill,

            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Bold
            ),

            ForeColor =
                TemaMiniOS.VerdeOscuro,

            BackColor =
                TemaMiniOS.Blanco,

            Padding = new Padding(10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            RowCount = 2,

            ColumnCount = 1
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

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,

            AutoScroll = true,

            BackColor =
                TemaMiniOS.Blanco
        };

        scroll.Controls.Add(tablaBloques);

        var bitsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            RowCount = 2,

            Padding = new Padding(4)
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

                Dock = DockStyle.Fill,

                Font = new Font(
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

        grupo.Controls.Add(layout);

        return grupo;
    }

    // =========================================================
    // TABLA DE PROCESOS
    // =========================================================

    private Control CrearPanelProcesos()
    {
        var grupo = new GroupBox
        {
            Text = "Procesos en memoria",

            Dock = DockStyle.Fill,

            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Bold
            ),

            ForeColor =
                TemaMiniOS.VerdeOscuro,

            BackColor =
                TemaMiniOS.Blanco,

            Padding = new Padding(10)
        };

        grupo.Controls.Add(dgvProcesos);

        return grupo;
    }

    private DataGridView CrearTablaProcesos()
    {
        var tabla = new DataGridView
        {
            Dock = DockStyle.Fill,

            ReadOnly = true,

            AllowUserToAddRows = false,

            AllowUserToDeleteRows = false,

            AllowUserToResizeRows = false,

            RowHeadersVisible = false,

            SelectionMode =
                DataGridViewSelectionMode
                    .FullRowSelect,

            MultiSelect = false,

            AutoGenerateColumns = false,

            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill,

            BackgroundColor =
                TemaMiniOS.Blanco,

            BorderStyle =
                BorderStyle.None
        };

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "PID",
                FillWeight = 18
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Proceso",
                FillWeight = 35
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Solicitada",
                FillWeight = 28
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Asignada",
                FillWeight = 28
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Bloques",
                FillWeight = 42
            }
        );

        tabla.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Estado",
                FillWeight = 30
            }
        );

        return tabla;
    }

    // =========================================================
    // BOTONES
    // =========================================================

    private Control CrearBotones()
    {
        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,

            FlowDirection =
                FlowDirection.LeftToRight,

            Padding = new Padding(5, 8, 5, 5)
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
        var boton = new Button
        {
            Text = texto,

            Size = new Size(220, 40),

            BackColor = color,

            ForeColor = Color.White,

            FlatStyle = FlatStyle.Flat,

            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold
            ),

            Cursor = Cursors.Hand,

            Margin = new Padding(5)
        };

        boton.FlatAppearance.BorderSize = 0;

        boton.Click += (_, _) => accion();

        return boton;
    }

    // =========================================================
    // ACTUALIZAR MAPA
    // =========================================================

    private void ActualizarVista()
    {
        lblTotal.Text =
            $"{kernel.Memoria.TotalMB} MB";

        lblBloque.Text =
            $"{kernel.Memoria.TamanoBloqueMB} MB";

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

        txtMapaBits.Text =
            kernel.Memoria.ObtenerMapaBitsTexto();

        ActualizarProcesos();
    }

    // =========================================================
    // CREAR CUADRÍCULAS
    // =========================================================

    private void CrearBloques()
    {
        tablaBloques.SuspendLayout();

        tablaBloques.Controls.Clear();

        tablaBloques.ColumnStyles.Clear();
        tablaBloques.RowStyles.Clear();

        tablaBloques.ColumnCount = 8;
        tablaBloques.RowCount = 8;

        for (int columna = 0; columna < 8; columna++)
        {
            tablaBloques.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    90
                )
            );
        }

        for (int fila = 0; fila < 8; fila++)
        {
            tablaBloques.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    64
                )
            );
        }

        for (
            int i = 0;
            i < kernel.Memoria.TotalBloques;
            i++)
        {
            bool ocupado =
                kernel.Memoria.EstaOcupado(i);

            int? propietario =
                kernel.Memoria
                    .ObtenerPropietarioBloque(i);

            string nombre = "Libre";

            if (propietario.HasValue)
            {
                var proceso =
                    kernel.Procesos.FirstOrDefault(
                        p =>
                            p.Id ==
                            propietario.Value
                    );

                nombre =
                    proceso?.Nombre ??
                    $"P{propietario.Value:00}";
            }

            var bloque = new Label
            {
                Dock = DockStyle.Fill,

                Margin = new Padding(3),

                Text =
                    ocupado
                        ? $"{i}\n1 · {nombre}"
                        : $"{i}\n0 · Libre",

                TextAlign =
                    ContentAlignment.MiddleCenter,

                Font = new Font(
                    "Segoe UI",
                    8.5f,
                    FontStyle.Bold
                ),

                BorderStyle =
                    BorderStyle.FixedSingle,

                BackColor =
                    ocupado
                        ? Color.MistyRose
                        : Color.Honeydew,

                ForeColor =
                    ocupado
                        ? Color.DarkRed
                        : Color.DarkGreen
            };

            int fila = i / 8;
            int columna = i % 8;

            tablaBloques.Controls.Add(
                bloque,
                columna,
                fila
            );
        }

        tablaBloques.ResumeLayout();
    }

    // =========================================================
    // ACTUALIZAR TABLA
    // =========================================================

    private void ActualizarProcesos()
    {
        dgvProcesos.Rows.Clear();

        foreach (var proceso in kernel.Procesos)
        {
            var bloques =
                kernel.Memoria
                    .ObtenerBloquesProceso(
                        proceso.Id
                    );

            string textoBloques =
                bloques.Count == 0
                    ? "-"
                    : string.Join(
                        ", ",
                        bloques
                    );

            int memoriaAsignada =
                kernel.Memoria
                    .ObtenerMemoriaAsignadaMB(
                        proceso.Id
                    );

            int fila =
                dgvProcesos.Rows.Add(
                    $"P{proceso.Id:00}",
                    proceso.Nombre,
                    $"{proceso.MemoriaMB} MB",
                    $"{memoriaAsignada} MB",
                    textoBloques,
                    proceso.Estado
                );

            dgvProcesos.Rows[fila].Tag =
                proceso.Id;
        }
    }

    // =========================================================
    // FINALIZAR PROCESO
    // =========================================================

    private void FinalizarSeleccionado()
    {
        if (
            dgvProcesos.SelectedRows.Count == 0)
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
            dgvProcesos.SelectedRows[0];

        if (fila.Tag is not int procesoId)
            return;

        var proceso =
            kernel.Procesos.FirstOrDefault(
                p => p.Id == procesoId
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
                $"¿Finalizar {proceso.Nombre} y liberar sus bloques de memoria?",
                "Liberar memoria",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

        if (respuesta != DialogResult.Yes)
            return;

        kernel.FinalizarProceso(procesoId);

        ActualizarVista();
    }
}