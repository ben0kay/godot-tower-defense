// E3A production carrier/wave/checkpoint regressions, with real results integration and no saved-map writes.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MissionFixture;

public partial class DeathSpawnChecks : Node
{
    // =========================================================
    // Always-running test owner can assert the real mission pause after the final offspring resolves.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await FinalCarrier(); DeathSpawnSafetyChecks.Run(this); DeathSpawnRouteChecks.Run(this);
            await DeathSpawnSchedulingChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: E3A real Transporter, staggered safe children, wave reservations/results, checkpoint handoff, caps/RNG and lifecycle"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Final carrier kill must never win before its reserved releases and actual offspring have resolved.
    private async Task FinalCarrier()
    {
        foreach (string scene in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene })
        {
            using DeathSpawnFixture f = new(this, scene);
            Enemy parent = f.Parent(true); Vector2 death = parent.GlobalPosition;
            var wallet = f.Main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
            Require(parent.Definition.MaxHealth == 500 && parent.Definition.MoveSpeed == 45 && parent.Definition.RadiusPixels == 24
                && parent.GetNode<Sprite2D>("Sprite").Texture.GetSize() == new Vector2(112, 72), "Real large slow carrier balance/art");
            parent.TakeDamage(99999); parent.TakeDamage(99999); parent.Arrive();
            Require(f.Queue.PendingChildren == 6 && f.Queue.SuccessfulChildren == 0 && f.Waves.PendingDeathSpawns == 6 && f.Waves.PlannedRemaining == 0
                && f.Waves.AliveCount == 0 && f.Waves.EnemiesRemaining == 6 && f.Mission.Session.State == LevelSessionState.Playing,
                "Pre-removal reservation prevents final-wave premature victory, with no synchronous creation");
            f.Queue.Advance(1.0 / 60, Engine.GetPhysicsFrames());
            Require(f.Queue.SuccessfulChildren == 0, "An already-active queue cannot release during the actual death physics frame");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(f.Mission.Session.State == LevelSessionState.Playing && !GetTree().Paused, "Deferred victory cannot bypass pending releases");
            f.Tick(.01); Require(f.Spawns.Registry.Living.Count == 1 && f.Waves.AliveCount == 1 && f.Waves.PendingDeathSpawns == 5, "First child on next permitted tick");
            f.Tick(.06); Require(f.Spawns.Registry.Living.Count == 1, "Interval respected");
            f.Main.GetNode<PauseMenu>("PauseMenu").Open(); f.Tick(10);
            Require(f.Spawns.Registry.Living.Count == 1, "Paused release clock cannot advance"); f.Main.GetNode<PauseMenu>("PauseMenu").Resume();
            f.Tick(.06); Require(f.Spawns.Registry.Living.Count == 2, "Unpaused interval resumes"); f.FinishReleases();
            Require(f.Waves.PendingDeathSpawns == 0 && f.Waves.AliveCount == 6 && f.Waves.PlannedRemaining == 0
                && wallet.GetBalance(wallet.CreditsResource) == credits + 15, "Parent pays once; children do not mint rewards at creation");
            Enemy[] children = f.Spawns.Registry.Living.ToArray();
            Require(children.Select(c => c.GlobalPosition).Distinct().Count() > 1 && children.All(c => c.Definition.EnemyId == "basic_mk1"
                && c.Health == 60 && c.CurrentShield == 0 && c.SpawnGeneration == 1 && c.GlobalPosition.DistanceTo(death) <= 48.001f
                && f.Spawns.Paths.Navigation.HasClearance(c.GlobalPosition, c.Definition.RadiusPixels)), "Independent valid child stats/positions/generation");
            for (int i = 0; i < 5; i++) children[i].TakeDamage(99999);
            Require(f.Mission.Session.State == LevelSessionState.Playing && f.Waves.AliveCount == 1, "Last offspring keeps mission playing");
            children[5].TakeDamage(99999);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(f.Mission.Session.Snapshot?.Outcome == LevelOutcome.Victory && f.Mission.Session.Snapshot.EnemiesKilled == 7
                && f.Waves.CompletedWaves == 1 && wallet.GetBalance(wallet.CreditsResource) == credits + 33, "Actual R1 victory includes parent+6 official offspring kills and normal rewards");
        }
        GD.Print("PASS: E3A final-carrier reservation/transfer, sequential .12s releases, pause/resume, wave-only stats/rewards and R1 victory on both launchers");
    }
}
