// Shared visual/instant impact versus swept timed fronts, real purple tower purchases and 500-enemy coverage.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

public static class ShockwaveChecks
{
    private static readonly Vector2 Origin = new(2000, 2000);

    // =========================================================
    // Run against production catalog, scenes, stat pipeline, wallet, registry, emitter and visuals.
    public static async Task Run(Node main, SpawnManager spawns)
    {
        foreach (int rate in new[] { 30, 60, 120 }) Stationary(main, spawns, rate);
        Moving(main, spawns); Overlap(main, spawns); Defenses(main, spawns); Removal(main, spawns);
        Visuals(main, spawns); LargeWave(main, spawns);
        await Tower(main, spawns);
        Clear(main, spawns);
        GD.Print("PASS: shared styled shockwaves, swept relative crossings30/60/120Hz, falloff/defenses, hit-once/overlap, lifecycle, purple tower/catalog/paid previews and500 enemies");
    }

    // =========================================================
    // Exact encounter timing and falloff are independent of tick rate; untouched targets remain ahead.
    private static void Stationary(Node main, SpawnManager spawns, int rate)
    {
        Clear(main, spawns);
        Enemy center = Spawn(spawns, 0), half = Spawn(spawns, 100), edge = Spawn(spawns, 200);
        Enemy outside = Spawn(spawns, 201), shield = Spawn(spawns, 100, true);
        ShockwaveEmitter wave = Emit(main, spawns, new(40, DamageType.Energy), visuals: true);
        Require(center.Health == 60 && half.Health == 60, "Emission creates no instant full-disc damage");
        int tick = 0;
        while (!wave.IsComplete && tick++ < rate * 2)
        {
            wave._PhysicsProcess(1.0 / rate);
            Near(center.Health, 26, "Center receives its one time-zero crossing");
            Require(wave.PreviousRadius <= wave.CurrentRadius, "Radius never moves backwards");
            Near(wave.Visual.CurrentRadius, wave.CurrentRadius, "Shared visual tracks the exact simulation radius");
            if (wave.CurrentRadius < 99.999f) Near(half.Health, 60, "No damage ahead of the front");
            if (wave.CurrentRadius > 100.001f) Near(half.Health, 38.75f, "Half-range falloff hits exactly once");
        }
        Require(wave.IsComplete && Math.Abs(tick / (double)rate - .8) <= 1.1 / rate, "Reach time matches250px/s at " + rate + "Hz");
        Near(half.Health, 38.75f, "Half range retains25 base Energy before0.85x hull");
        Near(shield.CurrentShield, 50, "Half range retains25 base before2x shield");
        Near(shield.Health, 60, "Unbroken shield prevents ordinary hull damage");
        Near(edge.Health, 51.5f, "Inclusive maximum radius retains25% of40 base");
        Near(outside.Health, 60, "Outside maximum radius is untouched");
        Require(wave.AppliedHits == 4, "One emission damages each eligible center once");
        wave._PhysicsProcess(4); Near(half.Health, 38.75f, "Completed emissions never damage again");
    }

    // =========================================================
    // Full relative sweeps catch fast through-passes, while behind-front spawns and post-expiry motion do not hit.
    private static void Moving(Node main, SpawnManager spawns)
    {
        Clear(main, spawns); Enemy fast = Spawn(spawns, 150);
        ShockwaveEmitter wave = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), edge: 1);
        wave._PhysicsProcess(.2); Near(fast.Health, 60, "Unreached moving target starts untouched");
        Enemy behind = Spawn(spawns, 25);
        fast.GlobalPosition = Origin - new Vector2(150, 0);
        wave._PhysicsProcess(.2);
        Near(fast.Health, 50, "Enemy crosses both sides of ring between samples without tunnelling");
        Near(behind.Health, 60, "New enemy behind the old front is not swept as a full disc");
        wave._PhysicsProcess(.4); Near(fast.Health, 50, "Second crossing of the same ring is ignored");
        Require(!ShockwaveEmitter.Crossing(new(.00001f, 0), new(.00001f, 0), 0, 0, out _),
            "Zero-radius geometry has no hidden proximity tolerance");
        Require(ShockwaveEmitter.Crossing(new(150, 0), new(-150, 0), 50, 100, out double contact)
            && Math.Abs(contact - 2.0 / 7) < 1e-8, "Relative quadratic returns first physical encounter");
        Clear(main, spawns); Enemy late = Spawn(spawns, 500), stationary = Spawn(spawns, 100);
        wave = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), edge: 1);
        late.GlobalPosition = Origin;
        wave._PhysicsProcess(3);
        Near(stationary.Health, 50, "Long delta sweeps the whole active radius without skipping");
        Near(late.Health, 60, "Motion after wave lifetime cannot create a retroactive hit");
        Clear(main, spawns); Enemy incoming = Spawn(spawns, 150);
        wave = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1));
        wave._PhysicsProcess(.2); incoming.GlobalPosition = Origin - new Vector2(150, 0); wave._PhysicsProcess(.2);
        Near(incoming.Health, 60 - 10 * (1 - .75f * (float)(50 + 50 * 2.0 / 7) / 200),
            "Moving target falloff uses the contact radius, not its end-of-tick distance");
    }

    // =========================================================
    // Each emitted front owns its own hit-once set, even with overlapping radius/time/origin.
    private static void Overlap(Node main, SpawnManager spawns)
    {
        Clear(main, spawns); Enemy target = Spawn(spawns, 100);
        ShockwaveEmitter first = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), edge: 1);
        ShockwaveEmitter second = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), edge: 1);
        first._PhysicsProcess(.4); second._PhysicsProcess(.2); Near(target.Health, 50, "Only the first front has reached target");
        first._PhysicsProcess(.1); second._PhysicsProcess(.2); Near(target.Health, 40, "Overlapping emission independently hits once");
        first._PhysicsProcess(1); second._PhysicsProcess(1); Near(target.Health, 40, "Already passed fronts never repeat damage");
        ShockwaveEmitter third = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), edge: 1);
        third._PhysicsProcess(.4); Near(target.Health, 30, "A subsequent emission can hit the same enemy again");
    }

    // =========================================================
    // Timed delivery reuses shared shield-break units, explicit overrides, zero responses and penetration.
    private static void Defenses(Node main, SpawnManager spawns)
    {
        foreach (var sample in new[] {
            (new DamageInfo(20, DamageType.Energy), 30f, 0f, 55.75f),
            (new DamageInfo(12, DamageType.Energy, 6, .15f), 30f, 0f, 58.95f),
            (new DamageInfo(12, DamageType.Energy, 0, 2, .5f), 100f, 100f, 48f) })
        {
            Clear(main, spawns); Enemy target = Spawn(spawns, 100, true);
            target.TakeDamage(100 - sample.Item2);
            ShockwaveEmitter wave = Emit(main, spawns, sample.Item1, edge: 1);
            wave._PhysicsProcess(.39); Near(target.Health, 60, "Defense processing waits for contact");
            wave._PhysicsProcess(.01);
            Near(target.CurrentShield, sample.Item3, "Wave uses canonical shield response once");
            Near(target.Health, sample.Item4, "Wave preserves base-unit spillover/override/penetration");
        }
    }

    // =========================================================
    // Mutation during delivery, external frees, scene removal and registry unload cannot leave late hits.
    private static void Removal(Node main, SpawnManager spawns)
    {
        Clear(main, spawns); Enemy first = Spawn(spawns, 100), removed = Spawn(spawns, 100);
        ShockwaveEmitter wave = Emit(main, spawns, new(200, DamageType.Energy));
        void Killed(Enemy enemy) { if (enemy == first && GodotObject.IsInstanceValid(removed)) removed.Free(); }
        spawns.Registry.EnemyKilled += Killed;
        try { wave._PhysicsProcess(1); Require(wave.AppliedHits == 1, "Kill callbacks can free later recipients without corrupting enumeration"); }
        finally { spawns.Registry.EnemyKilled -= Killed; }
        Clear(main, spawns); Enemy survivor = Spawn(spawns, 100), departed = Spawn(spawns, 100), freed = Spawn(spawns, 100);
        wave = Emit(main, spawns, new(10, DamageType.Energy), visuals: true);
        departed.Arrive(); freed.Free();
        wave.GetParent().RemoveChild(wave); wave._PhysicsProcess(1);
        Near(survivor.Health, 60, "Unloaded emission cannot resume damage");
        Require(wave.IsComplete && wave.Visual.IsQueuedForDeletion(), "Unload cleans its independent world visual"); wave.Free();
        wave = Emit(main, spawns, new(200, DamageType.Energy));
        void Unload(Enemy enemy) { if (GodotObject.IsInstanceValid(wave) && wave.IsInsideTree()) wave.GetParent().RemoveChild(wave); }
        spawns.Registry.EnemyKilled += Unload;
        try { wave._PhysicsProcess(.5); Require(wave.IsComplete, "Reentrant emitter unload during a kill safely cancels remaining dispatch"); }
        finally { spawns.Registry.EnemyKilled -= Unload; wave.Free(); }
        Clear(main, spawns);
        EnemyRegistry temporary = new(); main.AddChild(temporary);
        wave = new(); wave.Initialize(temporary, Origin, new(1, DamageType.Energy), 200, 250, .25f);
        main.GetNode("World/Projectiles").AddChild(wave); wave.SetPhysicsProcess(false); temporary.Free(); wave._PhysicsProcess(.1);
        Require(wave.IsComplete, "Registry unloading cancels the wave safely");
        Clear(main, spawns); Enemy invalidTarget = Spawn(spawns, 100);
        wave = Emit(main, spawns, new(10, DamageType.Energy));
        DamageResponseProfile profile = DamageResolver.DefaultProfile; float hull = profile.EnergyHull;
        try
        {
            profile.EnergyHull = float.NaN; bool rejected = false;
            try { wave._PhysicsProcess(.4); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected && wave.IsComplete && invalidTarget.Health == 60,
                "Malformed defense data cancels the emission before retrying or changing target state");
        }
        finally { profile.EnergyHull = hull; }
        // Invalid initialization must reject before allocating a visual or applying damage.
        foreach (float value in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
        {
            ShockwaveEmitter invalid = new();
            try { invalid.Initialize(spawns.Registry, Origin, new(1, DamageType.Energy), 200, value, .25f); throw new InvalidOperationException("Invalid speed accepted."); }
            catch (ArgumentException) { }
            finally { invalid.Free(); }
        }
    }

    // =========================================================
    // Style clocks/radius/artwork can never change simulation; independent copies prevent shared visual edits.
    private static void Visuals(Node main, SpawnManager spawns)
    {
        Clear(main, spawns); Enemy target = Spawn(spawns, 100);
        ShockwaveEmitter wave = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), edge: 1, visuals: true);
        ShockwaveStyle source = GD.Load<ShockwaveStyle>("res://TOWERS/Offensive/ShockwaveTower/Effects/ShockwaveTowerStyle.tres");
        Require(wave.Visual.Style != source, "Effect isolates mutable styles");
        wave.Visual.Style.RingColor = Colors.Green; wave.Visual.Style.RingWidth = 20;
        wave.Visual.Style.VisualRadius = 9000; wave.Visual.Style.ExpansionDuration = .001f;
        wave.Visual.Style.Artwork = GD.Load<Texture2D>("res://TOWERS/Offensive/ShockwaveTower/Artwork/shockwave_tower.svg");
        wave.Visual._Process(100); Near(target.Health, 60, "Externally driven visual has no autonomous damage or expansion");
        Near(wave.Visual.CurrentRadius, 0, "Visual style clock cannot run ahead of the gameplay radius");
        wave._PhysicsProcess(.4); Near(target.Health, 50, "Restyled wave retains its actual speed/damage");
        Near(wave.Visual.CurrentRadius, 100, "Style radius cannot override synchronized geometry");
        Require(source.RingColor != Colors.Green && source.VisualRadius == 200, "Live effect edits do not change authored tower style");
        wave.Visual.Free(); wave._PhysicsProcess(.4); Near(target.Health, 50, "Missing visual cannot suppress or repeat damage");
        ShockwaveEffect effect = ShockwaveEffect.Spawn(main.GetNode<Node2D>("World/Effects"), Origin, source, 200);
        effect.Style.ExpansionDuration = .1f; effect._Process(.05); Near(effect.CurrentRadius, 100, "Autonomous visual uses its own presentation clock");
        effect._Process(.1); Require(effect.IsQueuedForDeletion(), "Autonomous effect self-cleans");
        ShockwaveEffect standalone = GD.Load<PackedScene>("res://FEEDBACK/Shockwave/ShockwaveEffect.tscn").Instantiate<ShockwaveEffect>();
        main.GetNode("World/Effects").AddChild(standalone); standalone.SetProcess(false);
        standalone._Process(.2); Near(standalone.CurrentRadius, 64, "Standalone shared scene uses its neutral style radius");
        standalone.Free();
        Clear(main, spawns); Enemy exact = Spawn(spawns, 0), offCenter = Spawn(spawns, .1f);
        ShockwaveEmitter point = Emit(main, spawns, new(10, DamageType.Energy, hullCoefficientOverride: 1), radius: 0);
        point._PhysicsProcess(1); Near(exact.Health, 50, "Zero-radius pulse reaches only the center"); Near(offCenter.Health, 60, "Zero-radius pulse excludes other centers");
    }

    // =========================================================
    // One lightweight emission handles500 enemies without collision areas or per-recipient scene nodes.
    private static void LargeWave(Node main, SpawnManager spawns)
    {
        Clear(main, spawns);
        for (int i = 0; i < 500; i++) Spawn(spawns, 100);
        int before = main.GetNode("World/Projectiles").GetChildCount();
        ShockwaveEmitter wave = Emit(main, spawns, new(.01f, DamageType.Energy, hullCoefficientOverride: 1), edge: 1);
        Stopwatch time = Stopwatch.StartNew(); wave._PhysicsProcess(.4); time.Stop();
        Require(wave.AppliedHits == 500 && wave.GetChildCount() == 0
            && main.GetNode("World/Projectiles").GetChildCount() == before + 1, "500 crossings use one emitter and no per-enemy nodes");
        foreach (Enemy enemy in spawns.Registry.Living) Near(enemy.Health, 59.99f, "Each of500 recipients receives one hit");
        wave._PhysicsProcess(.1); Require(wave.AppliedHits == 500, "500 already-hit enemies are skipped on later ticks");
        GD.Print($"SHOCKWAVE500: swept contact/dispatch {time.Elapsed.TotalMilliseconds:0.00}ms (headless observation)");
    }

    // =========================================================
    // Real placement/action/payment, explicit levels, previews, cached snapshots, cadence and style ownership.
    private static async Task Tower(Node main, SpawnManager spawns)
    {
        Clear(main, spawns);
        TowerPlacementController placement = main.GetNode<TowerPlacementController>("TowerPlacement");
        GameplayHUD hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy");
        WorldGrid grid = main.GetNode<WorldGrid>("World/Grid");
        TowerDefinition definition = placement.Catalog.Find("shockwave");
        Require(definition != null && hud.GetBuildButton("shockwave") != null, "Purple Shockwave registers in the real catalog/build bar"); definition.Validate();
        wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        hud.GetBuildButton("shockwave").EmitSignal(Button.SignalName.Pressed);
        Vector2I cell = grid.Level.Passable.GetUsedCells().First(c => grid.PlacementFailure(c).Length == 0 && spawns.PlacementFailure(c).Length == 0);
        placement.Call("TryPlace", cell); ConstructionTestClock.FinishInitial(main);
        Node towers = main.GetNode("World/Towers");
        ShockwaveTower tower = towers.GetChild<ShockwaveTower>(towers.GetChildCount() - 1); tower.SetPhysicsProcess(false);
        try
        {
            Require(wallet.GetBalance(wallet.CreditsResource) == 650 && grid.IsOccupied(cell), "Real350-credit placement succeeds");
            Require(tower.FinalStats.Shockwave == new ShockwaveStats(40, 250, 4, .25f) && tower.FinalStats.RangePixels == 230
                && tower.FinalStats.ShieldPenetration == 0 && tower.FinalStats.Weapon.Value.Type == DamageType.Energy,
                "Level1 preserves every proposed playtest stat");
            Require(tower.Config != definition.DefaultConfig && tower.Config.Levels[0] != definition.DefaultConfig.Levels[0]
                && tower.Config.WaveStyle != ((ShockwaveTowerConfig)definition.DefaultConfig).WaveStyle, "Placed rows/recipes/styles are isolated");
            Label details = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/TowerStats");
            Require(details.Text.Contains("Damage per wave: 40") && details.Text.Contains("Expansion speed: 250") && details.Text.Contains("Interval: 4")
                && details.Text.Contains("Edge damage: 25%") && UpgradeTableChecks.HasRow(hud, "Wave Damage", "40", "44", "(+4)") && UpgradeTableChecks.HasRow(hud, "Wave Interval (s)", "4", "3.9", "(−0.1)"), "Inspector shows actual wave stats and complete paid preview");
            placement.Call("TryPlace", cell); ConstructionTestClock.FinishInitial(main); Require(wallet.GetBalance(wallet.CreditsResource) == 650, "Occupied placement never double-charges");
            TowerFinalStats cached = tower.FinalStats, upgraded = tower.ResolveLevel(2);
            tower.Config.WaveStyle.RingWidth = 7; tower.Config.WaveStyle.ExpansionDuration = .01f;
            Require(ReferenceEquals(cached, tower.FinalStats), "Style tuning never invalidates combat stats");
            tower._PhysicsProcess(.01);
            ShockwaveEmitter emission = main.GetNode("World/Projectiles").GetChildren().OfType<ShockwaveEmitter>().Single(); emission.SetPhysicsProcess(false);
            emission._PhysicsProcess(.2); Near(emission.CurrentRadius, 50, "Real tower emission uses level1 simulation speed");
            Require(ReferenceEquals(cached, tower.FinalStats), "Emitting does not recalculate cached stats");
            int revision = grid.NavigationRevision;
            await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
            hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeButton").EmitSignal(Button.SignalName.Pressed); ConstructionTestClock.FinishWork(main);
            Require(tower.UpgradeLevel == 2 && tower.FinalStats == upgraded && wallet.GetBalance(wallet.CreditsResource) == 425
                && grid.NavigationRevision == revision && tower.NextUpgrade == null, "Paid225 level2 matches preview and detects maximum without navigation edits");
            Require(upgraded.Shockwave == new ShockwaveStats(44, 260, 3.9f, .25f) && upgraded.RangePixels == 239, "Level2 has exact independent complete stats");
            emission._PhysicsProcess(.2); Near(emission.CurrentRadius, 100, "Upgrade cannot change a launched emission's speed/range");
            Require(details.Text.Contains("Damage per wave: 44") && !hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeButton").Visible, "Inspector applies paid stats and maximum-level state");
            Require(!TowerUpgradeController.TryUpgrade(tower, wallet, placement.Settings, 2, out _)
                && wallet.GetBalance(wallet.CreditsResource) == 425, "Maximum cannot spend again");
            tower.Config.SetLevelStats(1, tower.Config.Levels[0].Stats with { RangePixels = 900, Shockwave = new(999, 999, .1f, 1) });
            Require(tower.FinalStats == upgraded, "Changing level1 cannot alter the purchased level2");
            for (int i = 0; i < 240; i++) tower._PhysicsProcess(1.0 / 60);
            ShockwaveEmitter next = main.GetNode("World/Projectiles").GetChildren().OfType<ShockwaveEmitter>().Last(); next.SetPhysicsProcess(false);
            Require(next != emission, "Tower periodically emits later independent fronts");
            next._PhysicsProcess(.2); Near(next.CurrentRadius, 52, "Next emission uses upgraded speed rather than earlier/baseline row");
            tower.Enabled = false; int count = main.GetNode("World/Projectiles").GetChildCount(); tower._PhysicsProcess(10);
            Require(main.GetNode("World/Projectiles").GetChildCount() == count, "Disabled tower emits no waves");
            foreach (int rate in new[] { 30, 60, 120 })
            {
                ShockwaveTower cadence = definition.Scene.Instantiate<ShockwaveTower>(); cadence.InitializeDefinition(definition);
                towers.AddChild(cadence); cadence.SetPhysicsProcess(false);
                int before = main.GetNode("World/Projectiles").GetChildCount();
                for (int i = 0; i < rate * 8; i++) cadence._PhysicsProcess(1.0 / rate);
                Require(main.GetNode("World/Projectiles").GetChildCount() == before + 3,
                    "Initial pulse plus4/8-second pulses retain cadence at " + rate + "Hz");
                cadence._PhysicsProcess(100);
                int afterHitch = main.GetNode("World/Projectiles").GetChildCount();
                cadence._PhysicsProcess(.01);
                Require(main.GetNode("World/Projectiles").GetChildCount() == afterHitch,
                    "Long hitch skips missed pulses without a following-frame burst");
                cadence.Free();
            }
            ShockwaveTowerConfig invalid = new(); invalid.SetLevelStats(1, invalid.Levels[0].Stats with {
                RangePixels = float.NaN, Shockwave = new(float.NaN, 0, float.PositiveInfinity, 2), ShieldPenetration = 2 });
            Require(invalid.ResolveStats(1).Shockwave == new ShockwaveStats(0, 1, .05f, 1) && invalid.ResolveStats(1).RangePixels == 0
                && invalid.ResolveStats(1).ShieldPenetration == 1, "Wave capability retains finite safety bounds in the shared pipeline");
            TowerStatTrace trace = new(); TowerStatPipeline.Resolve(tower.Config, 2, trace);
            Require(trace.Steps.Count == 8 && trace.Steps.Skip(1).All(s => s.Stats.Shockwave == upgraded.Shockwave), "All eight stages preserve the selected wave capability");
        }
        finally
        {
            placement.Call("SelectTower", default(Variant)); placement.Call("CancelPlacement"); tower.Free(); grid.ReleaseOccupancy(cell);
            var placed = (Dictionary<Vector2I, TowerBase>)typeof(TowerPlacementController).GetField("_placedTowers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(placement);
            placed.Remove(cell); Clear(main, spawns); hud.ClearInspection(); wallet.Initialize(grid.Level.Settings);
        }
    }

    // =========================================================
    // Production enemy fixture with movement disabled and optional100-point shield from the runner.
    private static Enemy Spawn(SpawnManager spawns, float distance, bool shield = false) => AreaDamageChecks.Spawn(spawns, Origin + new Vector2(distance, 0), shield);
    // =========================================================
    // One real delivery node with manual tick control, using the authored purple style when requested.
    private static ShockwaveEmitter Emit(Node main, SpawnManager spawns, DamageInfo hit, float edge = .25f, bool visuals = false, float radius = 200)
    {
        ShockwaveEmitter wave = new();
        wave.Initialize(spawns.Registry, Origin, hit, radius, 250, edge,
            visuals ? main.GetNode<Node2D>("World/Effects") : null,
            GD.Load<ShockwaveStyle>("res://TOWERS/Offensive/ShockwaveTower/Effects/ShockwaveTowerStyle.tres"));
        main.GetNode("World/Projectiles").AddChild(wave); wave.SetPhysicsProcess(false); return wave;
    }
    // =========================================================
    // Free only this suite's attack visuals/delivery; keep production world services and authored resources.
    private static void Clear(Node main, SpawnManager spawns)
    {
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) if (GodotObject.IsInstanceValid(enemy)) enemy.Arrive();
        foreach (Node child in main.GetNode("World/Projectiles").GetChildren()) child.Free();
        foreach (Node child in main.GetNode("World/Effects").GetChildren()) if (child is ShockwaveEffect) child.Free();
    }
    // =========================================================
    // Float integration accepts only small simulation rounding, never a full extra hit.
    private static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .003f, message + $" ({actual} != {expected})");
    // =========================================================
    // Fail the full headless suite on any delivery, payment, cache, ownership or lifecycle regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
