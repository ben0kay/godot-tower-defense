// Authored Battle heat timings through actual cadence, cooling, live config edits and per-level previews.
using Godot;
using System;
using static AmmoLoopFixture;

public partial class BattleHeatTimingChecks : Node
{
    // =========================================================
    // Time is explicitly stepped in physics order; all bullets are freed without damage to retain a steady lock.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                foreach (int rate in new[] { 30, 60, 120 })
                    foreach (double minutes in new[] { 3d, 3.5 })
                        for (int level = 1; level <= 3; level++) Simulate(f, rate, minutes, level);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: Battle authored heat enabled; minutes3/3.5 →180/210s across levels1–3 at30/60/120Hz, nine-second recovery, idle cooling, live tuning, independent config/cache");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    // =========================================================
    // Use the selected level snapshot as row1 in this timing fixture; real paid level behavior has its own suite.
    private static void Simulate(AmmoLoopFixture f, int rate, double minutes, int level)
    {
        BattleTower tower = f.Add<BattleTower>("battle", new(2000,2128), c =>
        {
            var config = (BattleTowerConfig)c;
            Require(config.HeatEnabled && config.OverheatAfterMinutes == 3, "Authored Battle defaults activate three-minute heat");
            config.OverheatAfterMinutes = minutes; config.SetLevelStats(1, config.Levels[level - 1].Stats);
        });
        tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
        Enemy enemy = f.Target(new(2000,2000));
        TowerFinalStats cache = tower.FinalStats; double target = minutes * 60, elapsed = 0;
        int limit = (int)((target + 3) * rate);
        for (int i = 0; i < limit && !tower.IsOverheated; i++)
        {
            tower.AdvanceConditions(1d / rate); tower._PhysicsProcess(1d / rate); f.ClearProjectiles(); elapsed += 1d / rate;
        }
        Require(tower.IsOverheated && Math.Abs(elapsed - target) < 2,
            $"Actual sustained heat duration {elapsed:0.###}s vs {target}s, level{level} at{rate}Hz");
        Require(ReferenceEquals(cache, tower.FinalStats), "No per-tick heat stat rebuild");
        int stock = tower.Ammunition.Inventory.Amount; double recovery = 0;
        while (tower.IsOverheated && recovery < 10) { tower.AdvanceConditions(1d / rate); recovery += 1d / rate; }
        Require(Math.Abs(recovery - 9) < .1 && tower.Ammunition.Inventory.Amount == stock, "Nine-second recovery uses no ammunition");
        float before = tower.CurrentHeat; tower.AdvanceConditions(1); Require(tower.CurrentHeat < before, "Real firing break cools heat");
        tower.Config.OverheatAfterMinutes = 3.5;
        Require(tower.FinalStats.Heat.HasValue && ((BattleTowerConfig)f.Placement.Catalog.Find("battle").DefaultConfig).OverheatAfterMinutes == 3,
            "Minute tuning updates private effective stats without changing source preset");
        tower.Config.HeatEnabled = false; Require(tower.CurrentHeat == 0 && tower.FinalStats.Heat == null, "Explicit switch disables minute-based heat");
        GD.Print($"BATTLE HEAT {rate}Hz L{level}: {minutes}min →{elapsed:0.###}s, recovery{recovery:0.###}s");
        tower.Free(); enemy.Free(); f.ClearProjectiles();
    }
}
