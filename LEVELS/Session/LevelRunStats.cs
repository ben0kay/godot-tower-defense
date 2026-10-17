// Level-owned event telemetry: paid placements, first construction completion, upgrades and game time.
using Godot;
using System;
using System.Collections.Generic;

public partial class LevelRunStats : Node
{
    public int TowersPlaced { get; private set; }
    public int TowersConstructed { get; private set; }
    public int UpgradesPurchased { get; private set; }
    public double MissionSeconds { get; private set; }
    public bool Frozen { get; private set; }
    private TowerPlacementController _placement;
    private readonly Dictionary<TowerBase, (Action Work, Action Upgrade, Action Exit)> _subscriptions = new();

    // =========================================================
    // One lightweight clock per mission; no scene scans or per-tower frame polling.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Pausable; SetProcess(false);
        _placement = GetNode<TowerPlacementController>("../../TowerPlacement");
        _placement.TowerPlaced += OnTowerPlaced;
    }

    // =========================================================
    // Pauses and Results never accumulate mission time, even for explicit test calls.
    public override void _PhysicsProcess(double delta)
    {
        if (!Frozen && !GetTree().Paused && double.IsFinite(delta) && delta > 0)
            MissionSeconds = Math.Min(TimeSpan.MaxValue.TotalSeconds - 1, MissionSeconds + delta);
    }

    // =========================================================
    // The placement event is emitted only after wallet/occupancy commit succeeds.
    private void OnTowerPlaced(TowerBase tower)
    {
        if (Frozen || !GodotObject.IsInstanceValid(tower) || _subscriptions.ContainsKey(tower)) return;
        TowersPlaced++;
        bool constructed = tower.IsOperational;
        if (constructed) TowersConstructed++;
        Action work = () => { if (!Frozen && !constructed && tower.IsOperational) { constructed = true; TowersConstructed++; } };
        Action upgrade = () => { if (!Frozen) UpgradesPurchased++; };
        Action exit = () => Forget(tower);
        _subscriptions.Add(tower, (work, upgrade, exit));
        tower.WorkCompleted += work; tower.UpgradePurchased += upgrade; tower.TreeExiting += exit;
    }

    // =========================================================
    // Freeze immediately when an outcome latches; deferred cleanup cannot change counters.
    public void Freeze() { Frozen = true; SetPhysicsProcess(false); }

    // =========================================================
    // Removing a tower retires observers without subtracting historical counts.
    private void Forget(TowerBase tower)
    {
        if (!_subscriptions.Remove(tower, out var handlers) || !GodotObject.IsInstanceValid(tower)) return;
        tower.WorkCompleted -= handlers.Work; tower.UpgradePurchased -= handlers.Upgrade; tower.TreeExiting -= handlers.Exit;
    }

    // =========================================================
    // Scene transitions leave no strong tower references or subscribed menu/session callbacks.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_placement)) _placement.TowerPlaced -= OnTowerPlaced;
        foreach (TowerBase tower in new List<TowerBase>(_subscriptions.Keys)) Forget(tower);
    }
}
