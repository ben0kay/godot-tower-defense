// Shared work clocks, timed paid upgrades, Inspector and Battle-only construction art; no construction VFX.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using static AmmoLoopFixture;

public partial class ConstructionChecks : Node
{
    // =========================================================
    // Always-running runner can inspect the deliberately paused production world without advancing it.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await Timers(); ResultsPause(); ConstructionArtChecks.Run(this); await TimedUpgradeChecks.Run(this); ConstructionPlacementChecks.Run(this); ConstructionPerformanceChecks.Run(this);
            using (AmmoLoopFixture f = new(this))
            {
                await TowerUpgradeChecks.Run(f.Main);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: C1-C3 work clocks, paid construction/timed upgrades, rank/ammo/Inspector and weapon/production/logistics gates"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Private passive fixtures isolate work from economy/combat, while pause and automatic ticks use a real SceneTree.
    private async Task Timers()
    {
        Node scope = new(); AddChild(scope);
        ConstructionWorkManager manager = new(); scope.AddChild(manager);
        try
        {
            PassiveTestTower direct = Site(scope, manager, 0);
            Require(direct.IsOperational && !direct.IsBusy && manager.ActiveCount == 0 && !manager.IsPhysicsProcessing(), "Zero duration has no inactive frame/job");
            foreach (double invalid in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                bool rejected = false; try { TowerConfig.ValidateWorkSeconds(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Require(rejected, "Invalid duration rejected");
            }
            TowerLevel row = new(1, 1, default, 2); TowerLevel copy = row.CreateInstanceCopy(); copy.WorkSeconds = 9;
            Require(row.WorkSeconds == 2 && copy.WorkSeconds == 9, "WorkSeconds copies independently with absolute rows");
            BattleTowerConfig authored = new(); BattleTowerConfig privateCopy = authored.CreateInstanceCopy();
            privateCopy.Levels[0].WorkSeconds = 9; privateCopy.ConstructionSeconds = 999;
            Require(authored.BuildSeconds == 3 && privateCopy.BuildSeconds == 9 && privateCopy.Levels[1].WorkSeconds == 4,
                "Explicit level durations ignore passive fallback and remain independent");
            authored.Dispose(); privateCopy.Dispose();
            TowerConfig passive = new() { ConstructionSeconds = 7 };
            Require(passive.Levels.Count == 0 && passive.BuildSeconds == 7, "Passive duration needs no combat rows");
            passive.Dispose();
            foreach (int hz in new[] { 30, 60, 120 })
            {
                PassiveTestTower tower = Site(scope, manager, 2); int completed = 0, progress = 0;
                tower.WorkCompleted += () => completed++; tower.WorkProgressChanged += () => progress++;
                for (int i = 0; i < hz * 2 - 1; i++) manager.Advance(1.0 / hz);
                Require(tower.IsBusy && !tower.IsOperational && tower.RemainingWorkSeconds > 0, "No early completion");
                manager.Advance(1.0 / hz); manager.Advance(10);
                Require(tower.IsOperational && tower.WorkProgress == 1 && tower.ElapsedWorkSeconds == 2 && completed == 1
                    && progress <= 20 && manager.ActiveCount == 0, "Exact boundary/idempotence and bounded progress notifications30/60/120Hz"); tower.Free();
            }
            PassiveTestTower paused = Site(scope, manager, .2);
            GetTree().Paused = true; manager.Advance(99);
            await ToSignal(GetTree().CreateTimer(.03, true), SceneTreeTimer.SignalName.Timeout);
            Require(paused.ElapsedWorkSeconds == 0 && paused.IsBusy, "Manual and automatic clocks honor SceneTree pause");
            GetTree().Paused = false;
            await ToSignal(GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
            Require(paused.IsOperational && manager.ActiveCount == 0 && !manager.IsPhysicsProcessing(), "Automatic clock resumes and sleeps when empty");
            PassiveTestTower first = Site(scope, manager, 1), second = Site(scope, manager, 1), third = Site(scope, manager, 1), spawned = null;
            first.WorkCompleted += () => { second.Free(); spawned = Site(scope, manager, 1); manager.Advance(100); };
            manager.Advance(1);
            Require(first.IsOperational && third.IsOperational && spawned.IsBusy && spawned.ElapsedWorkSeconds == 0
                && manager.ActiveCount == 1, "Completion callback removal/new job/reentrant Advance cannot skip or pre-advance work");
            spawned.QueueFree(); manager.Advance(1);
            Require(manager.ActiveCount == 0 && !manager.IsPhysicsProcessing(), "Queued-free job retires without activation");
            PassiveTestTower unload = Site(scope, manager, 10); manager.Free();
            Require(unload.IsBusy && !unload.IsPhysicsProcessing(), "Manager unload never activates unfinished sites");
        }
        finally { GetTree().Paused = false; scope.Free(); }
        GD.Print("PASS: C1 source copies, passive durations, exact30/60/120Hz, pause/automatic processing, throttled events and callback-safe cancellation");
    }

    // =========================================================
    // Actual mission defeat freezes an existing job, not just an isolated manually paused timer.
    private void ResultsPause()
    {
        using MissionFixture f = new(this);
        ConstructionWorkManager manager = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        PassiveTestTower tower = Site(f.Main.GetNode("World/Towers"), manager, 10);
        manager.Advance(1); f.Health.TakeDamage(f.Health.CurrentHealth); manager.Advance(99);
        Require(GetTree().Paused && f.Session.State == LevelSessionState.Defeat && tower.ElapsedWorkSeconds == 1 && tower.IsBusy,
            "Mission Results pause freezes construction immediately");
        GetTree().Paused = false;
    }

    // =========================================================
    // Direct helpers are created operational unless paid placement explicitly prepares a construction job.
    internal static PassiveTestTower Site(Node scope, ConstructionWorkManager manager, double seconds)
    {
        PassiveTestTower tower = new() { Config = new TowerConfig { ConstructionSeconds = seconds } };
        tower.StartConstruction(manager); scope.AddChild(tower); return tower;
    }
}
