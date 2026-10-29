using System.Drawing;
using System.Windows.Forms;

namespace MiniOS.Simulator;

public sealed class FrmAsignacionArchivos : Form
{
    private readonly DiscoAsignacion disco;
    private readonly TextBox nombre = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown cantidad = new() { Minimum = 1, Maximum = 20, Value = 4, Dock = DockStyle.Fill };
    private readonly DataGridView tabla = new();
    private readonly DataGridView mapa = new();
    private readonly TextBox detalle = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both };
    private readonly ListBox historial = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private readonly Label resumen = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private bool actualizando;
    private bool Ligada => disco.Metodo == MetodoAsignacion.Ligada;

    public FrmAsignacionArchivos(MetodoAsignacion metodo)
    {
        disco = new DiscoAsignacion(metodo);
        Text = "AMS.OS - Asignación " + (Ligada ? "por listas ligadas" : "contigua / adyacente");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1180, 780);
        MinimumSize = new Size(1000, 700);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(241, 243, 240);
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6 };
        foreach (int alto in new[] { 70, 48, 58 }) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        Controls.Add(panel);
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Text = Ligada ? "ALGORITMO 2 · LISTAS LIGADAS\nCada bloque apunta al siguiente; FIN indica el último bloque."
                : "ALGORITMO 1 · CONTIGUA / ADYACENTE\nEl directorio guarda el bloque inicial y la longitud del archivo.",
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        }, 0, 0);
        var campos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        campos.Controls.Add(new Label { Text = "Archivo:", Dock = DockStyle.Fill }, 0, 0);
        campos.Controls.Add(nombre, 1, 0);
        campos.Controls.Add(new Label { Text = "Bloques:", Dock = DockStyle.Fill }, 2, 0);
        campos.Controls.Add(cantidad, 3, 0);
        panel.Controls.Add(campos, 0, 1);
        var botones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = Ligada ? 5 : 6 };
        for (int i = 0; i < botones.ColumnCount; i++) botones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / botones.ColumnCount));
        botones.Controls.Add(Boton("Crear", Crear), 0, 0);
        botones.Controls.Add(Boton("Consultar", Consultar), 1, 0);
        botones.Controls.Add(Boton("Eliminar", Eliminar), 2, 0);
        botones.Controls.Add(Boton("Ejemplo con huecos", Ejemplo), 3, 0);
        botones.Controls.Add(Boton("Reiniciar", Reiniciar), 4, 0);
        if (!Ligada) botones.Controls.Add(Boton("Compactar", Compactar), 5, 0);
        panel.Controls.Add(botones, 0, 2);
        var tablas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        ConfigurarTabla(tabla, new[] { "Archivo", "Inicio", "Bloques", Ligada ? "Cadena" : "Rango" });
        ConfigurarTabla(mapa, new[] { "Bloque", "Propietario", Ligada ? "Siguiente" : "Estado" });
        tabla.Columns[0].FillWeight = 95;
        tabla.Columns[1].FillWeight = 45;
        tabla.Columns[2].FillWeight = 50;
        tabla.Columns[3].FillWeight = 180;
        tabla.SelectionChanged += (_, _) =>
        {
            if (actualizando || tabla.SelectedRows.Count == 0) return;
            nombre.Text = Convert.ToString(tabla.SelectedRows[0].Cells[0].Value) ?? "";
            Ver(disco.Consultar(nombre.Text));
        };
        tablas.Controls.Add(tabla, 0, 0);
        tablas.Controls.Add(mapa, 1, 0);
        panel.Controls.Add(tablas, 0, 3);
        var textos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        textos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        textos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        textos.Controls.Add(detalle, 0, 0);
        textos.Controls.Add(historial, 1, 0);
        panel.Controls.Add(textos, 0, 4);
        var pie = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        pie.Controls.Add(resumen, 0, 0);
        pie.Controls.Add(Boton("Regresar", Close), 1, 0);
        panel.Controls.Add(pie, 0, 5);
        Actualizar();
        Registrar("Disco virtual independiente: 20 bloques libres. No modifica archivos reales.");
    }

    private Button Boton(string texto, Action accion)
    {
        var b = new Button { Text = texto, Dock = DockStyle.Fill, BackColor = Color.FromArgb(187, 205, 182), FlatStyle = FlatStyle.Flat };
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

    private string Ruta(DiscoAsignacion.Entrada a) => Ligada
        ? string.Join(" → ", disco.Recorrer(a)) + " → FIN"
        : $"{a.Inicio} a {a.Inicio + a.Cantidad - 1}";

    private void Ver(DiscoAsignacion.Entrada a)
    {
        detalle.Text = $"Archivo: {a.Nombre}\r\nInicio: {a.Inicio} | Tamaño: {a.Cantidad} bloques\r\n" + Ruta(a) + "\r\n" +
            (Ligada ? "Consulta secuencial: se parte del inicio y se sigue el puntero de cada bloque hasta FIN."
                    : "Acceso al bloque lógico k: inicio + k, con 0 <= k < tamaño.");
        foreach (DataGridViewRow fila in mapa.Rows)
            fila.DefaultCellStyle.BackColor = string.Equals(disco.Propietario(fila.Index), a.Nombre, StringComparison.OrdinalIgnoreCase)
                ? Color.Khaki : disco.Propietario(fila.Index) == null ? Color.White : Color.FromArgb(187, 205, 182);
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
            (Ligada ? "Usará 4 → 5 → 6 → 7 → 12 → 13 → FIN."
                    : "No existe un hueco de 6 bloques. Pulse Compactar y vuelva a crear F.txt.");
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
            mapa.Rows.Clear();
            for (int i = 0; i < disco.Total; i++)
            {
                string? propietario = disco.Propietario(i);
                mapa.Rows.Add(i, propietario ?? "Libre", propietario == null ? "—" : Ligada ? disco.Siguiente(i)?.ToString() ?? "FIN" : "Ocupado");
                mapa.Rows[i].DefaultCellStyle.BackColor = propietario == null ? Color.White : Color.FromArgb(187, 205, 182);
            }
            mapa.ClearSelection();
            resumen.Text = $"Total: 20 | Ocupados: {20 - disco.Libres} | Libres: {disco.Libres} | Mayor hueco: {disco.MayorHueco}";
        }
        finally { actualizando = false; }
    }
    private void Registrar(string mensaje) => historial.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {mensaje}");
}
