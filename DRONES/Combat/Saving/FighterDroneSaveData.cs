// Minimal FD1-B station-owned snapshots. No stock grants, purchases or launch callbacks replay on load.
using System.Collections.Generic;
public sealed record FighterStationSaveData
{
    public List<FighterDroneSaveData> Fighters { get; init; } = new();
}
public sealed record FighterDroneSaveData
{
    public string Id { get; init; } = "";
    public int Slot { get; init; }
    public float Health { get; init; }
    public int Ammo { get; init; }
    public FighterDroneState State { get; init; }
    public float Heading { get; init; }
    public SavePoint Position { get; init; }
    public SavePoint Velocity { get; init; }
    public float ManoeuvrePhase { get; init; }
    public DroneFlightMotion Motion { get; init; }
}
