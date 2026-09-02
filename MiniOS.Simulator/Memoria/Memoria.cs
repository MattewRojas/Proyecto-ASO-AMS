namespace MiniOS.Simulator;

public sealed class Memoria
{
    public int TotalMB { get; } = 4096;
    public int UsadaMB { get; private set; }
    public int DisponibleMB => TotalMB - UsadaMB;
    public int Porcentaje => TotalMB == 0 ? 0 : UsadaMB * 100 / TotalMB;
    public bool Reservar(int mb) { if (mb > DisponibleMB) return false; UsadaMB += mb; return true; }
    public void Liberar(int mb) => UsadaMB = Math.Max(0, UsadaMB - mb);
}
