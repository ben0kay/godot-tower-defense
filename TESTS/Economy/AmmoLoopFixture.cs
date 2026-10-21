// Test-only production-world helpers shared by the small ammunition regression groups.
using Godot;
using System;
using System.Linq;

internal sealed class AmmoLoopFixture : IDisposable
{
    internal Node Main { get; }
    internal Node Towers => Main.GetNode("World/Towers");
    internal SpawnManager Spawns => Main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
    internal AmmunitionLogistics Logistics => Main.GetNode<AmmunitionLogistics>("World/AmmunitionLogistics");
    internal TowerPlacementController Placement => Main.GetNode<TowerPlacementController>("TowerPlacement");
    internal GameplayHUD Hud => Main.GetNode<GameplayHUD>("UI/GameplayHUD");
    internal EconomyManager Wallet => Main.GetNode<EconomyManager>("Economy");
    internal AmmoLoopFixture(Node parent, string scene = "res://Main.tscn")
    {
        Main = GD.Load<PackedScene>(scene).Instantiate(); parent.AddChild(Main); Disable(Main);
        var builders = Main.GetNodeOrNull<ConstructionDroneManager>("World/ConstructionDrones");
        if (builders != null) builders.Config = new ConstructionDroneMasterConfig { Enabled = false };
    }

    // =========================================================
    // Explicit ticks keep transfer observations independent of the headless renderer's frame rate.
    internal static void Disable(Node node)
    { node.SetProcess(false); node.SetPhysicsProcess(false); foreach (Node child in node.GetChildren()) Disable(child); }
    internal void Tick(double delta)
    {
        foreach (AmmunitionFactory factory in Towers.GetChildren().OfType<AmmunitionFactory>()) factory._PhysicsProcess(delta);
        Logistics._PhysicsProcess(delta);
        foreach (AmmoDeliveryJob job in Logistics.Jobs.ToArray())
        { job.Drone.SetPhysicsProcess(false); job.Drone._PhysicsProcess(delta); }
    }
    internal void Dispatch() { Logistics._PhysicsProcess(.25); foreach (AmmoDeliveryJob job in Logistics.Jobs) job.Drone.SetPhysicsProcess(false); }
    internal void Arrive(AmmoDeliveryJob job)
    { job.Drone._PhysicsProcess(job.Drone.GlobalPosition.DistanceTo(job.Destination.GlobalPosition) / job.Drone.Speed + .01); }
    internal void Return(AmmoDeliveryJob job)
    { job.Drone._PhysicsProcess(job.Drone.GlobalPosition.DistanceTo(job.Source.GlobalPosition) / job.Drone.Speed + .01); }

    // =========================================================
    // Exercise real build buttons and the authoritative paid placement transaction.
    internal T Purchase<T>(string id, bool completeConstruction = true) where T : TowerBase
    {
        Placement.Call("CancelPlacement");
        Hud.GetBuildButton(id).EmitSignal(Button.SignalName.Pressed);
        WorldGrid grid = Main.GetNode<WorldGrid>("World/Grid");
        int before = Towers.GetChildCount();
        foreach (Vector2I cell in grid.Level.Passable.GetUsedCells().Concat(grid.Level.Impassable.GetUsedCells()).Concat(grid.Level.Interactive.GetUsedCells()).Distinct())
        {
            if (grid.PlacementFailure(cell, Placement.Catalog.Find(id).Footprint).Length != 0 || Spawns.PlacementFailure(cell, Placement.Catalog.Find(id).Footprint).Length != 0) continue;
            Placement.Call("TryPlace", cell);
            if (Towers.GetChildCount() == before) continue;
            T tower = Towers.GetChild<T>(Towers.GetChildCount() - 1);
            if (completeConstruction) ConstructionTestClock.FinishInitial(Main);
            Placement.Call("SelectTower", tower); // Explicit inspection is separate from the placement action.
            Disable(tower); return tower;
        }
        throw new InvalidOperationException("Could not purchase " + id);
    }

    // =========================================================
    // Isolated stock/weapon fixtures still use catalog configs, actual scenes and private instance copies.
    internal T Add<T>(string id, Vector2 position, Action<TowerConfig> configure = null) where T : TowerBase
    {
        TowerDefinition definition = Placement.Catalog.Find(id);
        T tower = definition.Scene.Instantiate<T>(); tower.InitializeDefinition(definition);
        if (tower.AuthoredConfig is BattleTowerConfig battle) battle.TargetPriority = TowerTargeting.Priority.Closest;
        if (tower.AuthoredConfig is BlasterCannonConfig blaster) blaster.TargetPriority = TowerTargeting.Priority.Closest;
        if (tower.AuthoredConfig is LaserTowerConfig laser) laser.TargetPriority = TowerTargeting.Priority.Closest;
        configure?.Invoke(tower.AuthoredConfig); Towers.AddChild(tower); tower.GlobalPosition = position; Disable(tower); return tower;
    }
    internal Enemy Target(Vector2 position)
    {
        Enemy enemy = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        enemy.Definition = (EnemyDefinition)enemy.Definition.Duplicate(true);
        enemy.Definition.MaxHealth = 10000; enemy.Definition.KillRewardCredits = 0;
        Main.GetNode("World/Enemies").AddChild(enemy); enemy.GlobalPosition = position;
        enemy.Initialize(Spawns.Registry, Spawns.Paths); Disable(enemy); return enemy;
    }
    internal void ClearProjectiles()
    { foreach (Node child in Main.GetNode("World/Projectiles").GetChildren()) child.Free(); }
    internal long AccountedRounds() => Towers.GetChildren().OfType<TowerBase>().Sum(t =>
        (long)(t is AmmoBuilding b ? b.Inventory.Amount : t.Ammunition?.Inventory.Amount ?? 0) +
        (t.Ammunition?.ConsumedRounds ?? 0) + (t.Ammunition?.DiscardedRounds ?? 0)) +
        Logistics.Jobs.Sum(job => (long)job.Cargo.Amount) + Logistics.LostRounds;
    internal long ProducedRounds() => Towers.GetChildren().OfType<AmmunitionFactory>().Sum(f => f.ProducedRounds);
    public void Dispose() { if (GodotObject.IsInstanceValid(Main)) Main.Free(); }
    internal static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
