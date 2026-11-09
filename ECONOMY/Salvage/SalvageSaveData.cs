// Between-wave material state. Reservations are rebuilt from the owning jobs.
using System.Collections.Generic;

public sealed record SalvageSaveData
{
    public ulong RandomState { get; init; }
    public long NextDrop { get; init; }
    public long LostScrap { get; init; }
    public long LostMetal { get; init; }
    public List<ScrapDropSaveData> Drops { get; init; } = new();
    public List<MaterialStockSaveData> Stocks { get; init; } = new();
    public List<MaterialJobSaveData> Jobs { get; init; } = new();
    public Dictionary<string, int> Holding { get; init; } = new();
}

public sealed record ScrapDropSaveData(long Id, SavePoint Position, double Remaining);
public sealed record MaterialStockSaveData(string Id, int Scrap, int Metal, bool Processing, double BatchElapsed);
public sealed record MaterialJobSaveData(int Role, int Leg, string Home, string Refinery, string Storage, string Site,
    long Drop, int Cargo, int Required, double Hover, SavePoint Position, DroneFlightMotion Motion);
