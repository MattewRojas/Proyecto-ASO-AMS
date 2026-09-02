namespace MiniOS.Simulator;

public sealed record EventoSistema(TimeSpan Tiempo, string Mensaje);

public sealed class RegistroEventos
{
    public List<EventoSistema> Eventos { get; } = [];
    public void Agregar(TimeSpan tiempo, string mensaje) => Eventos.Add(new(tiempo, mensaje));
}
