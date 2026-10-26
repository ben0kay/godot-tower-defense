// Typed between-wave source state. Derived combat stats and authored resources are deliberately excluded.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed record LevelSaveData
{
    public string LevelId { get; init; } = "";
    public string ContentHash { get; init; } = "";
    public int CompletedWaves { get; init; }
    public ulong WaveRandomState { get; init; }
    public Dictionary<string, int> Balances { get; init; } = new();
    public ResearchSaveData Research { get; init; } = new();
    public MissionRewardTicket RewardTicket { get; init; }
    public RunSaveData Run { get; init; } = new();
    public List<CitadelSaveData> Citadels { get; init; } = new();
    public List<TowerSaveData> Towers { get; init; } = new();
    public LogisticsSaveData Logistics { get; init; } = new();
    public double BuilderRetry { get; init; }
    public List<BuilderSaveData> Builders { get; init; } = new();
    public SavePoint CameraPosition { get; init; }
    public float CameraZoom { get; init; }
}
public readonly record struct SavePoint(float X, float Y)
{
    public static SavePoint From(Godot.Vector2 p) => new(p.X, p.Y);
    public Godot.Vector2 Vector() => new(X, Y);
}
public sealed record ResearchSaveData(string ProfileId, double CitadelHealthPercent, Dictionary<string, TowerResearchBonuses> Towers)
{
    public ResearchSaveData() : this("", 0, new()) { }
}
public sealed record RunSaveData(int Killed, int Breached, int Placed, int Constructed, int Upgrades, double Seconds)
{
    public RunSaveData() : this(0, 0, 0, 0, 0, 0) { }
}
public sealed record CitadelSaveData(string Id, int Health, int Credits, double ProductionSeconds, bool CriticalLatched);
public sealed record DisableSaveData(TowerDisableReason Reason, double? Remaining, string Source);
public sealed record TowerSaveData
{
    public string Id { get; init; } = "";
    public string TypeId { get; init; } = "";
    public int X { get; init; }
    public int Y { get; init; }
    public int Tier { get; init; }
    public double XP { get; init; }
    public float Health { get; init; }
    public TowerTargeting.Priority? Priority { get; init; }
    public TowerWorkState Work { get; init; }
    public bool Held { get; init; }
    public float Heat { get; init; }
    public bool Overheated { get; init; }
    public List<DisableSaveData> Disables { get; init; } = new();
    public int? Ammo { get; init; }
    public long Consumed { get; init; }
    public long Discarded { get; init; }
    public double BeamCredit { get; init; }
    public int? StoredCredits { get; init; }
    public double CreditSeconds { get; init; }
    public double AmmoSeconds { get; init; }
    public long Produced { get; init; }
    public double WeaponCooldown { get; init; }
}
public sealed record DeliverySaveData(string Source, string Destination, int Cargo, int Reserved, bool Returning, SavePoint Position, float Speed);
public sealed record LogisticsSaveData
{
    public List<DeliverySaveData> Jobs { get; init; } = new();
    public List<string> Pending { get; init; } = new();
    public List<string> Factories { get; init; } = new();
    public List<string> Depots { get; init; } = new();
    public int FactoryCursor { get; init; }
    public int RequestCursor { get; init; }
    public bool FactoryTurn { get; init; }
    public double Elapsed { get; init; }
    public long Lost { get; init; }
    public long Delivered { get; init; }
}
public sealed record BuilderSaveData
{
    public string Tower { get; init; } = "";
    public string HomeId { get; init; } = "";
    public bool Queued { get; init; }
    public ConstructionDronePhase Phase { get; init; }
    public SavePoint Home { get; init; }
    public SavePoint Target { get; init; }
    public SavePoint Position { get; init; }
    public float HalfWidth { get; init; }
    public float Hover { get; init; }
    public float Height { get; init; }
    public float Age { get; init; }
    public float Deploy { get; init; }
}
