namespace MiniOS.Simulator;

public static class TemaMiniOS
{
    public static readonly Color Verde = ColorTranslator.FromHtml("#7B9669");
    public static readonly Color Fondo = ColorTranslator.FromHtml("#E6E6E6");
    public static readonly Color VerdeAzulado = ColorTranslator.FromHtml("#6C8480");
    public static readonly Color VerdeClaro = ColorTranslator.FromHtml("#BAC8B1");
    public static readonly Color VerdeOscuro = ColorTranslator.FromHtml("#404E3B");
    public static readonly Color Blanco = Color.White;

    public static void Aplicar(Form formulario)
    {
        formulario.BackColor = Fondo;
        formulario.ForeColor = VerdeOscuro;
        formulario.Font = new Font("Segoe UI", 9.5f);
    }
}
