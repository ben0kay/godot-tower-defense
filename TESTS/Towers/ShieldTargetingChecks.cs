// Shared preferred-target scoring and actual Laser acquisition, with real route/fallback regression coverage.
using Godot;
using System;
using System.Reflection;
using static AmmoLoopFixture;

public partial class ShieldTargetingChecks : Node
{
    #region Integration
    // =========================================================
    // Run isolated production fixtures; no profile writes, extra scans or speculative stealth behavior.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Vector2 origin = new(2000, 2000);
                Enemy ordinary = f.Target(origin + new Vector2(0, -40)), shielded = f.Target(origin + new Vector2(0, -120));
                shielded.Definition.MaxShield = 100; shielded.RestoreShield(100);
                Require(TowerTargeting.Find(f.Spawns.Registry, origin, 200, TowerTargeting.Priority.Closest) == ordinary, "Other towers retain unpreferred closest");
                Require(TowerTargeting.Find(f.Spawns.Registry, origin, 60, TowerTargeting.Priority.Closest, preference: TowerTargeting.Preference.Shielded) == ordinary,
                    "Out-of-range shields cannot suppress valid fallback");
                LaserTower laser = f.Add<LaserTower>("laser", origin);
                Require(laser.Config.TargetPreference == TowerTargeting.Preference.Shielded, "Laser defaults to shield preference");
                FieldInfo target = typeof(LaserTower).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
                TowerFinalStats cache = laser.FinalStats;
                laser._PhysicsProcess(1);
                Require(target.GetValue(laser) == shielded, "Actual Laser picks shield before closer ordinary enemy");
                shielded.TakeDamage(100);
                laser._PhysicsProcess(laser.Config.TargetSearchInterval);
                Require(target.GetValue(laser) == ordinary && ReferenceEquals(cache, laser.FinalStats), "Next normal search falls back without stat rebuild");
                shielded.RestoreShield(100); laser.Config.TargetPreference = TowerTargeting.Preference.None;
                laser._PhysicsProcess(laser.Config.TargetSearchInterval);
                Require(target.GetValue(laser) == ordinary && ((LaserTowerConfig)f.Placement.Catalog.Find("laser").DefaultConfig).TargetPreference == TowerTargeting.Preference.Shielded,
                    "Private opt-out does not mutate authored Laser defaults");
                shielded.Free(); ordinary.Free(); laser.Free();
            }
            await JourneyTargetingChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("ShieldTargetingChecks: PASS shared shield preference, six modes, routed First/Last, fallback and live Laser acquisition"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion
}
