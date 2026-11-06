// Minimal FD1-B station-owned snapshots. No stock grants, purchases or launch callbacks replay on load.
using System.Collections.Generic;
public sealed record FighterStationSaveData
{
    public double ServiceElapsed { get; init; }
    public long ServiceSerial { get; init; }
    public List<FighterDroneSaveData> Fighters { get; init; } = new();
}
public sealed record FighterDroneSaveData
{
    public long? RoundsFired { get; init; }
    public FighterServiceSaveData Service { get; init; }
    public string Id { get; init; } = "";
    public int Slot { get; init; }
    public float Health { get; init; }
    public int Ammo { get; init; }
    public FighterDroneState State { get; init; }
    public float Heading { get; init; }
    public SavePoint Position { get; init; }
    public SavePoint Velocity { get; init; }
    public float ManoeuvrePhase { get; init; }
    public double WeaponCooldown { get; init; } // Optional old checkpoint field; absent defaults to zero.
    public DroneFlightMotion Motion { get; init; }
}

public enum FighterReturnReason { None, AutoService, ManualRecall, StationDisabled }
// Source quantities only; queue reason and prepaid fractional repair credit survive interruptions/load.
public sealed record FighterServiceSaveData
{
    public FighterReturnReason Reason { get; set; }
    public bool VisitStarted { get; set; }
    public int MissingAtDock { get; set; }
    public int AmmoTransferred { get; set; }
    public double AmmoFraction { get; set; }
    public double RepairCredit { get; set; }
    public long QueueOrder { get; set; }
    public bool ManualResourceOverride { get; set; }
}
