// Real purchased factory -> physical drone -> depot -> physical drone -> firing tower loop.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class AmmunitionLoopChecks : Node
{
    // =========================================================
    // Verify old foundations first, then actual gameplay services without campaign save/profile dependencies.
    public override async void _Ready()
    {
        try
        {
            AmmoFoundationChecks.Run(); GD.Print("PASS: existing ammunition foundations reused");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            WeaponAmmunitionChecks.Run(this);
            using (AmmoLoopFixture f = new(this))
            {
                AmmunitionFactory factory = f.Purchase<AmmunitionFactory>("ammo_factory");
                Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == 800, "Factory construction pays 200 Credits");
                Require(factory.FinalStats == null && factory.Ammunition == null, "Factory remains passive");
                factory._PhysicsProcess(8); factory._PhysicsProcess(50);
                Require(factory.Inventory.Amount == 1000 && factory.Production.IsPaused && factory.ProducedRounds == 1000 && f.Logistics.Jobs.Count == 0,
                    "No depot: auto production fills finite buffer and pauses without lost batches");
                AmmunitionDepot depot = f.Purchase<AmmunitionDepot>("ammo_depot");
                BattleTower tower = f.Purchase<BattleTower>("battle");
                Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == 450, "Depot/Battle use existing paid placement");
                Require(tower.Ammunition.Inventory.Amount == 1000 && f.AccountedRounds() == 1000 + f.ProducedRounds(), "Explicit starting stock conservation");
                f.Dispatch(); AmmoDeliveryJob incoming = f.Logistics.Jobs.Single();
                Require(incoming.Source == factory && incoming.Destination == depot && incoming.Cargo.Amount == 1000 && depot.Inventory.Amount == 0,
                    "Factory launches visible loaded drone; depot receives nothing at dispatch");
                Require(f.Logistics.ReservedSpace(depot) == 1000 && f.Logistics.BusySlots(factory) == 1, "Capacity and source slot reserved");
                Vector2 start = incoming.Drone.GlobalPosition; incoming.Drone._PhysicsProcess(.001);
                Require(incoming.Drone.GlobalPosition != start && depot.Inventory.Amount == 0, "Drone actually travels before unloading");
                factory._PhysicsProcess(2); Require(factory.Inventory.Amount == 100 && !factory.Production.IsPaused, "Loading frees production buffer and resumes it");
                f.Arrive(incoming);
                Require(depot.Inventory.Amount == 1000 && incoming.Cargo.Amount == 0 && incoming.Drone.Returning && f.Logistics.BusySlots(factory) == 1,
                    "Only arrival unloads once; source slot stays busy on return");
                f.Return(incoming); Require(f.Logistics.BusySlots(factory) == 0, "Returning home releases factory slot");
                tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
                Enemy enemy = f.Target(tower.GlobalPosition + new Vector2(100, 0));
                for (int i = 0; i < 900; i++) { tower._PhysicsProcess(2); f.ClearProjectiles(); }
                Require(tower.Ammunition.Inventory.Amount == 100 && tower.Ammunition.ConsumedRounds == 900 && tower.Ammunition.ResupplyRequested,
                    "900 actual Battle launches reach global 10% threshold and one demand");
                f.Dispatch(); AmmoDeliveryJob refill = f.Logistics.Jobs.Single(j => j.Destination == tower);
                Require(refill.Source == depot && tower.Ammunition.Inventory.Amount == 100 && refill.Cargo.Amount == 250, "Depot loads physical stock, never refills at dispatch");
                f.Dispatch(); Require(f.Logistics.Jobs.Count(j => j.Destination == tower) == 1, "No duplicate tower job during repeated dispatch");
                f.Arrive(refill); Require(tower.Ammunition.Inventory.Amount == 350, "Tower refills only on drone arrival");
                f.Return(refill); Require(!tower.Ammunition.DeliveryInFlight && !tower.Ammunition.ResupplyRequested, "Request rearms after completed refill");
                Require(f.AccountedRounds() == 1000 + f.ProducedRounds(), "Complete loop conserves produced, stored, consumed and carried ammo");
                f.Placement.Call("SelectTower", tower); tower.Ammunition.Inventory.TryConsume(1);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/RuntimeStats").Text.Contains("349"), "Selected tower ammo updates by event");
                f.Placement.Call("SelectTower", factory);
                Require(f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/RuntimeStats").Text.Contains("Buffered ammo"), "Factory Inspector shows runtime buffer");
                f.Placement.Call("SelectTower", depot);
                Require(f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/RuntimeStats").Text.Contains("Stored ammo"), "Depot Inspector shows runtime stock");
            }
            DeliverySafetyChecks.Run(this);
            await LiveLoop();
            using (AmmoLoopFixture second = new(this, "res://MainLevel002.tscn"))
                Require(second.Hud.GetBuildButton("ammo_factory") != null && second.Hud.GetBuildButton("ammo_depot") != null && second.Logistics != null,
                    "Both levels expose buildable ammunition pipeline");
            GD.Print("PASS: purchased factory/depot/Battle, production pause/resume, both visible physical legs, 900 real shots, 10% demand, arrival-only refill, Inspector and conservation");
            GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // A short real-physics run proves automatic callbacks, including pause and resume, beyond explicit test ticks.
    private async System.Threading.Tasks.Task LiveLoop()
    {
        using AmmoLoopFixture f = new(this);
        AmmunitionFactory factory = f.Add<AmmunitionFactory>("ammo_factory", new(2000, 2000), c =>
        { var a = (AmmunitionFactoryConfig)c; a.BatchAmount = 10; a.InternalBufferCapacity = 20;
          a.ProductionSecondsPerBatch = .05f; a.DroneSpeed = 10000; a.DronePayload = 10; a.ShipmentAmount = 10; });
        f.Add<AmmunitionDepot>("ammo_depot", new(2100, 2000), c =>
        { var d = (AmmunitionDepotConfig)c; d.StorageCapacity = 100; d.DroneSpeed = 10000; d.DronePayload = 10; });
        BattleTower tower = f.Add<BattleTower>("battle", new(2200, 2000), c =>
        { c.InitialAmmoFraction = 0; c.SetLevelStats(1, c.Levels[0].Stats with { Ammunition = new(100, 1) }); });
        f.Logistics.SchedulingChecksPerInterval = 1; // Even a one-check budget must alternate fairly between both legs.
        f.Logistics.DispatchIntervalSeconds = .05f; f.Logistics.SetPhysicsProcess(true); factory.SetPhysicsProcess(true);
        await ToSignal(GetTree().CreateTimer(.1), SceneTreeTimer.SignalName.Timeout);
        PauseMenu pause = f.Main.GetNode<PauseMenu>("PauseMenu"); pause.Open();
        long produced = factory.ProducedRounds; var flights = f.Logistics.Jobs.ToDictionary(j => j.Drone, j => j.Drone.GlobalPosition);
        await ToSignal(GetTree().CreateTimer(.1), SceneTreeTimer.SignalName.Timeout);
        Require(factory.ProducedRounds == produced && flights.All(pair => pair.Key.GlobalPosition == pair.Value), "Pause freezes production and active drone motion");
        pause.Resume(); await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        Require(factory.ProducedRounds > produced && tower.Ammunition.Inventory.Amount > 0 && f.Logistics.DeliveredRounds > 0,
            "Automatic physics runs both delivery legs and resumes production");
        Require(f.AccountedRounds() == f.ProducedRounds(), "Real-frame loop conserves all generated stock");
        GD.Print("PASS: automatic production/dispatch/drone callbacks and pause/resume conserve ammunition in real physics frames");
    }

}
