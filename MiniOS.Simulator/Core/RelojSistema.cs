namespace MiniOS.Simulator;

public sealed class RelojSistema
{
    public TimeSpan Tiempo { get; private set; }
    public void AvanzarSegundo() => Tiempo += TimeSpan.FromSeconds(1);
    public void Reiniciar() => Tiempo = TimeSpan.Zero;
}
