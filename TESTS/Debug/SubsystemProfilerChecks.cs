// PF2: deterministic aggregation/world isolation/lifecycle plus production movement, targeting and projectile hooks.
using Godot;
using System;
using System.Diagnostics;
using static AmmoLoopFixture;

public partial class SubsystemProfilerChecks : Node
{
    #region Runner
    // =========================================================
    // No source maps or profiles are changed; callback clocks are explicit and timings are diagnostic only.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            Aggregation();
            using (AmmoLoopFixture f = new(this)) Production(f);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: PF2 fixed-window mean/p95/peak/calls, nesting, world isolation, pause/hide/teardown, allocation budget and production hooks");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    #endregion

    #region Aggregation and Lifecycle
    // =========================================================
    // Sample counts are per selected physics tick, including idle zeroes; nested rows must not be summed.
    private void Aggregation()
    {
        Node world = new(), other = new(), actor = new(); AddChild(world); AddChild(other); world.AddChild(actor);
        SubsystemProfiler sampler = new(); AddChild(sampler); sampler.Initialize(world, 30);
        using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement)) { }
        Require(sampler.Samples == 0 && !sampler.Running, "Off is inert");
        sampler.Start(); sampler._PhysicsProcess(.016);
        using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement))
        {
            using (SubsystemProfiler.Measure(actor, ProfileCategory.Clearance)) { }
            using (SubsystemProfiler.Measure(actor, ProfileCategory.Clearance)) { }
        }
        using (SubsystemProfiler.Measure(other, ProfileCategory.Movement)) { }
        Require(sampler.Samples == 0, "Partial physics tick is not displayed as complete"); sampler.EndTick();
        Require(sampler.Read(ProfileCategory.Movement).Calls == 1 && sampler.Read(ProfileCategory.Clearance).Calls == 2, "Nested scopes count once and foreign world is excluded");
        var read = sampler.Read(ProfileCategory.Movement);
        Require(read.Mean >= 0 && read.P95 == read.Peak && read.Peak >= sampler.Read(ProfileCategory.Clearance).Peak, "Inclusive parent covers child timing");
        for (int i = 0; i < 31; i++) { sampler._PhysicsProcess(.016); sampler.EndTick(); }
        Require(sampler.Samples == 30 && sampler.Read(ProfileCategory.Movement).Calls == 0 && sampler.Read(ProfileCategory.Movement).Mean == 0, "Bounded ring evicts old spikes and includes idle ticks");
        sampler.SampleEveryTicks = 3; sampler.Start();
        for (int i = 0; i < 9; i++) { sampler._PhysicsProcess(.016); sampler.EndTick(); }
        Require(sampler.Samples == 3, "Stride retains selected ticks only");
        GetTree().Paused = true; sampler._PhysicsProcess(.016); sampler.EndTick();
        Require(sampler.Samples == 3, "Pause adds no stale physics samples"); GetTree().Paused = false;
        sampler.Stop(); int count = sampler.Samples;
        for (int i = 0; i < 100; i++) using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement)) { }
        long bytes = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
        for (int i = 0; i < 10000; i++) using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement)) { }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Require(allocated == 0 && sampler.Samples == count, "Disabled hooks allocate no managed storage");
        double off = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        sampler.SampleEveryTicks = 1; sampler.Start(); sampler._PhysicsProcess(.016);
        using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement)) { } // Warm owner binding before measuring.
        bytes = GC.GetAllocatedBytesForCurrentThread(); started = Stopwatch.GetTimestamp();
        for (int i = 0; i < 10000; i++) using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement)) { }
        allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        double on = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency; sampler.EndTick();
        Require(allocated == 0, "Warmed scopes allocate no managed objects per call");
        GD.Print($"PF2 synthetic 10,000 empty scopes: OFF={off:0.000}ms ON={on:0.000}ms allocated={allocated}; not full-game observer overhead");
        Require(sampler.Report("test build/workload").Contains("DO NOT SUM"), "Report declares overlapping timings");
        sampler.Free(); using (SubsystemProfiler.Measure(actor, ProfileCategory.Movement)) { }
        world.Free(); other.Free();
    }
    #endregion

    #region Production Hooks
    // =========================================================
    // Tick actual services under the active world's scope; the test never calls Record or invents timings.
    private void Production(AmmoLoopFixture f)
    {
        SubsystemBattlefield.DetachSession(f.Main);
        SubsystemProfiler sampler = new(); AddChild(sampler); sampler.Initialize(f.Main.GetNode("World"));
        Enemy enemy = f.Target(new(2000, 2000)); sampler.Start(); sampler._PhysicsProcess(.016);
        enemy.SetMovementPosition(new(2010, 2000));
        TowerTargeting.Find(f.Spawns.Registry, enemy.GlobalPosition, 100, TowerTargeting.Priority.Closest);
        f.Spawns.Paths.Navigation.SegmentClear(new(2000, 2000), new(2010, 2000), enemy.Definition.RadiusPixels);
        BasicProjectile shot = new(); shot.Initialize(enemy, 100, new DamageInfo(1, DamageType.Energy));
        f.Main.GetNode("World/Projectiles").AddChild(shot); Disable(shot); shot.LaunchFrom(new(1900, 2000), Vector2.Right);
        shot._PhysicsProcess(.016); sampler.EndTick();
        Require(sampler.Read(ProfileCategory.Registry).Calls > 0 && sampler.Read(ProfileCategory.Targeting).Calls == 1
            && sampler.Read(ProfileCategory.Clearance).Calls == 1 && sampler.Read(ProfileCategory.Projectiles).Calls == 1, "Production hooks feed actual categories");
        sampler.Free(); f.ClearProjectiles(); enemy.Free();
    }
    #endregion
}
