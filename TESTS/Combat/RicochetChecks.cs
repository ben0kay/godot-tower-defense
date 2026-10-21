// SR1-SR4: pure affine reflection, actual shield contact, bounded cosmetic motion and allocation stress.
using Godot;
using System;
using System.Diagnostics;
using static AmmoLoopFixture;
public partial class RicochetChecks : Node
{
    #region Runner
    // =========================================================
    // Run real projectiles against production registry/shield services; rendering cost requires desktop profiling.
    public override async void _Ready()
    {
        try
        {
            Geometry();
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                var fx = f.Main.GetNode<RicochetController>("World/Effects/Ricochets");
                fx.Style = new() { ChancePercent = 100, AngleJitterDegrees = 0, MaxActiveRicochets = 128 };
                fx.Master = new();
                Integration(f, fx); Stress(f, fx);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: ricochet geometry, shield bridge, once-only damage, policy, caps, pause, expiry, disabled and allocation stress");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion
    #region Geometry
    // =========================================================
    // Rotated and sheared/nonuniform transforms use the inverse transpose for the outward world normal.
    private static void Geometry()
    {
        foreach (float angle in new[] { 0f, .7f, 2f })
        {
            Transform2D ellipse = new Transform2D(Vector2.Right.Rotated(angle) * 30, Vector2.Down.Rotated(angle) * 15, new(120, 90));
            Vector2 d = ellipse.X.Normalized(), c = ellipse.Origin;
            Require(RicochetGeometry.Reflect(ellipse, c - d * 100, c, d, out var p, out var n, out var r), "Head-on intersection");
            Require(p.DistanceTo(c - d * 30) < .001f && n.Dot(-d) > .999f && r.Dot(-d) > .999f, "Head-on returns toward gun");
            Vector2 from = ellipse * new Vector2(-3, .9f), contact = ellipse * new Vector2(0, .9f);
            Require(RicochetGeometry.Reflect(ellipse, from, contact, d, out p, out n, out r) && r.Dot(n) > 0 && r.Dot(d) > 0, "Glancing retains forward tangent");
            Require(Mathf.Abs((ellipse.AffineInverse() * p).Length() - 1) < .001f, "Boundary lies on actual ellipse");
            Require(!RicochetGeometry.Reflect(ellipse, c, c + d * 5, d, out _, out _, out _), "Inside origin suppressed");
        }
        var shear = new Transform2D(new(30, 4), new(10, 15), new(80, 90));
        Vector2 local = new(-.8f, .6f), boundary = shear * local;
        Vector2 tangent = shear.BasisXform(new(-local.Y, local.X)).Normalized();
        Require(RicochetGeometry.Reflect(shear, boundary - Vector2.Right * 100, boundary + Vector2.Right, Vector2.Right,
            out _, out var normal, out _) && Mathf.Abs(normal.Dot(tangent)) < .001f, "Sheared inverse-transpose normal");
        Require(!RicochetGeometry.Reflect(new Transform2D(Vector2.Zero, Vector2.Zero, Vector2.Zero), Vector2.Zero, Vector2.One, Vector2.Right, out _, out _, out _), "Degenerate suppressed");
        Require(!RicochetGeometry.Reflect(Transform2D.Identity, new(-2, 1), new(2, 1), Vector2.Right, out _, out _, out _), "Exact tangent suppressed");
    }
    #endregion
    #region Gameplay Bridge
    // =========================================================
    // Capture actual shield ellipse then tick a funded-shot equivalent once; cosmetic rounds cannot affect recipients.
    private static void Integration(AmmoLoopFixture f, RicochetController fx)
    {
        Enemy enemy = f.Target(new(2000, 2000)); enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths, true); Disable(enemy);
        var shield = enemy.GetNode<ShieldVisual>("ShieldVisual");
        enemy.Rotation = .5f; enemy.Scale = new(1.4f, .8f);
        Require(shield.TryGetRim(out var rim), "Live shield geometry available");
        var aura = shield.GetNode<ColorRect>("Aura");
        Vector2 expected = aura.GetGlobalTransform() * (aura.Size * .5f + Vector2.Right * aura.Size.X * .4375f);
        Require((rim * Vector2.Right).DistanceTo(expected) < .001f, "Actual shader/pivot geometry matches");
        enemy.Rotation = 0; enemy.Scale = Vector2.One;
        var visual = new ProjectileVisualStyle();
        BasicProjectile Shot(DamageInfo hit, bool eligible = true)
        {
            BasicProjectile bullet = new(); bullet.Initialize(enemy, 1000, hit, visual: visual, shieldRicochet: eligible);
            f.Main.GetNode("World/Projectiles").AddChild(bullet); Disable(bullet);
            bullet.LaunchFrom(enemy.GlobalPosition - Vector2.Right * 100, Vector2.Right); bullet._PhysicsProcess(.15); return bullet;
        }
        float initial = enemy.CurrentShield;
        var shot = Shot(new(10, DamageType.Kinetic, shieldCoefficientOverride: 1));
        Require(shot.IsQueuedForDeletion() && enemy.CurrentShield == initial - 10 && fx.ActiveCount == 1, "One real hit and one fake round");
        shot._PhysicsProcess(1); Require(enemy.CurrentShield == initial - 10 && fx.ActiveCount == 1, "Repeated tick cannot hit twice");
        fx._PhysicsProcess(.2); Require(enemy.CurrentShield == initial - 10, "Cosmetic motion cannot damage"); fx.Clear();
        Shot(new(10, DamageType.Kinetic, shieldPenetration: 1)); Require(fx.ActiveCount == 0, "Full bypass has no shield absorption");
        Shot(new(10, DamageType.Kinetic, shieldCoefficientOverride: 0)); Require(fx.ActiveCount == 0, "Zero response has no visual");
        Shot(new(1, DamageType.Kinetic), false); Require(fx.ActiveCount == 0, "Non-opted weapon excluded");
        Shot(new(1, DamageType.Energy)); Require(fx.ActiveCount == 0, "Nonkinetic excluded");
        fx.Master.ShieldRicochetsEnabled = false; float before = enemy.CurrentShield;
        Shot(new(1, DamageType.Kinetic, shieldCoefficientOverride: 1)); Require(fx.ActiveCount == 0 && enemy.CurrentShield == before - 1, "Off preserves damage");
        fx.Master.ShieldRicochetsEnabled = true;
        Shot(new(100000, DamageType.Kinetic)); Require(!enemy.IsAlive && fx.ActiveCount == 1, "Lethal callback safely emits captured geometry");
        fx._PhysicsProcess(2); Require(fx.ActiveCount == 0 && !fx.IsPhysicsProcessing(), "Death-independent expiry sleeps");
        foreach (string id in new[] { "machine_gun", "sniper", "cryo", "stasis" })
            Require(((ProjectileTurretConfig)f.Placement.Catalog.Find(id).DefaultConfig).ShieldRicochet == (id == "machine_gun" || id == "sniper"), "Typed opt-in " + id);
        Require(((BattleTowerConfig)f.Placement.Catalog.Find("battle").DefaultConfig).ShieldRicochet, "Battle opt-in");
    }
    #endregion
    #region Stress and Lifecycle
    // =========================================================
    // Hundreds of independent enemies do not change effect complexity; only the fixed active buffer is visited.
    private static void Stress(AmmoLoopFixture f, RicochetController fx)
    {
        var visual = new ProjectileVisualStyle().Capture();
        int children = fx.GetChildCount();
        for (int i = 0; i < 300; i++) f.Target(new(1000 + i * 10, 3000));
        for (int i = 0; i < 1000; i++) fx.Spawn(Vector2.Zero, Vector2.Left, Vector2.Left, 1000, visual);
        Require(fx.ActiveCount == 128 && fx.SkippedCapacity == 872 && fx.GetChildCount() == children, "Fixed cap no per-effect nodes");
        GetPaused(fx, true); fx._PhysicsProcess(2); Require(fx.ActiveCount == 128, "Pause freezes expiry"); GetPaused(fx, false);
        fx.Master.ShieldRicochetsEnabled = false; fx._PhysicsProcess(.01); Require(fx.ActiveCount == 0, "Global off clears active work");
        fx.Master.ShieldRicochetsEnabled = true;
        for (int i = 0; i < 20; i++) { fx.Spawn(Vector2.Zero, Vector2.Left, Vector2.Left, 1000, visual); fx._PhysicsProcess(2); }
        long startBytes = GC.GetAllocatedBytesForCurrentThread(); var clock = Stopwatch.StartNew();
        for (int tick = 0; tick < 600; tick++)
        { for (int i = 0; i < 16; i++) fx.Spawn(Vector2.Zero, Vector2.Left, Vector2.Left, 1000, visual); fx._PhysicsProcess(1d / 60); }
        clock.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        GD.Print($"RICOCHET 9600 requests/600 ticks/300 enemies: {clock.Elapsed.TotalMilliseconds:0.###}ms total; managedBytes={allocated}; active={fx.ActiveCount}");
        Require(allocated < 4096, "No per-round managed allocation");
        fx._PhysicsProcess(2); Require(fx.ActiveCount == 0, "Hard expiry");
        fx.Master.ShieldRicochetsEnabled = false; clock.Restart();
        for (int tick = 0; tick < 600; tick++) for (int i = 0; i < 16; i++) fx.Spawn(Vector2.Zero, Vector2.Left, Vector2.Left, 1000, visual);
        clock.Stop(); GD.Print($"RICOCHET OFF 9600 requests: {clock.Elapsed.TotalMilliseconds:0.###}ms total; active={fx.ActiveCount}");
        fx.Master.ShieldRicochetsEnabled = true;
        fx.Spawn(Vector2.Zero, Vector2.Left, Vector2.Left, 1000, visual); fx._ExitTree(); Require(fx.ActiveCount == 0, "Unload clears buffer");
    }
    // =========================================================
    // Explicit paused ticks test guards even when the fixture disables automatic processing.
    private static void GetPaused(Node node, bool paused) => node.GetTree().Paused = paused;
    #endregion
}
