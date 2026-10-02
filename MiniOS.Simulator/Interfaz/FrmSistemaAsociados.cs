using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOS.Simulator;

public sealed class FrmSistemaAsociados : Form
{
    private readonly MemoriaAsociados memoria =
        new(1024);

    private int siguienteProcesoId = 1;

    // =========================================================
    // CONTROLES
    // =========================================================

    private readonly Label lblTotal = new();
    private readonly Label lblAsignada = new();
    private readonly Label lblLibre = new();
    private readonly Label lblFragmentacion = new();

    private readonly TextBox txtNombre =
        new()
        {
            Text = "P01",
            Dock = DockStyle.Fill
        };

    private readonly NumericUpDown numTamano =
        new()
        {
            Minimum = 1,
            Maximum = 1024,
            Value = 100,
            Dock = DockStyle.Fill
        };

    private readonly Panel pnlMemoria =
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
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

    private readonly RichTextBox rtbLog =
        new()
        {
            Dock = DockStyle.Fill,

            ReadOnly = true,

            BackColor =
                Color.FromArgb(
                    52,
                    72,
                    49
                ),

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

    public FrmSistemaAsociados()
    {
        Text =
            "AMS.OS - Administración de Memoria - Sistema de Asociados";

        StartPosition =
            FormStartPosition.CenterParent;

        WindowState =
            FormWindowState.Maximized;

        MinimumSize =
            new Size(1100, 700);

        TemaMiniOS.Aplicar(this);

        ConfigurarTablas();

        Controls.Add(
            CrearInterfaz()
        );

        pnlMemoria.Paint +=
            DibujarMemoria;

        ActualizarVista();

        Registrar(
            "Sistema de Asociados inicializado con 1024 MB."
        );
    }

    // =========================================================
    // CONFIGURAR TABLAS
    // =========================================================

    private void ConfigurarTablas()
    {
        dgvProcesos.Columns.Add(
            "PID",
            "PID"
        );

        dgvProcesos.Columns.Add(
            "Nombre",
            "Proceso"
        );

        dgvProcesos.Columns.Add(
            "Solicitada",
            "Solicitada"
        );

        dgvProcesos.Columns.Add(
            "Asignada",
            "Asignada"
        );

        dgvProcesos.Columns.Add(
            "Fragmentacion",
            "Fragmentación"
        );

        dgvProcesos.Columns.Add(
            "Inicio",
            "Inicio"
        );

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
    }

    // =========================================================
    // INTERFAZ
    // =========================================================

    private Control CrearInterfaz()
    {
        var raiz =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

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
                95
            )
        );

        raiz.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                170
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

        raiz.Controls.Add(
            CrearTitulo(),
            0,
            0
        );

        raiz.Controls.Add(
            CrearResumen(),
            0,
            1
        );

        raiz.Controls.Add(
            CrearPanelNuevoProceso(),
            0,
            2
        );

        raiz.Controls.Add(
            CrearRepresentacion(),
            0,
            3
        );

        raiz.Controls.Add(
            CrearZonaDatos(),
            0,
            4
        );

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
                Dock =
                    DockStyle.Fill,

                BackColor =
                    TemaMiniOS.VerdeOscuro
            };

        panel.Controls.Add(
            new Label
            {
                Text =
                    "◉  ADMINISTRACIÓN DE MEMORIA - SISTEMA DE ASOCIADOS",

                Dock =
                    DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Padding =
                    new Padding(
                        20,
                        0,
                        0,
                        0
                    ),

                ForeColor =
                    Color.White,

                Font =
                    new Font(
                        "Segoe UI",
                        16f,
                        FontStyle.Bold
                    )
            }
        );

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
                Dock =
                    DockStyle.Fill,

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
        PrepararMetrica(lblAsignada);
        PrepararMetrica(lblLibre);
        PrepararMetrica(lblFragmentacion);

        panel.Controls.Add(
            lblTotal,
            0,
            0
        );

        panel.Controls.Add(
            lblAsignada,
            1,
            0
        );

        panel.Controls.Add(
            lblLibre,
            2,
            0
        );

        panel.Controls.Add(
            lblFragmentacion,
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
    // NUEVO PROCESO
    // =========================================================

    private Control CrearPanelNuevoProceso()
    {
        var panel =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount = 6,

                RowCount = 2,

                BackColor =
                    TemaMiniOS.Blanco,

                Padding =
                    new Padding(15)
            };

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                20
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                20
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                10
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                20
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                20
            )
        );

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                10
            )
        );

        panel.Controls.Add(
            Etiqueta("Proceso"),
            0,
            0
        );

        panel.Controls.Add(
            Etiqueta("Tamaño solicitado"),
            1,
            0
        );

        panel.Controls.Add(
            txtNombre,
            0,
            1
        );

        panel.Controls.Add(
            numTamano,
            1,
            1
        );

        panel.Controls.Add(
            Etiqueta("MB"),
            2,
            1
        );

        panel.Controls.Add(
            CrearBoton(
                "+ Añadir proceso",
                AgregarProceso
            ),
            3,
            1
        );

        return panel;
    }

    private static Label Etiqueta(
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
    // REPRESENTACIÓN
    // =========================================================

    private Control CrearRepresentacion()
    {
        var grupo =
            new GroupBox
            {
                Text =
                    "Representación actual de la memoria",

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
            pnlMemoria
        );

        return grupo;
    }

    // =========================================================
    // TABLAS Y LOG
    // =========================================================

    private Control CrearZonaDatos()
    {
        var zona =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount = 2,

                RowCount = 1
            };

        zona.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                62
            )
        );

        zona.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                38
            )
        );

        var izquierda =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount = 1,

                RowCount = 2
            };

        izquierda.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                50
            )
        );

        izquierda.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                50
            )
        );

        var grupoBloques =
            new GroupBox
            {
                Text =
                    "Bloques actuales",

                Dock =
                    DockStyle.Fill
            };

        grupoBloques.Controls.Add(
            dgvBloques
        );

        var grupoProcesos =
            new GroupBox
            {
                Text =
                    "Procesos en memoria",

                Dock =
                    DockStyle.Fill
            };

        grupoProcesos.Controls.Add(
            dgvProcesos
        );

        izquierda.Controls.Add(
            grupoBloques,
            0,
            0
        );

        izquierda.Controls.Add(
            grupoProcesos,
            0,
            1
        );

        var grupoLog =
            new GroupBox
            {
                Text =
                    "Registro de operaciones",

                Dock =
                    DockStyle.Fill
            };

        grupoLog.Controls.Add(
            rtbLog
        );

        zona.Controls.Add(
            izquierda,
            0,
            0
        );

        zona.Controls.Add(
            grupoLog,
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
                    new Padding(5)
            };

        panel.Controls.Add(
            CrearBoton(
                "↻ Reiniciar",
                Reiniciar
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
                Text =
                    texto,

                Width =
                    180,

                Height =
                    36,

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
            (_, _) =>
                accion();

        return boton;
    }

    // =========================================================
    // AGREGAR
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

        int solicitada =
            (int)numTamano.Value;

        var bloque =
            memoria.AsignarProceso(
                siguienteProcesoId,
                nombre,
                solicitada
            );

        if (bloque is null)
        {
            MessageBox.Show(
                this,

                "No existe un bloque disponible adecuado para este proceso.",

                "AMS.OS - Sistema de Asociados",

                MessageBoxButtons.OK,

                MessageBoxIcon.Warning
            );

            Registrar(
                $"No se pudo asignar P{siguienteProcesoId:00} - {nombre} ({solicitada} MB)."
            );

            return;
        }

        Registrar(
            $"P{siguienteProcesoId:00} - {nombre}: " +
            $"solicita {solicitada} MB, " +
            $"recibe un bloque de {bloque.TamanoMB} MB. " +
            $"Fragmentación: {bloque.FragmentacionInternaMB} MB."
        );

        siguienteProcesoId++;

        txtNombre.Text =
            $"P{siguienteProcesoId:00}";

        ActualizarVista();
    }

    // =========================================================
    // LIBERAR
    // =========================================================

    private void LiberarProceso()
    {
        if (dgvProcesos.CurrentRow is null ||
            dgvProcesos.CurrentRow.Tag is not int procesoId)
        {
            MessageBox.Show(
                this,

                "Seleccione un proceso de la tabla para liberarlo.",

                "AMS.OS - Sistema de Asociados",

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
            $"P{procesoId:00} liberado. " +
            "Los bloques asociados libres se unieron cuando fue posible."
        );

        ActualizarVista();
    }

    // =========================================================
    // REINICIAR
    // =========================================================

    private void Reiniciar()
    {
        memoria.Reiniciar();

        siguienteProcesoId = 1;

        txtNombre.Text =
            "P01";

        rtbLog.Clear();

        Registrar(
            "Sistema reiniciado: bloque único de 1024 MB."
        );

        ActualizarVista();
    }

    // =========================================================
    // ACTUALIZAR VISTA
    // =========================================================

    private void ActualizarVista()
    {
        lblTotal.Text =
            $"Memoria total\n{memoria.MemoriaTotalMB} MB";

        lblAsignada.Text =
            $"Memoria asignada\n{memoria.MemoriaAsignadaMB} MB";

        lblLibre.Text =
            $"Memoria libre\n{memoria.MemoriaLibreMB} MB";

        lblFragmentacion.Text =
            $"Fragmentación interna\n{memoria.FragmentacionInternaMB} MB";

        ActualizarBloques();

        ActualizarProcesos();

        pnlMemoria.Invalidate();
    }

    private void ActualizarBloques()
    {
        dgvBloques.Rows.Clear();

        foreach (var bloque in
                 memoria.ObtenerBloquesFinales())
        {
            dgvBloques.Rows.Add(
                $"{bloque.InicioMB} MB",

                $"{bloque.TamanoMB} MB",

                bloque.EstaOcupado
                    ? "Ocupado"
                    : "Libre",

                bloque.EstaOcupado
                    ? $"P{bloque.ProcesoId:00} - {bloque.NombreProceso}"
                    : "-"
            );
        }
    }

    private void ActualizarProcesos()
    {
        dgvProcesos.Rows.Clear();

        foreach (var bloque in
                 memoria.ObtenerBloquesOcupados())
        {
            int fila =
                dgvProcesos.Rows.Add(
                    $"P{bloque.ProcesoId:00}",

                    bloque.NombreProceso,

                    $"{bloque.MemoriaSolicitadaMB} MB",

                    $"{bloque.TamanoMB} MB",

                    $"{bloque.FragmentacionInternaMB} MB",

                    $"{bloque.InicioMB} MB"
                );

            dgvProcesos.Rows[fila].Tag =
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

        var bloques =
            memoria.ObtenerBloquesFinales();

        if (bloques.Count == 0)
            return;

        var area =
            new Rectangle(
                15,
                25,

                Math.Max(
                    1,
                    pnlMemoria.ClientSize.Width - 30
                ),

                Math.Max(
                    1,
                    pnlMemoria.ClientSize.Height - 50
                )
            );

        foreach (var bloque in bloques)
        {
            float proporcionInicio =
                (float)bloque.InicioMB /
                memoria.MemoriaTotalMB;

            float proporcionTamano =
                (float)bloque.TamanoMB /
                memoria.MemoriaTotalMB;

            int x =
                area.Left +
                (int)(
                    area.Width *
                    proporcionInicio
                );

            int ancho =
                Math.Max(
                    2,

                    (int)(
                        area.Width *
                        proporcionTamano
                    )
                );

            var rect =
                new Rectangle(
                    x,
                    area.Top,
                    ancho,
                    area.Height
                );

            Color color =
                bloque.EstaOcupado
                    ? Color.FromArgb(
                        248,
                        218,
                        213
                    )
                    : Color.FromArgb(
                        229,
                        247,
                        229
                    );

            using var fondo =
                new SolidBrush(color);

            using var borde =
                new Pen(
                    TemaMiniOS.VerdeOscuro,
                    1
                );

            e.Graphics.FillRectangle(
                fondo,
                rect
            );

            e.Graphics.DrawRectangle(
                borde,
                rect
            );

            if (ancho >= 48)
            {
                string texto =
                    bloque.EstaOcupado
                        ? $"P{bloque.ProcesoId:00}\n{bloque.TamanoMB} MB"
                        : $"Libre\n{bloque.TamanoMB} MB";

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
                    rect,
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
            $"[{DateTime.Now:HH:mm:ss}] " +
            $"{mensaje}" +
            $"{Environment.NewLine}"
        );

        rtbLog.ScrollToCaret();
    }
}