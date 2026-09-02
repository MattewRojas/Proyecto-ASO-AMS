namespace MiniOS.Simulator;

public sealed class FrmNuevoProceso : Form
{
    private readonly TextBox txtNombre = new() { Text = "Calculadora", Dock = DockStyle.Fill };
    private readonly NumericUpDown numMemoria = new() { Minimum = 32, Maximum = 2048, Value = 256, Increment = 32, Dock = DockStyle.Fill };
    public string NombreProceso => string.IsNullOrWhiteSpace(txtNombre.Text) ? "Proceso" : txtNombre.Text.Trim();
    public int MemoriaMB => (int)numMemoria.Value;

    public FrmNuevoProceso()
    {
        Text = "Nuevo proceso"; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(390, 180); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; TemaMiniOS.Aplicar(this);
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 3 };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.Controls.Add(new Label { Text = "Nombre:", AutoSize = true }, 0, 0); t.Controls.Add(txtNombre, 1, 0);
        t.Controls.Add(new Label { Text = "Memoria (MB):", AutoSize = true }, 0, 1); t.Controls.Add(numMemoria, 1, 1);
        var ok = new Button { Text = "Crear", DialogResult = DialogResult.OK, Width = 100, BackColor = TemaMiniOS.Verde, ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100, BackColor = TemaMiniOS.VerdeAzulado, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; f.Controls.Add(cancelar); f.Controls.Add(ok); t.Controls.Add(f, 0, 2); t.SetColumnSpan(f, 2);
        Controls.Add(t); AcceptButton = ok; CancelButton = cancelar;
    }
}

public sealed class FrmDetalle : Form
{
    public FrmDetalle(string titulo, object datos)
    {
        Text = titulo; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(620, 420); TemaMiniOS.Aplicar(this);
        var encabezado = new Label { Text = titulo.ToUpperInvariant(), Dock = DockStyle.Top, Height = 58, BackColor = TemaMiniOS.VerdeClaro, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = TemaMiniOS.VerdeOscuro };
        var lista = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.None, BackColor = TemaMiniOS.Blanco, ForeColor = TemaMiniOS.VerdeOscuro };
        if (datos is TreeNode nodo) AgregarNodo(lista, nodo, "");
        else if (datos is System.Collections.IEnumerable items) foreach (var item in items) lista.Items.Add(item?.ToString());
        else lista.Items.Add(datos.ToString());
        var volver = new Button { Text = "Volver", Dock = DockStyle.Bottom, Height = 40, BackColor = TemaMiniOS.VerdeAzulado, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        volver.Click += (_, _) => Close(); Controls.Add(lista); Controls.Add(volver); Controls.Add(encabezado);
    }
    private static void AgregarNodo(ListBox lista, TreeNode nodo, string sangria) { lista.Items.Add(sangria + "📁 " + nodo.Text); foreach (TreeNode hijo in nodo.Nodes) AgregarNodo(lista, hijo, sangria + "    "); }
}
