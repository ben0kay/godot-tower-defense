// Earned rank/XP: actual damage attribution, finite enemy budgets, production deliveries and existing Inspector.
using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

public static class TowerRankChecks
{
    private static readonly Vector2 Origin = new(2000, 2000);

    // =========================================================
    // Exercise production state/transactions with private fixtures; no disk/profile writes or authored edits.
    public static async Task Run(Node main, SpawnManager spawns)
    {
        try
        {
            Thresholds(main); Damage(main, spawns); Shields(main, spawns); Deliveries(main, spawns);
            await Inspector(main, spawns); Scale(main, spawns);
            GD.Print("PASS: tower XP/ranks, bounded actual damage/shields/overkill, four weapon sources, named promotions, level independence, cached Stage03 and live Inspector/MAX RANK");
        }
        finally
        {
            var placement = main.GetNode<TowerPlacementController>("TowerPlacement");
            placement.Call("SelectTower", default(Variant)); placement.Call("CancelPlacement");
            Clear(main, spawns);
            main.GetNode<EconomyManager>("Economy").Initialize(main.GetNode<WorldGrid>("World/Grid").Level.Settings);
            main.GetNode<GameplayHUD>("UI/GameplayHUD").ClearInspection();
        }
    }

    // =========================================================
    // Exact named totals/increment doubling, explicit bonuses, multiple promotions and fractional summation.
    private static void Thresholds(Node main)
    {
        double[] totals = { 0, 1000, 3000, 7000, 15000 };
        foreach (var rows in new[] { BattleTowerRanks.Rows, LaserTowerRanks.Rows, BlasterCannonRanks.Rows, ShockwaveTowerRanks.Rows })
            for (int i = 0; i < 5; i++)
            {
                Require(rows[i].Rank == (TowerRank)i && rows[i].TotalXp == totals[i], "Five exact names and total XP thresholds");
                if (i >= 2) Near(rows[i].TotalXp - rows[i - 1].TotalXp, 2 * (rows[i - 1].TotalXp - rows[i - 2].TotalXp), "Promotion XP doubles rather than total XP doubling");
                Near(rows[i].DamageBonus, .02 * i, "Rank damage bonus is current-row total");
            }
        TowerBase fractional = Create(main, "battle");
        try
        {
            TowerFinalStats cached = fractional.FinalStats; int stats = 0, xpEvents = 0;
            fractional.StatsChanged += () => stats++; fractional.ExperienceChanged += () => xpEvents++;
            for (int i = 0; i < 9999; i++) TowerRankController.AwardExperience(fractional, .1);
            Require(fractional.CurrentRank.Rank == TowerRank.Standard && ReferenceEquals(cached, fractional.FinalStats), "Subthreshold fractional awards never rebuild stats");
            TowerRankController.AwardExperience(fractional, .1);
            Near(fractional.RankProgress.TotalXp, 1000, "Fractional summation reaches exact threshold");
            Require(fractional.CurrentRank.Rank == TowerRank.Veteran && stats == 1 && xpEvents == 10000, "One promotion rebuild with event-driven XP updates");
            TowerRankController.AwardExperience(fractional, 14000);
            Require(fractional.CurrentRank.Rank == TowerRank.Legendary && fractional.NextRank == null && stats == 2, "One award traverses all remaining ranks with one rebuild");
            Require(fractional.UpgradeLevel == 1 && fractional.FinalStats.DamagePerShot == 32, "Level1/Legendary applies only8% total with integer rounding");
            Near(fractional.FinalStats.RangePixels, 256 * 1.04, "Legendary total range bonus is4%, not cumulative10%");
            TowerRankController.AwardExperience(fractional, 500);
            Require(stats == 2 && fractional.RankProgress.TotalXp == 2000, "Maximum rank retains XP without repeated stat rebuilding");
            foreach (double bad in new[] { -1, 0, double.NaN, double.PositiveInfinity }) TowerRankController.AwardExperience(fractional, bad);
            Near(fractional.RankProgress.TotalXp, 15500, "Malformed/nonpositive XP never changes progress");
            TowerStatTrace trace = new();
            var final = TowerStatPipeline.Resolve(fractional.AuthoredConfig, 3, trace, fractional.CurrentRank);
            Require(trace.Steps.Count == 8 && trace.Steps[1].Stats.DamagePerShot == 36 && trace.Steps[2].Stats.DamagePerShot == 39
                && final.DamagePerShot == 39, "Stage02 selects36, Stage03 alone applies Legendary8%, later stages preserve result");
            Require(fractional.AuthoredConfig.Levels[2].Stats.DamagePerShot == 36 && fractional.AuthoredConfig.Levels[2].Costs[0].Amount == 250,
                "Rank progression never mutates explicit levels or price recipes");
            var custom = new TowerRankDefinition(TowerRank.Veteran, 50, .5f, .25f, 5);
            var laser = new LaserTowerConfig();
            Near(TowerStatPipeline.Resolve(laser, 1, rank: custom).Beam.Value.DamagePerSecond, 18, "Independent type/rank bonuses preserve float beam capability");
            var blaster = new BlasterCannonConfig(); var splash = TowerStatPipeline.Resolve(blaster, 1, rank: custom);
            Require(splash.DamagePerShot == 90 && splash.Area == blaster.ResolveStats(1).Area && splash.ShieldPenetration == 0,
                "Rank damage scales splash payload; blast radius/falloff and penetration remain separate");
            var shock = TowerStatPipeline.Resolve(new ShockwaveTowerConfig(), 1, rank: custom);
            Near(shock.Shockwave.Value.DamagePerWave, 60, "Rank scales float timed-wave damage");
            Near(shock.RangePixels, 250, "Rank range scales wave maximum radius");
            Require(shock.Shockwave.Value.IntervalSeconds == 4 && shock.Shockwave.Value.ExpansionSpeedPixelsPerSecond == 250, "Rank does not invent cooldown/speed/HP changes");
            var alternative = TowerRankController.Define(new(TowerRank.Standard, 0, 0, 0, 0), new(TowerRank.Veteran, 50, .1f, .1f, 0),
                new(TowerRank.Enhanced, 150, .2f, .2f, 0), new(TowerRank.Superior, 350, .3f, .3f, 0), new(TowerRank.Legendary, 750, .4f, .4f, 0));
            var progress = new TowerRankProgress(); progress.Add(150, alternative);
            Require(progress.Current(alternative).Rank == TowerRank.Enhanced, "Type-specific thresholds/bonuses need no progression redesign");
        }
        finally { fractional.Free(); }
    }

    // =========================================================
    // Two attackers split a six-XP pool by actual hull loss; only the lethal hit owns the30-XP kill.
    private static void Damage(Node main, SpawnManager spawns)
    {
        Clear(main, spawns); TowerBase first = Create(main, "battle"), killer = Create(main, "laser");
        var wallet = main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
        try
        {
            Enemy target = Enemy(main, spawns);
            TowerFinalStats cached = first.FinalStats;
            target.ApplyDamage(new(12, DamageType.Kinetic, source: first.DamageSource));
            Near(first.RankProgress.TotalXp, 1.2, "12 actual of60 hull distributes1.2 damage XP");
            Require(ReferenceEquals(cached, first.FinalStats), "Damage XP alone reads cached combat stats");
            target.ApplyDamage(new(10000, DamageType.Kinetic, source: killer.DamageSource));
            Near(killer.RankProgress.TotalXp, 34.8, "Overkill earns remaining4.8 damage XP plus30 lethal XP");
            target.ApplyDamage(new(10000, DamageType.Energy, source: first.DamageSource)); target.TakeDamage(10000); target.Arrive();
            Near(first.RankProgress.TotalXp, 1.2, "Dead/repeated/manual attacks never steal or duplicate kill credit");
            Require(wallet.GetBalance(wallet.CreditsResource) == credits + 3, "Existing Credits kill reward remains exactly3");
            Enemy debug = Enemy(main, spawns); debug.TakeDamage(12); debug.TakeDamage(10000);
            Enemy arrive = Enemy(main, spawns); arrive.Arrive();
            Near(killer.RankProgress.TotalXp, 34.8, "Debug kills and Citadel arrivals grant no tower XP");
            Enemy partial = Enemy(main, spawns); partial.TakeDamage(12); partial.ApplyDamage(new(10000, DamageType.Kinetic, source: first.DamageSource));
            Near(first.RankProgress.TotalXp, 36, "Unattributed partial damage also consumes finite damage budget");
            Enemy zero = Enemy(main, spawns, credits: 0); zero.ApplyDamage(new(10000, DamageType.Kinetic, source: first.DamageSource));
            Near(first.RankProgress.TotalXp, 36, "Zero-credit enemy grants zero damage and kill XP");
            Enemy queued = Enemy(main, spawns); queued.QueueFree(); queued.ApplyDamage(new(10000, DamageType.Kinetic, source: first.DamageSource));
            Near(first.RankProgress.TotalXp, 36, "Queued invalid targets award nothing");
            Enemy unregistered = Enemy(main, spawns); spawns.Registry.Unregister(unregistered);
            unregistered.ApplyDamage(new(10000, DamageType.Kinetic, source: first.DamageSource));
            Near(first.RankProgress.TotalXp, 36, "Unregistered invalid targets cannot award XP");
            Near(unregistered.Health, 60, "Invalid registry membership rejects typed damage"); unregistered.Free();
            TowerBase promoted = Create(main, "battle");
            try
            {
                Enemy valuable = Enemy(main, spawns, credits: 1500);
                valuable.ApplyDamage(new(10000, DamageType.Kinetic, source: promoted.DamageSource));
                Require(promoted.CurrentRank.Rank == TowerRank.Legendary && promoted.RankProgress.TotalXp == 18000,
                    "A single actual lethal award can promote through all five named ranks");
            }
            finally { promoted.Free(); }
            Enemy untouched = Enemy(main, spawns); untouched.ApplyDamage(new(0, DamageType.Kinetic, source: first.DamageSource));
            Near(first.RankProgress.TotalXp, 36, "Zero attempted damage grants nothing");
        }
        finally { first.Free(); killer.Free(); Clear(main, spawns); }
    }

    // =========================================================
    // Shields count actual points, while repeated restoration and changing multipliers cannot refill an enemy pool.
    private static void Shields(Node main, SpawnManager spawns)
    {
        var rules = new TowerMasterConfig(); TowerBase tower = Create(main, "battle", rules);
        try
        {
            Enemy shielded = Enemy(main, spawns, shield: true);
            shielded.ApplyDamage(new(40, DamageType.Kinetic, 1, 1, source: tower.DamageSource));
            Near(tower.RankProgress.TotalXp, 1.5, "40 actual shield of160 starting durability earns1.5 XP");
            shielded.RestoreShield(40); rules.DamageXpMultiplier = 999; rules.KillXpMultiplier = 999;
            for (int i = 0; i < 50; i++) { shielded.ApplyDamage(new(20, DamageType.Kinetic, 1, 1, source: tower.DamageSource)); shielded.RestoreShield(20); }
            Near(tower.RankProgress.TotalXp, 6, "Restoration never creates XP beyond the snapshotted six-point pool");
            shielded.ApplyDamage(new(10000, DamageType.Kinetic, shieldPenetration: 1, source: tower.DamageSource));
            Near(tower.RankProgress.TotalXp, 36, "Penetrating kill retains one30-XP reward after shield pool exhaustion");
            rules.DamageXpMultiplier = 2; rules.KillXpMultiplier = 10;
            Enemy immune = Enemy(main, spawns, shield: true);
            immune.ApplyDamage(new(100, DamageType.Kinetic, 0, 1, source: tower.DamageSource));
            Near(tower.RankProgress.TotalXp, 36, "Blocked zero-actual-damage hits award no XP");
            immune.ApplyDamage(new(12, DamageType.Kinetic, 0, 2, .5f, tower.DamageSource));
            Near(tower.RankProgress.TotalXp, 36.45, "12 actual penetrating hull loss uses the same initial160-point denominator");
            Clear(main, spawns);
            rules.DamageXpMultiplier = 3; rules.KillXpMultiplier = 4;
            Enemy configured = Enemy(main, spawns, credits: 5);
            configured.ApplyDamage(new(12, DamageType.Kinetic, source: tower.DamageSource));
            Near(tower.RankProgress.TotalXp, 39.45, "Inspector multipliers are used by a newly encountered enemy");
            configured.ApplyDamage(new(10000, DamageType.Kinetic, source: tower.DamageSource));
            Near(tower.RankProgress.TotalXp, 71.45, "Custom reward/multipliers derive XP without per-enemy XP fields");
        }
        finally { tower.Free(); Clear(main, spawns); }
    }

    // =========================================================
    // Actual production attacks carry the same weak owner through guided travel, beams, splash and wave falloff.
    private static void Deliveries(Node main, SpawnManager spawns)
    {
        Clear(main, spawns);
        BattleTower battle = (BattleTower)Create(main, "battle"); Enemy target = Enemy(main, spawns);
        battle.Config.TargetPriority = TowerTargeting.Priority.Closest; battle.Config.AimToleranceDegrees = 180;
        battle._PhysicsProcess(1);
        BasicProjectile bullet = main.GetNode("World/Projectiles").GetChildren().OfType<BasicProjectile>().Single();
        Near(battle.RankProgress.TotalXp, 0, "Launching a guided projectile is not damage XP");
        bullet._PhysicsProcess(.2); Near(battle.RankProgress.TotalXp, 3, "Production guided contact credits its Battle owner"); bullet.Free(); battle.Free(); Clear(main, spawns);
        LaserTower laser = (LaserTower)Create(main, "laser"); target = Enemy(main, spawns);
        laser.Config.TargetPriority = TowerTargeting.Priority.Closest; laser.Config.AimToleranceDegrees = 180;
        laser._PhysicsProcess(1); Near(laser.RankProgress.TotalXp, .18, "Continuous Laser credits actual1.8 hull, not12 raw base");
        laser.Config.DamagePerSecond = 10000; laser._PhysicsProcess(1);
        Near(laser.RankProgress.TotalXp, 36, "Production beam lethal tick receives remaining damage pool plus kill XP"); laser.Free(); Clear(main, spawns);
        BlasterCannon blaster = (BlasterCannon)Create(main, "blaster_cannon");
        blaster.Config.TargetPriority = TowerTargeting.Priority.Closest; blaster.Config.AimToleranceDegrees = 180;
        target = Enemy(main, spawns); Enemy half = Enemy(main, spawns, position: Origin + new Vector2(40, 0));
        blaster._PhysicsProcess(1); var splash = main.GetNode("World/Projectiles").GetChildren().OfType<SplashProjectile>().Single();
        Near(blaster.RankProgress.TotalXp, 0, "Splash travel grants no speculative XP");
        splash._PhysicsProcess(.2); Near(blaster.RankProgress.TotalXp, 40.6875, "Splash credits center kill36 plus each secondary's actual damage pool share");
        Require(splash.AppliedHits == 2 && half.IsAlive, "Splash attribution does not add duplicate primary damage"); splash.Free(); blaster.Free(); Clear(main, spawns);
        ShockwaveTower shock = (ShockwaveTower)Create(main, "shockwave"); target = Enemy(main, spawns, position: Origin + new Vector2(100, 64));
        shock._PhysicsProcess(.01); var wave = main.GetNode("World/Projectiles").GetChildren().OfType<ShockwaveEmitter>().Single(); wave.SetPhysicsProcess(false);
        wave._PhysicsProcess(.39); Near(shock.RankProgress.TotalXp, 0, "Unreached wavefront grants no XP");
        wave._PhysicsProcess(.01); Near(shock.RankProgress.TotalXp, 2.125, "Travelling wave preserves its source through encounter falloff");
        wave._PhysicsProcess(.1); Near(shock.RankProgress.TotalXp, 2.125, "One emission cannot award damage XP twice"); shock.Free(); Clear(main, spawns);
        battle = (BattleTower)Create(main, "battle"); target = Enemy(main, spawns);
        battle.Config.TargetPriority = TowerTargeting.Priority.Closest; battle.Config.AimToleranceDegrees = 180; battle._PhysicsProcess(1);
        bullet = main.GetNode("World/Projectiles").GetChildren().OfType<BasicProjectile>().Single();
        battle.Free(); bullet._PhysicsProcess(.2);
        Near(target.Health, 30, "Deleting owner does not cancel a fired projectile's combat");
        Near(battle.RankProgress.TotalXp, 0, "Weak source awards nothing to a freed tower"); Clear(main, spawns);
    }

    // =========================================================
    // Paid levels retain XP/rank; selected HUD displays event-driven progress/bonuses without losing upgrade details.
    private static async Task Inspector(Node main, SpawnManager spawns)
    {
        TowerBase tower = Create(main, "battle"), other = Create(main, "laser");
        var placement = main.GetNode<TowerPlacementController>("TowerPlacement"); var hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        var wallet = main.GetNode<EconomyManager>("Economy"); wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        try
        {
            TowerRankController.AwardExperience(tower, 1000); TowerFinalStats next = tower.ResolveLevel(2);
            Require(TowerUpgradeController.TryUpgrade(tower, wallet, placement.Settings, 1, out _) && tower.FinalStats == next,
                "Paid next-level preview includes current Veteran bonuses");
            Require(tower.UpgradeLevel == 2 && tower.RankProgress.TotalXp == 1000 && tower.CurrentRank.Rank == TowerRank.Veteran,
                "Purchased level never resets rank or accumulated XP");
            Near(tower.FinalStats.RangePixels, 264 * 1.01, "Rank applies after full purchased row selection");
            TowerRankController.AwardExperience(tower, 2000);
            Require(TowerUpgradeController.TryUpgrade(tower, wallet, placement.Settings, 2, out _) && tower.FinalStats.DamagePerShot == 37,
                "Level3 Enhanced rounds36×1.04 once");
            Require(wallet.GetBalance(wallet.CreditsResource) == 600 && tower.RankProgress.TotalXp == 3000,
                "Existing150/250 credit purchases preserve all3000 earned XP");
            placement.Call("SelectTower", tower);
            var section = hud.GetNode<Control>("BottomArea/Inspector/Insets/Body/RankSection");
            var label = section.GetNode<Label>("Rank"); var text = section.GetNode<Label>("ExperienceText");
            var bar = section.GetNode<ProgressBar>("ExperienceBar"); var bonuses = section.GetNode<Label>("RankBonuses");
            Require(section.Visible && label.Text == "Rank: Enhanced" && text.Text.Contains("0 / 4000 XP to Superior")
                && bonuses.Text.Contains("Damage +4%") && bonuses.Text.Contains("Range +2%"), "Placed bottom-right Inspector shows named rank, segment XP and active bonuses");
            TowerFinalStats cached = tower.FinalStats; string upgrade = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/UpgradeInfo").Text;
            TowerRankController.AwardExperience(tower, 1200);
            await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
            await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(ReferenceEquals(cached, tower.FinalStats) && bar.Value == 1200 && bar.MaxValue == 4000
                && text.Text.Contains("1200 / 4000 XP to Superior") && text.Text.Contains("Total XP: 4200"), "XP events update actual progress without pipeline polling/rebuilds");
            Require(hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/UpgradeInfo").Text == upgrade, "XP updates preserve the existing upgrade panel");
            TowerRankController.AwardExperience(tower, 10800);
            Require(label.Text == "Rank: Legendary" && text.Text.Contains("MAX RANK") && bar.Value == bar.MaxValue,
                "Multiple promotions update MAX RANK display and full progress bar automatically");
            Require(tower.UpgradeLevel == 3 && tower.FinalStats.DamagePerShot == 39 && tower.RankProgress.TotalXp == 15000,
                "Level and earned rank stay independent at maximum");
            await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
            await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
            var panel = hud.GetNode<Control>("BottomArea/Inspector");
            Require(panel.GetGlobalRect().Position.Y >= 0 && panel.GetGlobalRect().End.Y <= hud.Size.Y + 1,
                "Rank section preserves onscreen Inspector layout");
            placement.Call("SelectTower", other);
            Require(label.Text == "Rank: Standard" && text.Text.Contains("0 / 1000 XP to Veteran"), "Selecting another tower shows its independent XP");
            TowerRankController.AwardExperience(tower, 50);
            Require(label.Text == "Rank: Standard", "Previously selected tower unsubscribes from Inspector updates");
            hud.ClearInspection(); TowerRankController.AwardExperience(other, 1.5);
            placement.Call("SelectTower", other);
            Require(hud.IsInspectionVisible && text.Text.Contains("1.5 / 1000 XP to Veteran"),
                "Reopening the same closed selection displays XP earned while hidden");
            hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
            Require(!section.Visible, "Build preview never shows stale placed-tower rank data");
        }
        finally { placement.Call("SelectTower", default(Variant)); tower.Free(); other.Free(); }
    }

    // =========================================================
    //500 recipients award one owner from actual damage without rank polling, tree searches or per-target nodes.
    private static void Scale(Node main, SpawnManager spawns)
    {
        Clear(main, spawns); TowerBase tower = Create(main, "battle");
        try
        {
            for (int i = 0; i < 500; i++) Enemy(main, spawns);
            TowerFinalStats cached = tower.FinalStats; int nodes = main.GetNode("World/Projectiles").GetChildCount();
            Stopwatch timer = Stopwatch.StartNew();
            int count = AreaDamage.ApplyInstant(Origin, 80, new(.01f, DamageType.Kinetic, hullCoefficientOverride: 1, source: tower.DamageSource), 1, spawns.Registry);
            timer.Stop();
            double actual = spawns.Registry.Living.Sum(e => 60 - (double)e.Health);
            Near(tower.RankProgress.TotalXp, actual / 60 * 6, "500-enemy actual damage distributes fractional pool shares accurately");
            Require(count == 500 && ReferenceEquals(cached, tower.FinalStats) && main.GetNode("World/Projectiles").GetChildCount() == nodes,
                "500 attributed hits need no per-enemy nodes or repeated stat resolution");
            GD.Print($"RANK XP500: attributed area contact {timer.Elapsed.TotalMilliseconds:0.00}ms (headless observation)");
        }
        finally { tower.Free(); Clear(main, spawns); }
    }

    // =========================================================
    // Private real tower scene at a deterministic up-facing firing point; custom rules use the placement API.
    private static TowerBase Create(Node main, string id, TowerMasterConfig rules = null)
    {
        var definition = main.GetNode<TowerPlacementController>("TowerPlacement").Catalog.Find(id);
        var tower = definition.Scene.Instantiate<TowerBase>(); tower.InitializeDefinition(definition, rules);
        main.GetNode("World/Towers").AddChild(tower); tower.GlobalPosition = Origin + new Vector2(0, 64); tower.SetPhysicsProcess(false); return tower;
    }
    // =========================================================
    // Isolated definition/initialization allows zero/custom credit rewards without mutating shared enemy assets.
    private static Enemy Enemy(Node main, SpawnManager spawns, int credits = 3, bool shield = false, Vector2? position = null)
    {
        var enemy = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        enemy.Definition = (EnemyDefinition)enemy.Definition.Duplicate(true); enemy.Definition.KillRewardCredits = credits;
        enemy.Definition.MaxHealth = 60; enemy.Definition.MaxShield = 100;
        main.GetNode("World/Enemies").AddChild(enemy); enemy.GlobalPosition = position ?? Origin;
        enemy.Initialize(spawns.Registry, spawns.Paths, shield); enemy.Movement.SetPhysicsProcess(false); return enemy;
    }
    // =========================================================
    // Remove fixture actors/delivery without granting synthetic kills or changing authored defaults.
    private static void Clear(Node main, SpawnManager spawns)
    {
        AreaDamageChecks.Clear(spawns);
        foreach (Node node in main.GetNode("World/Projectiles").GetChildren()) node.Free();
        foreach (Node node in main.GetNode("World/Effects").GetChildren()) if (node is ShockwaveEffect) node.Free();
    }
    // =========================================================
    // Compare fractional XP/float stat conversions without hiding an extra reward or hit.
    private static void Near(double actual, double expected, string message) => Require(double.IsFinite(actual) && Math.Abs(actual - expected) < .0001, message + $" ({actual} != {expected})");
    // =========================================================
    // Abort the headless suite on attribution, progression, cache, payment or Inspector regressions.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
