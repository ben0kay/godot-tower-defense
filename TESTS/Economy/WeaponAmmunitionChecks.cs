// Optional level stats, independent reserves, discrete attack charging and frame-rate-independent paid beam time.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

internal static class WeaponAmmunitionChecks
{
    // =========================================================
    // Pure beam funding is independent of 30/60/120Hz and cannot deliver free tail damage while empty.
    internal static void Run(Node parent)
    {
        foreach (int hz in new[] { 30, 60, 120 })
        {
            TowerAmmunition ammo = new(new(3, 0, 2), 1); double funded = 0;
            for (int i = 0; i < hz * 3; i++) funded += ammo.FundBeamSeconds(1.0 / hz);
            Require(Math.Abs(funded - 1.5) < 1e-7 && ammo.ConsumedRounds == 3 && ammo.Inventory.Amount == 0, "Beam time funding at " + hz + "Hz");
        }
        Require(new TowerAmmunition(new(10, 0, 0), 1).FundBeamSeconds(1) == 0, "Invalid zero beam rate cannot supply free firing time");
        TowerAmmunition snapshot = new(new(1000, 1), 1); snapshot.Inventory.TryConsume(901);
        snapshot.Synchronize(new(2000, 1)); Require(snapshot.Inventory.Amount == 99, "Growing capacity never refills");
        snapshot.Synchronize(new(50, 1)); Require(snapshot.Inventory.Amount == 50 && snapshot.DiscardedRounds == 49, "Shrink policy explicitly discards excess");
        Require(new TowerAmmunition(new(1000, 1), 0).Inventory.Amount == 0, "Initial stock is configurable empty");
        BattleTowerConfig authored = new(); TowerFinalStats level3 = authored.ResolveStats(3);
        authored.SetLevelStats(2, authored.Levels[1].Stats with { Ammunition = new(7, 4) });
        Require(authored.ResolveStats(3) == level3 && level3.Ammunition == new AmmunitionStats(1000, 1), "Complete level ammo rows remain independent");
        using AmmoLoopFixture f = new(parent);
        foreach (string id in new[] { "battle", "blaster_cannon", "shockwave" })
        {
            TowerBase tower = f.Add<TowerBase>(id, new Vector2(2000, 2000), c =>
            { c.SetLevelStats(1, c.Levels[0].Stats with { AimToleranceDegrees = 180, Ammunition = new(2, 1) }); });
            Enemy enemy = f.Target(tower.GlobalPosition + new Vector2(50, 0));
            int attacks = 0;
            for (int i = 0; i < 3; i++)
            {
                tower._PhysicsProcess(10); attacks += f.Main.GetNode("World/Projectiles").GetChildCount(); f.ClearProjectiles();
            }
            Require(attacks == 2 && tower.Ammunition.Inventory.Amount == 0 && tower.Ammunition.ConsumedRounds == 2,
                id + $" charges per launch/emission and stops empty (attacks={attacks}, stock={tower.Ammunition.Inventory.Amount}, consumed={tower.Ammunition.ConsumedRounds})");
            AmmoInventory cargo = new(1, 1); Require(cargo.TryTransferTo(tower.Ammunition.Inventory, 1), "Refill uses physical transfer");
            tower._PhysicsProcess(10);
            Require(f.Main.GetNode("World/Projectiles").GetChildCount() == 1 && tower.Ammunition.Inventory.Amount == 0, id + " resumes after resupply");
            f.ClearProjectiles(); enemy.Free(); tower.Free();
        }
        LaserTower laser = f.Add<LaserTower>("laser", new Vector2(2000, 2000), c =>
            c.SetLevelStats(1, c.Levels[0].Stats with { AimToleranceDegrees = 180, Ammunition = new(3, 0, 2) }));
        Enemy target = f.Target(laser.GlobalPosition + new Vector2(50, 0));
        float before = target.Health;
        for (int i = 0; i < 120; i++) laser._PhysicsProcess(1.0 / 60);
        float depleted = target.Health;
        Require(depleted < before && laser.Ammunition.Inventory.Amount == 0 && laser.Ammunition.ConsumedRounds == 3, "Actual beam consumes by firing duration");
        for (int i = 0; i < 120; i++) laser._PhysicsProcess(1.0 / 60);
        Require(target.Health == depleted, "Empty beam cannot continue damaging for free");
        laser.Free(); target.Free();
        BattleTower upgrade = f.Add<BattleTower>("battle", new Vector2(2200, 2000));
        upgrade.Ammunition.Inventory.TryConsume(100); TowerRankController.AwardExperience(upgrade, 1500);
        upgrade.Config.SetLevelStats(2, upgrade.Config.Levels[1].Stats with { Ammunition = new(1200, 1) });
        f.Placement.Call("SelectTower", upgrade);
        Require(UpgradeTableChecks.HasRow(f.Hud, "Ammo Capacity", "1000", "1200", "(+200)"), "Inspector preview uses resolved next-level ammo");
        double xp = upgrade.RankProgress.TotalXp; int stock = upgrade.Ammunition.Inventory.Amount;
        Require(ConstructionTestClock.PurchaseAndFinish(upgrade, f.Wallet, new TowerMasterConfig { AllowUpgrades = true }, 1, out _), "Real purchased upgrade succeeds");
        Require(upgrade.Ammunition.Inventory.Amount == stock && upgrade.Ammunition.Inventory.Capacity == 1200 && upgrade.RankProgress.TotalXp == xp, "Paid levels preserve ammo and rank XP");
        GD.Print("PASS: ammunition absolute rows, initial stock, capacity policy, per-launch Battle/Blaster/Shockwave, paid beam time30/60/120, empty shutdown/refill and upgrade/rank persistence");
    }
}
