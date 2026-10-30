using System.Drawing;
using System.Windows.Forms;

namespace MiniOS.Simulator;

public sealed class FrmAsignacionContinua : Form
{
    private readonly DiscoAsignacion disco;
    private readonly TextBox nombre = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown cantidad = new() { Minimum = 1, Maximum = 20, Value = 4, Dock = DockStyle.Fill };
    private readonly DataGridView tabla = new() { Dock = DockStyle.Fill };
    private readonly TextBox detalle = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both };
    private readonly ListBox historial = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private readonly Label resumen = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private bool actualizando;

    public FrmAsignacionContinua()
    {
        disco = new DiscoAsignacion(MetodoAsignacion.Continua);
        Text = "AMS.OS - Asignación " + ("continua / adyacente");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1250, 790);
        MinimumSize = new Size(1080, 720);
        Font = new Font("Segoe UI", 9.5f);
        detalle.Font = new Font("Consolas", 10);
        resumen.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        BackColor = Color.FromArgb(241, 243, 240);
        ConstruirInterfaz();
        Actualizar();
        Registrar("Disco virtual preparado: 20 bloques libres.");
    }

    private const int TotalBloques = 20;
    private readonly Color verde = Color.FromArgb(55, 75, 54);
    private readonly Color verdeClaro = Color.FromArgb(187, 205, 182);
    private readonly Color fondo = Color.FromArgb(241, 243, 240);
    private readonly Label[] bloquesVisuales = new Label[TotalBloques];
    private readonly Label lblEstado = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private string? seleccionado;
    private static Label Etiqueta(string texto) => new() { Text = texto, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

    private void ConstruirInterfaz()
    {
        var principal = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 4,
            ColumnCount = 1
        };
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        Controls.Add(principal);

        principal.Controls.Add(new Label
        {
            Text = "ASIGNACIÓN CONTINUA / ADYACENTE · ALGORITMO 1",
            Dock = DockStyle.Fill,
            ForeColor = verde,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        principal.Controls.Add(resumen, 0, 1);

        // Barra inferior: estado y botón para regresar a AMS.OS.
        var barraInferior = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 3, 0, 0)
        };
        barraInferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        barraInferior.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        barraInferior.Controls.Add(lblEstado, 0, 0);
        barraInferior.Controls.Add(Boton("← Regresar", () => Close()), 1, 0);
        principal.Controls.Add(barraInferior, 0, 3);

        var centro = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        centro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        centro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        principal.Controls.Add(centro, 0, 2);

        var izquierda = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0, 0, 7, 0)
        };
        izquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        izquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        izquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        izquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        izquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        centro.Controls.Add(izquierda, 0, 0);
        izquierda.Controls.Add(Etiqueta("CREAR, CONSULTAR Y ELIMINAR ARCHIVOS"), 0, 0);

        var campos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        campos.Controls.Add(Etiqueta("Nombre:"), 0, 0);
        campos.Controls.Add(nombre, 1, 0);
        campos.Controls.Add(Etiqueta("Bloques:"), 2, 0);
        campos.Controls.Add(cantidad, 3, 0);
        izquierda.Controls.Add(campos, 0, 1);

        var acciones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        for (int i = 0; i < 3; i++)
            acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        acciones.Controls.Add(Boton("Crear archivo", Crear), 0, 0);
        acciones.Controls.Add(Boton("Consultar", Consultar), 1, 0);
        acciones.Controls.Add(Boton("Eliminar", Eliminar), 2, 0);
        izquierda.Controls.Add(acciones, 0, 2);

        tabla.AllowUserToAddRows = false;
        tabla.AllowUserToDeleteRows = false;
        tabla.ReadOnly = true;
        tabla.RowHeadersVisible = false;
        tabla.MultiSelect = false;
        tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        tabla.BackgroundColor = Color.White;
        tabla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        tabla.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        tabla.Columns.Add("Nombre", "Archivo");
        tabla.Columns.Add("Inicio", "Inicio");
        tabla.Columns.Add("Tamano", "Tamaño");
        tabla.Columns.Add("Ubicacion", "Rango consecutivo");
        tabla.Columns[0].FillWeight = 90;
        tabla.Columns[1].FillWeight = 45;
        tabla.Columns[2].FillWeight = 55;
        tabla.Columns[3].FillWeight = 150;
        tabla.SelectionChanged += (_, _) =>
        {
            if (actualizando || tabla.SelectedRows.Count == 0) return;
            nombre.Text = Convert.ToString(tabla.SelectedRows[0].Cells[0].Value) ?? "";
            Ver(disco.Consultar(nombre.Text));
        };
        izquierda.Controls.Add(tabla, 0, 3);
        izquierda.Controls.Add(detalle, 0, 4);

        var derecha = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(7, 0, 0, 0)
        };
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 185));
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        centro.Controls.Add(derecha, 1, 0);
        derecha.Controls.Add(Etiqueta("ESTADO DEL DISCO · BLOQUES 0 AL 19"), 0, 0);

        var mapa = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 10,
            RowCount = 2,
            Padding = new Padding(5)
        };
        for (int i = 0; i < 10; i++)
            mapa.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
        mapa.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        mapa.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        for (int i = 0; i < TotalBloques; i++)
        {
            bloquesVisuales[i] = new Label
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            mapa.Controls.Add(bloquesVisuales[i], i % 10, i / 10);
        }
        derecha.Controls.Add(mapa, 0, 1);

        var herramientas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        for (int i = 0; i < 4; i++)
            herramientas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        herramientas.Controls.Add(Boton("Mostrar disco", MostrarDisco), 0, 0);
        herramientas.Controls.Add(Boton("Espacio libre", MostrarLibres), 1, 0);
        herramientas.Controls.Add(Boton("Cargar ejemplo", Ejemplo), 2, 0);
        herramientas.Controls.Add(Boton("Reiniciar", Reiniciar), 3, 0);
        derecha.Controls.Add(herramientas, 0, 2);
        derecha.Controls.Add(Boton("Compactar disco", Compactar), 0, 3);
        derecha.Controls.Add(historial, 0, 4);
    }

    private Button Boton(string texto, Action accion)
    {
        var b = new Button { Text = texto, Dock = DockStyle.Fill, BackColor = Color.FromArgb(187, 205, 182), FlatStyle = FlatStyle.Flat, ForeColor = verde, Cursor = Cursors.Hand, Margin = new Padding(3, 4, 3, 4) };
        b.Click += (_, _) =>
        {
            try { accion(); }
            catch (InvalidOperationException ex)
            {
                Registrar(ex.Message);
                MessageBox.Show(this, ex.Message, "Asignación de archivos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        return b;
    }

    private static void ConfigurarTabla(DataGridView grid, string[] columnas)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.BackgroundColor = Color.White;
        foreach (var titulo in columnas) grid.Columns.Add(titulo, titulo);
    }

    private string Ruta(DiscoAsignacion.Entrada a) =>
        $"{a.Inicio} a {a.Inicio + a.Cantidad - 1}";

    private void Ver(DiscoAsignacion.Entrada a)
    {
        detalle.Text = $"Archivo: {a.Nombre}\r\nInicio: {a.Inicio} | Tamaño: {a.Cantidad} bloques\r\n" + Ruta(a) + "\r\n" +
            ("Acceso al bloque lógico k: inicio + k, con 0 <= k < tamaño.");
        seleccionado = a.Nombre;
        ActualizarMapa();
    }
    private void Crear()
    {
        var a = disco.Crear(nombre.Text, (int)cantidad.Value);
        Actualizar(); Ver(a); Registrar($"Creado {a.Nombre}: {Ruta(a)}.");
    }
    private void Consultar()
    {
        var a = disco.Consultar(nombre.Text);
        Ver(a); Registrar($"Consulta {a.Nombre}: {Ruta(a)}.");
    }
    private bool Confirmar(string mensaje) => MessageBox.Show(this, mensaje, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    private void Eliminar()
    {
        var a = disco.Consultar(nombre.Text);
        if (!Confirmar($"¿Eliminar {a.Nombre} y liberar sus {a.Cantidad} bloques?")) return;
        disco.Eliminar(a.Nombre); Actualizar(); detalle.Clear(); nombre.Clear();
        Registrar($"Eliminado {a.Nombre}: {a.Cantidad} bloques liberados.");
    }
    private void Compactar()
    {
        disco.Compactar(); Actualizar(); detalle.Clear();
        Registrar("Compactación realizada: se actualizaron los inicios y se agruparon los bloques libres al final.");
    }
    private void Ejemplo()
    {
        if (disco.Archivos.Any() && !Confirmar("¿Reemplazar la simulación por el ejemplo?")) return;
        disco.Reiniciar();
        foreach (string n in new[] { "A.txt", "B.txt", "C.txt", "D.txt", "E.txt" }) disco.Crear(n, 4);
        disco.Eliminar("B.txt"); disco.Eliminar("D.txt");
        Actualizar(); nombre.Text = "F.txt"; cantidad.Value = 6;
        detalle.Text = "8 bloques libres: 4–7 y 12–15. Intente crear F.txt con 6 bloques.\r\n" +
            ("No existe un hueco de 6 bloques. Pulse Compactar y vuelva a crear F.txt.");
        Registrar("Ejemplo: 12 ocupados, 8 libres, mayor hueco de 4 bloques.");
    }
    private void Reiniciar()
    {
        if (!Confirmar("¿Borrar todos los archivos simulados?")) return;
        disco.Reiniciar(); nombre.Clear(); detalle.Clear(); historial.Items.Clear(); Actualizar(); Registrar("Disco reiniciado.");
    }
    private void Actualizar()
    {
        actualizando = true;
        try
        {
            tabla.Rows.Clear();
            foreach (var a in disco.Archivos) tabla.Rows.Add(a.Nombre, a.Inicio, a.Cantidad, Ruta(a));
            tabla.ClearSelection();
            seleccionado = null;
            ActualizarMapa();
            resumen.Text = $"Total: 20 | Ocupados: {20 - disco.Libres} | Libres: {disco.Libres} | Mayor hueco: {disco.MayorHueco}";
        }
        finally { actualizando = false; }
    }
    private void ActualizarMapa()
    {
        for (int i = 0; i < disco.Total; i++)
        {
            string? propietario = disco.Propietario(i);
            bloquesVisuales[i].Text = $"{i:00}\n{propietario ?? "Libre"}";
            bloquesVisuales[i].BackColor = propietario == null ? Color.FromArgb(236, 239, 234) :
                string.Equals(propietario, seleccionado, StringComparison.OrdinalIgnoreCase) ? Color.Khaki : verdeClaro;
            bloquesVisuales[i].ForeColor = verde;
        }
    }

    private void MostrarLibres()
    {
        string mensaje = $"Hay {disco.Libres} bloques libres de 20. Mayor hueco: {disco.MayorHueco}.";
        Registrar(mensaje);
        MessageBox.Show(this, mensaje, "Espacio disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void MostrarDisco()
    {
        using var ventana = new Form
        {
            Text = "AMS.OS - Detalle del disco virtual", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(760, 620), MinimumSize = new Size(640, 520), BackColor = fondo, Font = Font
        };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        panel.Controls.Add(Etiqueta("CONTINUA: directorio con inicio + cantidad de bloques."), 0, 0);
        var tablaDisco = new DataGridView();
        ConfigurarTabla(tablaDisco, new[] { "Bloque", "Estado", "Archivo propietario", "Ubicación" });
        for (int i = 0; i < disco.Total; i++)
        {
            var propietario = disco.Propietario(i);
            tablaDisco.Rows.Add(i, propietario == null ? "Libre" : "Ocupado", propietario ?? "—",
                propietario == null ? "—" : Ruta(disco.Consultar(propietario)));
            tablaDisco.Rows[i].DefaultCellStyle.BackColor = propietario == null ? Color.White : verdeClaro;
        }
        tablaDisco.ClearSelection();
        panel.Controls.Add(tablaDisco, 0, 1);
        panel.Controls.Add(Boton("Regresar al simulador", ventana.Close), 0, 2);
        ventana.Controls.Add(panel);
        ventana.ShowDialog(this);
    }

    private void Registrar(string mensaje)
    {
        historial.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {mensaje}");
        lblEstado.Text = mensaje;
    }
}
