using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MiniOS.Simulator;

// Simulación independiente: no modifica archivos ni unidades reales.
public sealed class FrmAsignacionINodos : Form
{
    private const int TotalBloques = 20;
    private readonly string?[] disco = new string?[TotalBloques];
    private readonly Dictionary<string, INodo> archivos =
        new(StringComparer.OrdinalIgnoreCase);
    private int siguienteINodo = 1;

    private readonly Color verde = Color.FromArgb(55, 75, 54);
    private readonly Color verdeClaro = Color.FromArgb(187, 205, 182);
    private readonly Color fondo = Color.FromArgb(241, 243, 240);

    private readonly TextBox txtNombre = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown numBloques = new()
    {
        Minimum = 1,
        Maximum = TotalBloques,
        Value = 4,
        Dock = DockStyle.Fill
    };
    private readonly DataGridView tabla = new() { Dock = DockStyle.Fill };
    private readonly TextBox txtDetalle = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10)
    };
    private readonly ListBox historial = new() { Dock = DockStyle.Fill };
    private readonly Label lblResumen = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", 11, FontStyle.Bold)
    };
    private readonly Label lblEstado = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label[] bloquesVisuales = new Label[TotalBloques];
    private bool actualizandoTabla;
    private string? seleccionado;

    // I-NODOS: referencias centralizadas en el i-nodo; los bloques no enlazan al siguiente.
    private sealed class INodo
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = "";
        public DateTime CreadoEl { get; init; } = DateTime.Now;
        // El bloque lógico k se localiza con Bloques[k], sin recorrer una cadena.
        public List<int> Bloques { get; init; } = new();
        public int TamanoBloques => Bloques.Count;
    }

    public FrmAsignacionINodos()
    {
        Text = "AMS.OS - Asignación mediante i-nodos";
        // Disco e i-nodos simulados: no se crean ni eliminan archivos reales.
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1080, 720);
        ClientSize = new Size(1250, 790);
        BackColor = fondo;
        Font = new Font("Segoe UI", 9.5f);
        ConstruirInterfaz();
        Registrar("Disco virtual preparado: 20 bloques libres.");
        Actualizar();
    }

    private static Label Etiqueta(string texto) => new()
    {
        Text = texto,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private Button Boton(string texto, Action accion)
    {
        var boton = new Button
        {
            Text = texto,
            Dock = DockStyle.Fill,
            BackColor = verdeClaro,
            ForeColor = verde,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 4, 3, 4)
        };
        boton.Click += (_, _) => accion();
        return boton;
    }

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
            Text = "ASIGNACIÓN MEDIANTE I-NODOS · ALGORITMO 3",
            Dock = DockStyle.Fill,
            ForeColor = verde,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        principal.Controls.Add(lblResumen, 0, 1);

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
        campos.Controls.Add(txtNombre, 1, 0);
        campos.Controls.Add(Etiqueta("Bloques:"), 2, 0);
        campos.Controls.Add(numBloques, 3, 0);
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
        tabla.Columns.Add("Id", "I-nodo");
        tabla.Columns.Add("Nombre", "Archivo");
        tabla.Columns.Add("Tamano", "Tamaño");
        tabla.Columns.Add("Referencias", "Bloques asignados");
        tabla.SelectionChanged += (_, _) =>
        {
            if (actualizandoTabla || tabla.SelectedRows.Count == 0) return;
            var nombre = Convert.ToString(tabla.SelectedRows[0].Cells[1].Value);
            if (nombre != null && archivos.TryGetValue(nombre, out var archivo))
            {
                txtNombre.Text = nombre;
                VerINodo(archivo);
            }
        };
        izquierda.Controls.Add(tabla, 0, 3);
        izquierda.Controls.Add(txtDetalle, 0, 4);

        var derecha = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(7, 0, 0, 0)
        };
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 185));
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
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
        herramientas.Controls.Add(Boton("Cargar ejemplo", CargarEjemplo), 2, 0);
        herramientas.Controls.Add(Boton("Reiniciar", Reiniciar), 3, 0);
        derecha.Controls.Add(herramientas, 0, 2);
        derecha.Controls.Add(historial, 0, 3);
    }

    private int Libres => disco.Count(nombre => nombre == null);

    private void Crear()
    {
        string nombre = txtNombre.Text.Trim();
        int cantidad = (int)numBloques.Value;
        if (nombre.Length == 0 || nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            Error("Escriba un nombre de archivo válido.");
            return;
        }
        if (archivos.ContainsKey(nombre))
        {
            Error("Ya existe un archivo con ese nombre.");
            return;
        }
        if (cantidad > Libres)
        {
            Error($"Espacio insuficiente: necesita {cantidad} bloques y solo hay {Libres} libres.");
            return;
        }

        var libres = Enumerable.Range(0, TotalBloques)
            .Where(i => disco[i] == null).Take(cantidad).ToList();
        var archivo = new INodo { Id = siguienteINodo++, Nombre = nombre, Bloques = libres };
        archivos.Add(nombre, archivo);
        foreach (int bloque in libres) disco[bloque] = nombre;
        seleccionado = nombre;
        Actualizar();
        VerINodo(archivo);
        Registrar($"Creado {nombre}: i-nodo #{archivo.Id}, bloques [{string.Join(", ", libres)}].");
    }

    private void Consultar()
    {
        if (!archivos.TryGetValue(txtNombre.Text.Trim(), out var archivo))
        {
            Error("No existe el archivo indicado.");
            return;
        }
        VerINodo(archivo);
        Registrar($"Consultado {archivo.Nombre}.");
    }

    private void Eliminar()
    {
        if (!archivos.TryGetValue(txtNombre.Text.Trim(), out var archivo))
        {
            Error("No existe el archivo indicado.");
            return;
        }
        if (MessageBox.Show($"¿Eliminar {archivo.Nombre} y liberar sus bloques?",
                "Confirmar eliminación", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;

        foreach (int bloque in archivo.Bloques) disco[bloque] = null;
        archivos.Remove(archivo.Nombre);
        seleccionado = null;
        txtNombre.Clear();
        txtDetalle.Clear();
        Actualizar();
        Registrar($"Eliminado {archivo.Nombre}: {archivo.Bloques.Count} bloques liberados.");
    }

    private void VerINodo(INodo archivo)
    {
        seleccionado = archivo.Nombre;
        txtDetalle.Text =
            $"I-NODO #{archivo.Id}\r\n" +
            $"Archivo: {archivo.Nombre}\r\n" +
            $"Tamaño: {archivo.TamanoBloques} bloques\r\n" +
            $"Creado: {archivo.CreadoEl:dd/MM/yyyy HH:mm:ss}\r\n" +
            $"Referencias: [{string.Join(", ", archivo.Bloques)}]\r\n" +
            "Referencias guardadas en el i-nodo; los bloques no contienen Siguiente.\r\n" +
            "Acceso al bloque lógico k: archivo.Bloques[k].";
        ActualizarMapa();
    }

    private void MostrarDisco()
    {
        Actualizar();
        Registrar($"Estado del disco: {TotalBloques - Libres} ocupados, {Libres} libres.");

        // Ventana de consulta del disco virtual: no modifica los bloques.
        using var ventana = new Form
        {
            Text = "AMS.OS - Detalle del disco virtual",
            StartPosition = FormStartPosition.CenterParent,
            MinimumSize = new Size(640, 520),
            ClientSize = new Size(760, 620),
            BackColor = fondo,
            Font = new Font("Segoe UI", 9.5f)
        };

        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        ventana.Controls.Add(contenedor);

        contenedor.Controls.Add(new Label
        {
            Text = "ESTADO DETALLADO DEL DISCO · BLOQUES 0 AL 19",
            Dock = DockStyle.Fill,
            ForeColor = verde,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        contenedor.Controls.Add(new Label
        {
            Text = $"Total: {TotalBloques} bloques     Ocupados: {TotalBloques - Libres}" +
                   $"     Libres: {Libres}     Archivos / i-nodos: {archivos.Count}",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = verde,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var tablaDisco = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            EnableHeadersVisualStyles = false
        };
        tablaDisco.ColumnHeadersDefaultCellStyle.BackColor = verdeClaro;
        tablaDisco.ColumnHeadersDefaultCellStyle.ForeColor = verde;
        tablaDisco.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9.5f, FontStyle.Bold);
        tablaDisco.DefaultCellStyle.SelectionBackColor = Color.Khaki;
        tablaDisco.DefaultCellStyle.SelectionForeColor = verde;
        tablaDisco.Columns.Add("Bloque", "Bloque");
        tablaDisco.Columns.Add("Estado", "Estado");
        tablaDisco.Columns.Add("Archivo", "Archivo propietario");
        tablaDisco.Columns.Add("INodo", "I-nodo");

        for (int i = 0; i < TotalBloques; i++)
        {
            string? propietario = disco[i];
            string idINodo = "—";
            if (propietario != null && archivos.TryGetValue(propietario, out var archivo))
                idINodo = $"#{archivo.Id}";

            int indice = tablaDisco.Rows.Add(
                $"{i:00}",
                propietario == null ? "Libre" : "Ocupado",
                propietario ?? "—",
                idINodo);

            tablaDisco.Rows[indice].DefaultCellStyle.BackColor =
                propietario == null ? Color.FromArgb(236, 239, 234) :
                string.Equals(propietario, seleccionado, StringComparison.OrdinalIgnoreCase)
                    ? Color.Khaki : verdeClaro;
        }

        tablaDisco.ClearSelection();
        contenedor.Controls.Add(tablaDisco, 0, 2);
        contenedor.Controls.Add(
            Boton("Regresar al simulador", () => ventana.Close()), 0, 3);

        ventana.ShowDialog(this);
    }

    private void MostrarLibres()
    {
        string mensaje = $"Hay {Libres} bloques libres de {TotalBloques}.";
        Registrar(mensaje);
        MessageBox.Show(mensaje, "Espacio disponible", MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void CargarEjemplo()
    {
        if (archivos.Count != 0 && MessageBox.Show(
                "Se borrará la simulación actual. ¿Cargar el ejemplo?",
                "Confirmación", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;
        Limpiar();
        foreach (var ejemplo in new[] { ("A.txt", 4), ("B.txt", 3),
                                         ("C.txt", 5), ("D.txt", 2) })
        {
            txtNombre.Text = ejemplo.Item1;
            numBloques.Value = ejemplo.Item2;
            Crear();
        }
        seleccionado = null;
        txtNombre.Clear();
        txtDetalle.Text = "Ejemplo cargado: 14 bloques ocupados y 6 libres.";
        Actualizar();
    }

    private void Reiniciar()
    {
        if (MessageBox.Show("¿Eliminar todos los i-nodos del disco simulado?",
                "Reiniciar", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;
        Limpiar();
        txtNombre.Clear();
        txtDetalle.Clear();
        historial.Items.Clear();
        Actualizar();
        Registrar("Simulación reiniciada.");
    }

    private void Limpiar()
    {
        Array.Clear(disco, 0, disco.Length);
        archivos.Clear();
        siguienteINodo = 1;
        seleccionado = null;
    }

    private void Actualizar()
    {
        lblResumen.Text = $"Total: 20 bloques     Ocupados: {TotalBloques - Libres}" +
                          $"     Libres: {Libres}     Archivos / i-nodos: {archivos.Count}";
        actualizandoTabla = true;
        try
        {
            tabla.Rows.Clear();
            foreach (var a in archivos.Values.OrderBy(a => a.Id))
                tabla.Rows.Add($"#{a.Id}", a.Nombre, a.Bloques.Count,
                               string.Join(", ", a.Bloques));
            tabla.ClearSelection();
        }
        finally { actualizandoTabla = false; }
        ActualizarMapa();
    }

    private void ActualizarMapa()
    {
        for (int i = 0; i < TotalBloques; i++)
        {
            var dueño = disco[i];
            bool destacado = dueño != null &&
                string.Equals(dueño, seleccionado, StringComparison.OrdinalIgnoreCase);
            bloquesVisuales[i].Text = $"{i:00}\n{dueño ?? "Libre"}";
            bloquesVisuales[i].BackColor = destacado ? Color.Khaki :
                dueño == null ? Color.FromArgb(236, 239, 234) : verdeClaro;
            bloquesVisuales[i].ForeColor = verde;
        }
    }

    private void Registrar(string mensaje)
    {
        historial.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {mensaje}");
        lblEstado.Text = mensaje;
    }

    private void Error(string mensaje)
    {
        Registrar("Error: " + mensaje);
        MessageBox.Show(mensaje, "AMS.OS - I-nodos", MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}