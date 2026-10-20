// Real catalog construction/upgrades, immutable launches and instantaneous splash lifecycle regressions.
using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using static AreaDamageChecks;

public static class BlasterCannonChecks
{
    // =========================================================
    // Real build/Inspector/payment integration plus deterministic30/60/120Hz production weapon impacts.
    public static async System.Threading.Tasks.Task Run(Node main, SpawnManager spawns)
    {
        await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
        Clear(spawns);
        TowerPlacementController placement = main.GetNode<TowerPlacementController>("TowerPlacement");
        GameplayHUD hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy");
        WorldGrid grid = main.GetNode<WorldGrid>("World/Grid");
        Node towers = main.GetNode("World/Towers"), projectiles = main.GetNode("World/Projectiles");
        Node2D effects = main.GetNode<Node2D>("World/Effects");
        TowerDefinition definition = placement.Catalog.Find("blaster_cannon");
        Require(definition != null && hud.GetBuildButton("blaster_cannon") != null, "Blaster is registered in the real catalog/build bar");
        definition.Validate();
        wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        hud.GetBuildButton("blaster_cannon").EmitSignal(Button.SignalName.Pressed);
        Vector2I cell = grid.Level.Passable.GetUsedCells().First(c => grid.PlacementFailure(c).Length == 0 && spawns.PlacementFailure(c).Length == 0);
        placement.Call("TryPlace", cell); ConstructionTestClock.FinishInitial(main);
        BlasterCannon tower = towers.GetChild<BlasterCannon>(towers.GetChildCount() - 1); tower.SetPhysicsProcess(false);
        placement.Call("SelectTower", tower); // Inspect via a separate action after placement.
        Require(tower.TowerTypeId == "blaster_cannon" && grid.IsOccupied(cell) && wallet.GetBalance(wallet.CreditsResource) == 700,
            "Catalog action places a1x1 Blaster and pays300");
        Require(tower.Config != definition.DefaultConfig && tower.Levels[0] != definition.DefaultConfig.Levels[0], "Blaster config and nested tiers are private");
        Require(tower.FinalStats.Area == new AreaBlastStats(80, 0.25f) && tower.FinalStats.DamagePerShot == 60
            && tower.FinalStats.ShieldPenetration == 0 && tower.Config.DamageType == DamageType.Explosive, "Authored baseline is60/80px/25% with zero penetration");
        placement.Call("TryPlace", cell); ConstructionTestClock.FinishInitial(main); Require(wallet.GetBalance(wallet.CreditsResource) == 700, "Repeated occupancy does not charge Blaster again");
        Label details = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/TowerStats");
        
        Require(details.Text.Contains("Base damage: 60") && details.Text.Contains("Blast radius: 80") && details.Text.Contains("Edge damage: 25%")
            && UpgradeTableChecks.HasRow(hud, "Damage", "60", "66", "(+6)") && UpgradeTableChecks.HasRow(hud, "Blast Radius (px)", "80", "88", "(+8)"), "Placed Inspector shows real AoE stats and next-tier preview");
        TowerFinalStats preview = tower.ResolveLevel(2); int revision = grid.NavigationRevision;
        hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeButton").EmitSignal(Button.SignalName.Pressed); ConstructionTestClock.FinishWork(main);
        Require(tower.UpgradeLevel == 2 && tower.FinalStats == preview && tower.FinalStats.DamagePerShot == 66
            && tower.FinalStats.Area == new AreaBlastStats(88, 0.25f) && wallet.GetBalance(wallet.CreditsResource) == 500
            && grid.NavigationRevision == revision, "Paid200 tier matches preview and never changes falloff or occupancy");
        TowerFinalStats cached = tower.FinalStats;
        tower.Config.SetLevelStats(2, tower.Config.Levels[1].Stats with { Area = new(88, 0.75f) });
        Require(!ReferenceEquals(cached, tower.FinalStats) && details.Text.Contains("Edge damage: 75%"), "Falloff config edit rebuilds selected cache/Inspector immediately");
        Require(tower.FinalStats.DamagePerShot == cached.DamagePerShot && tower.FinalStats.Area.Value.RadiusPixels == cached.Area.Value.RadiusPixels
            && ((BlasterCannonConfig)definition.DefaultConfig).EdgeDamageFraction == 0.25f, "Falloff tuning leaves center/radius/authored source unchanged");
        Near(AreaDamage.AtDistance(tower.Config.CreateHit(tower.FinalStats), 88, 88, 0.75f).Amount, 49.5f, "Only weapon edge input changes the outer base amount");
        Require(DamageResolver.DefaultProfile.Coefficients(DamageType.Explosive) == (0.25f, 1.25f)
            && ((BattleTowerConfig)placement.Catalog.Find("battle").DefaultConfig).ResolveStats(1).ShieldPenetration == 0.05f
            && ((LaserTowerConfig)placement.Catalog.Find("laser").DefaultConfig).DamagePerSecond == 12, "Blaster tuning cannot change shared defaults, Battle or Laser");
        TowerFinalStats edited = tower.FinalStats; tower._PhysicsProcess(0.01);
        Require(ReferenceEquals(edited, tower.FinalStats), "Blaster physics consumes cached stats without per-tick resolution");
        placement.Call("SelectTower", default(Variant)); placement.Call("CancelPlacement");
        tower.Free(); grid.ReleaseOccupancy(cell);
        var placed = (Dictionary<Vector2I, TowerBase>)typeof(TowerPlacementController).GetField("_placedTowers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(placement);
        placed.Remove(cell);
        Stats((BlasterCannonConfig)definition.DefaultConfig);
        foreach (int rate in new[] { 30, 60, 120 }) Impact(main, spawns, definition, rate);
        foreach (string exit in new[] { "death", "free", "arrival" }) await LostTarget(main, spawns, exit);
        PayloadOverrides(main, spawns, definition);
        FalloffTuning(main, spawns, definition);
        CleanupDelivery(main, spawns);
        RangeAfterLaunch(main, spawns, definition);
        Clear(spawns);
        foreach (Node projectile in projectiles.GetChildren()) projectile.Free();
        foreach (Node effect in effects.GetChildren()) if (effect is ShockwaveEffect) effect.Free();
        hud.ClearInspection(); wallet.Initialize(grid.Level.Settings);
        GD.Print("PASS: Blaster catalog/300-credit placement,200-credit tier, AoE Inspector/cache, launch snapshots,30/60/120Hz instant-only impact and lost-target/unload/timeout cleanup");
    }

    // =========================================================
    // Optional area capability uses all eight stages and ordinary typed modifier operations/clamps.
    private static void Stats(BlasterCannonConfig authored)
    {
        TowerStatTrace trace = new();
        TowerStatPipeline.Resolve(new TowerStatContext(new TowerStatBlock(256, 0.5f, 60, 500, 180, 12, 0.15f,
            Area: new AreaBlastStats(80, 0.25f))), trace);
        Require(trace.Steps.Count == 8 && trace.Steps.All(s => s.Stats.Area == new AreaBlastStats(80, 0.25f)), "All eight neutral stages preserve area capability");
        BlasterCannonConfig modifier = authored.CreateInstanceCopy();
        TowerFinalStats second = modifier.ResolveStats(2);
        modifier.BlastRadiusPixels = 500; modifier.EdgeDamageFraction = 0.9f;
        Require(modifier.ResolveStats(2) == second, "Blaster level2 is independent of level1 area values");
        modifier.SetLevelStats(2, modifier.Levels[1].Stats with { Area = new(60, 0.21f) });
        Near(modifier.ResolveStats(2).Area.Value.RadiusPixels, 60, "Area radius selects the absolute row value");
        Near(modifier.ResolveStats(2).Area.Value.EdgeDamageFraction, 0.21f, "Edge retention selects the absolute row value");
        foreach (float input in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            BlasterCannonConfig invalid = authored.CreateInstanceCopy(); invalid.BlastRadiusPixels = input; invalid.EdgeDamageFraction = input;
            Require(invalid.ResolveStats(1).Area == new AreaBlastStats(0, 0), "Area safety clamps negative/nonfinite inputs");
        }
        modifier.EdgeDamageFraction = 4;
        Require(modifier.ResolveStats(1).Area.Value.EdgeDamageFraction == 1 && authored.ResolveStats(1).Area.Value.EdgeDamageFraction == 0.25f,
            "Edge safety clamps to1 without source mutation");
    }

    // =========================================================
    // A real fired projectile travels, then hits primary/half/edge once with launch-time area and damage.
    private static void Impact(Node main, SpawnManager spawns, TowerDefinition definition, int rate)
    {
        Clear(spawns); ClearProjectiles(main);
        Vector2 center = new(2000, 2000);
        Enemy primary = Spawn(spawns, center), half = Spawn(spawns, center + new Vector2(40, 0));
        Enemy edge = Spawn(spawns, center + new Vector2(80, 0)), outside = Spawn(spawns, center + new Vector2(81, 0));
        BlasterCannon tower = Create(main, definition, center + new Vector2(0, 128)); tower.Config.ProjectileSpeed = 128;
        tower._PhysicsProcess(0.2);
        SplashProjectile shot = main.GetNode("World/Projectiles").GetChild<SplashProjectile>(0);
        float travelSeconds = shot.GlobalPosition.DistanceTo(center) / 128f;
        Require(shot.GlobalPosition.IsEqualApprox(tower.GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA").GlobalPosition), "Blaster launches at real barrel tip");
        Require(!shot.HasDetonated && primary.Health == 60 && half.Health == 60, "Launching never applies immediate direct or area damage");
        tower.Config.ImpactStyle.RingColor = Colors.Green;
        tower.Config.DamagePerShot = 120; tower.Config.BlastRadiusPixels = 200; tower.Config.EdgeDamageFraction = 1;
        tower.Config.OverrideHullCoefficient = true; tower.Config.HullCoefficient = 4; tower.Config.ShieldPenetration = 1;
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
        int ticks = 0;
        while (!shot.HasDetonated && ticks < rate * 2)
        {
            shot._PhysicsProcess(1.0 / rate); ticks++;
            if (ticks < rate / 2) Require(primary.Health == 60 && half.Health == 60, "Travel does not scan/damage recipients");
        }
        Require(shot.HasDetonated && Math.Abs(ticks / (double)rate - travelSeconds) <= 1.1 / rate && shot.AppliedHits == 3,
            "Production splash reaches its point once at consistent time across physics rates");
        Require(!primary.IsAlive && wallet.GetBalance(wallet.CreditsResource) == credits + 3, "Primary kill/reward occurs once without direct+splash doubling");
        Near(half.Health, 13.125f, "Fired shot preserves launch damage, falloff and default Explosive hull coefficient");
        Near(edge.Health, 41.25f, "Fired shot preserves launch radius/edge fraction"); Near(outside.Health, 60, "Outside launch radius remains untouched");
        shot._PhysicsProcess(1); Require(wallet.GetBalance(wallet.CreditsResource) == credits + 3, "Repeated ticks cannot detonate/pay twice");
        foreach (Node effect in main.GetNode("World/Effects").GetChildren())
            if (effect is ShockwaveEffect flash)
            {
                Require(flash.Style.RingColor != Colors.Green && flash.Style.ExpansionDuration == .22f,
                    "Blaster impact uses its orange launch-style snapshot, not subsequent source edits");
                flash.Style.ExpansionDuration = 10; flash.Style.VisualRadius = 1000; flash._Process(.5);
                Near(half.Health, 13.125f, "Longer/larger cosmetic ring never reapplies or delays instant damage");
                flash.Free();
            }
        Near(half.Health, 13.125f, "Visual animation/expiry never deals extra damage");
        tower.Free(); Clear(spawns); shot.Free();
    }

    // =========================================================
    // Both normal lifecycle and external frees retain the latest target point, never an arbitrary replacement.
    private static async System.Threading.Tasks.Task LostTarget(Node main, SpawnManager spawns, string exit)
    {
        Clear(spawns); ClearProjectiles(main);
        Vector2 original = new(2000, 2000), latest = original + new Vector2(32, 0);
        Enemy target = Spawn(spawns, original), survivor = Spawn(spawns, latest);
        SplashProjectile shot = new();
        shot.Initialize(target, spawns.Registry, original, 128, new(20, DamageType.Explosive), new(80, 0.25f), main.GetNode<Node2D>("World/Effects"), trackTarget: true);
        main.GetNode("World/Projectiles").AddChild(shot); shot.SetPhysicsProcess(false); shot.GlobalPosition = original + new Vector2(0, 128);
        shot._PhysicsProcess(0.25); target.GlobalPosition = latest;
        if (exit == "death") target.TakeDamage(1000);
        else if (exit == "arrival") target.Arrive();
        else target.Free();
        await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
        shot._PhysicsProcess(2);
        Require(shot.HasDetonated && shot.AppliedHits == 1, "Lost target still produces one instantaneous blast: " + exit);
        Near(shot.GlobalPosition.X, latest.X, "Target removal preserves final impactX"); Near(survivor.Health, 35, "Last-known point blast applies once to nearby survivors");
        shot._PhysicsProcess(1); Near(survivor.Health, 35, "Lost-target blast never repeats");
        shot.Free(); Clear(spawns);
    }

    // =========================================================
    // An opted-in weapon snapshots explicit zero shield response, hull override and penetration together.
    private static void PayloadOverrides(Node main, SpawnManager spawns, TowerDefinition definition)
    {
        Clear(spawns); ClearProjectiles(main); Vector2 center = new(2000, 2000);
        Enemy primary = Spawn(spawns, center, true), half = Spawn(spawns, center + new Vector2(40, 0), true);
        BlasterCannon tower = Create(main, definition, center + new Vector2(0, 128));
        tower.Config.DamageType = DamageType.Kinetic; tower.Config.DamagePerShot = 12;
        tower.Config.OverrideShieldCoefficient = true; tower.Config.ShieldCoefficient = 0;
        tower.Config.OverrideHullCoefficient = true; tower.Config.HullCoefficient = 2;
        tower.Config.ShieldPenetration = 0.5f; tower._PhysicsProcess(0.2);
        SplashProjectile shot = main.GetNode("World/Projectiles").GetChild<SplashProjectile>(0);
        tower.Config.OverrideShieldCoefficient = false; tower.Config.OverrideHullCoefficient = false;
        tower.Config.ShieldPenetration = 0; tower.Config.DamageType = DamageType.Energy;
        shot._PhysicsProcess(1);
        Require(shot.AppliedHits == 2, "Opted-in splash dispatches one complete hit per shielded enemy");
        Near(primary.CurrentShield, 100, "Explicit zero shield override survives the launch snapshot");
        Near(primary.Health, 48, "Launch-time hull override and50% penetration bypass without bonus damage");
        Near(half.CurrentShield, 100, "Ordinary half-radius portion remains blocked");
        Near(half.Health, 52.5f, "Falloff scales base units before penetration and hull override");
        shot.Free(); tower.Free(); Clear(spawns);
    }

    // =========================================================
    // Changing only the weapon's falloff changes its next real impact, never center damage or defaults.
    private static void FalloffTuning(Node main, SpawnManager spawns, TowerDefinition definition)
    {
        foreach (float fraction in new[] { 0.25f, 0.75f })
        {
            ClearProjectiles(main); Vector2 center = new(2000, 2000);
            Enemy primary = Spawn(spawns, center), edge = Spawn(spawns, center + new Vector2(80, 0));
            BlasterCannon tower = Create(main, definition, center + new Vector2(0, 128));
            tower.Config.DamagePerShot = 20; tower.Config.EdgeDamageFraction = fraction; tower._PhysicsProcess(0.2);
            SplashProjectile shot = main.GetNode("World/Projectiles").GetChild<SplashProjectile>(0); shot._PhysicsProcess(1);
            Near(primary.Health, 35, "Center damage stays constant across weapon falloff edits");
            Near(edge.Health, 60 - 20 * fraction * 1.25f, "New Blaster projectile uses its edited edge retention");
            Require(((BlasterCannonConfig)definition.DefaultConfig).EdgeDamageFraction == 0.25f
                && DamageResolver.DefaultProfile.Coefficients(DamageType.Explosive) == (0.25f, 1.25f),
                "Instance falloff never changes authored defaults or shared Explosive responses");
            shot.Free(); tower.Free(); Clear(spawns);
        }
    }

    // =========================================================
    // Removing delivery or reaching its lifetime cannot cause a delayed explosion.
    private static void CleanupDelivery(Node main, SpawnManager spawns)
    {
        Vector2 center = new(2000, 2000); Enemy target = Spawn(spawns, center);
        Node parent = main.GetNode("World/Projectiles"), effects = main.GetNode("World/Effects"); int visuals = effects.GetChildCount();
        SplashProjectile removed = new(); removed.Initialize(target, spawns.Registry, center, 100, new(60, DamageType.Explosive), new(80, 0.25f));
        parent.AddChild(removed); removed.SetPhysicsProcess(false); parent.RemoveChild(removed); removed._PhysicsProcess(10);
        Require(!removed.HasDetonated && target.Health == 60 && effects.GetChildCount() == visuals, "Unload cancels subscriptions/damage/visuals"); removed.Free();
        SplashProjectile expired = new(); expired.Initialize(target, spawns.Registry, center, 1, new(60, DamageType.Explosive), new(80, 0.25f), lifetime: 0.05f);
        parent.AddChild(expired); expired.SetPhysicsProcess(false); expired.GlobalPosition = center + new Vector2(0, 128); expired._PhysicsProcess(0.1);
        Require(!expired.HasDetonated && expired.IsQueuedForDeletion() && target.Health == 60, "Timeout expires without damage or impact visuals"); expired.Free();
        DamageResponseProfile profile = DamageResolver.DefaultProfile; float hull = profile.ExplosiveHull;
        SplashProjectile invalid = new(); invalid.Initialize(target, spawns.Registry, center, 500,
            new(60, DamageType.Explosive), new(80, 0.25f));
        parent.AddChild(invalid); invalid.SetPhysicsProcess(false); invalid.GlobalPosition = center;
        try
        {
            profile.ExplosiveHull = float.NaN; bool rejected = false;
            try { invalid._PhysicsProcess(0.01); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected && target.Health == 60 && invalid.IsQueuedForDeletion(),
                "Invalid defense data rejects before mutation and still releases impact delivery");
        }
        finally { profile.ExplosiveHull = hull; invalid.Free(); Clear(spawns); }
    }

    // =========================================================
    // Firing range is checked at launch only; traveling projectiles track beyond it and still explode.
    private static void RangeAfterLaunch(Node main, SpawnManager spawns, TowerDefinition definition)
    {
        ClearProjectiles(main); Vector2 center = new(2000, 2000); Enemy target = Spawn(spawns, center);
        BlasterCannon tower = Create(main, definition, center + new Vector2(0, 128)); tower._PhysicsProcess(0.2);
        SplashProjectile shot = main.GetNode("World/Projectiles").GetChild<SplashProjectile>(0);
        target.GlobalPosition += new Vector2(400, 0); shot._PhysicsProcess(1);
        Require(shot.HasDetonated && !target.IsAlive, "Impact still hits after target leaves tower firing range");
        shot.Free(); tower.Free(); Clear(spawns);
    }

    // =========================================================
    // Create a real registered scene with private config and a deterministic closest/aligned fixture.
    private static BlasterCannon Create(Node main, TowerDefinition definition, Vector2 position)
    {
        BlasterCannon tower = definition.Scene.Instantiate<BlasterCannon>(); tower.InitializeDefinition(definition);
        tower.Config.TargetPriority = TowerTargeting.Priority.Closest; tower.Config.AimToleranceDegrees = 180;
        // Exact falloff/lifecycle fixtures isolate blast behavior from separately tested launch dispersion.
        for (int i = 1; i <= tower.Config.Levels.Count; i++)
            tower.Config.SetLevelStats(i, tower.Config.Levels[i - 1].Stats with { Handling = new(0) });
        main.GetNode("World/Towers").AddChild(tower); tower.SetPhysicsProcess(false); tower.GlobalPosition = position; return tower;
    }

    // =========================================================
    // Clear queued fixtures explicitly without changing BasicProjectile or production lifetime behavior.
    private static void ClearProjectiles(Node main) { foreach (Node child in main.GetNode("World/Projectiles").GetChildren()) child.Free(); }
}
