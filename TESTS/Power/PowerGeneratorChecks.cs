// Focused PW3 fixture: paid construction, passive generation, shared disabling and checkpoint restoration without simulation.
using Godot;
using System;

public partial class PowerGeneratorChecks : Node
{
    // =========================================================
    // Explicit work commands exercise production owners; this is optional local validation, not a stress benchmark.
    public override void _Ready()
    {
        try
        {
            using AmmoLoopFixture f = new(this);
            f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
            PowerGridManager power = f.Main.GetNode<PowerGridManager>("World/PowerNetworks");
            double baseline = power.Totals.Supply;
            PowerGenerator generator = f.Purchase<PowerGenerator>("industrial_generator", completeConstruction: false);
            Require(generator.IsBusy && generator.CurrentOutput == 0 && power.Totals.Supply == baseline,
                "Unfinished generator supplies no power and uses existing paid work");
            Require(generator.Footprint == new Vector2I(2, 2) && generator.MaxHealth == 1200 && generator.NextUpgrade == null &&
                generator.FinalStats == null && !generator.IsProcessing() && !generator.IsPhysicsProcessing(),
                "Passive 2x2 generator has authored hull but no fake weapon/upgrade/tick");
            Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == 700, "One build pays the ordinary 300-Credit recipe");

            TowerSaveData pending = generator.CaptureCheckpoint(_ => ""); generator.Free();
            generator = Restore(f, pending);
            Require(generator.SaveId == pending.Id && generator.IsBusy && power.Totals.Supply == baseline &&
                f.Wallet.GetBalance(f.Wallet.CreditsResource) == 700, "Unfinished checkpoint restores identity/work without payment or premature output");
            ConstructionTestClock.FinishInitial(f.Main);
            Require(generator.CurrentOutput == 250 && generator.PowerRegion > 0 && power.Totals.Supply == baseline + 250,
                "Completion registers the correct center-based multi-cell island and adds output once");

            TowerSaveData completed = generator.CaptureCheckpoint(_ => ""); generator.Free();
            Require(power.Totals.Supply == baseline, "External removal withdraws supply immediately");
            generator = Restore(f, completed);
            Require(generator.SaveId == completed.Id && power.Totals.Supply == baseline + 250,
                "Completed checkpoint derives one registration rather than saved power stock");
            TowerDisableHandle handle = generator.ApplyDisable(TowerDisableReason.EMP);
            Require(generator.CurrentOutput == 0 && power.Totals.Supply == baseline, "Shared condition-based disabling suspends generation");
            generator.RemoveDisable(handle);
            Require(generator.CurrentOutput == 250 && power.Totals.Supply == baseline + 250, "Clearing its disable restores the same source once");
            generator.Config.Output = 375;
            Require(power.Totals.Supply == baseline + 375 &&
                ((PowerGeneratorConfig)f.Placement.Catalog.Find("industrial_generator").DefaultConfig).Output == 250,
                "Private placed config output updates never mutate authored defaults");
            generator.ApplyDamage(new DamageInfo(1000000, DamageType.Kinetic));
            Require(generator.IsDestroyed && generator.CurrentOutput == 0 && power.Totals.Supply == baseline,
                "Lethal hull damage withdraws power before queued deletion");
            GD.Print("PASS: Industrial Generator paid construction, output, disable/recovery, private config and checkpoint seams");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Match CampaignSession's normal two-stage restore API without touching profile files or reward tickets.
    private static PowerGenerator Restore(AmmoLoopFixture f, TowerSaveData data)
    {
        PowerGenerator generator = (PowerGenerator)f.Placement.RestorePlacement(data);
        generator.RestoreCheckpoint(data, _ => 0); return generator;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
