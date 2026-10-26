// C4 shared stage-art regressions for Battle, Machine Gun, Blaster Cannon and Rocket Tower.
using Godot;
using static AmmoLoopFixture;

internal static class ConstructionArtChecks
{
    private const string Folder = "res://TOWERS/Offensive/BattleTower/Artwork/Construction/";
    private static Texture2D Texture(string name) => GD.Load<Texture2D>(Folder + name + ".svg");
    private static Texture2D Original => GD.Load<Texture2D>("res://TOWERS/Offensive/BattleTower/Artwork/battle_tower.svg");

    // =========================================================
    // Exercise real paid placement and optional per-level slots without modifying authored balance/art resources.
    internal static void Run(Node runner)
    {
        Build(runner); MachineGunBuild(runner); BlasterAndRocketBuild(runner); DataAndFallback(runner); Upgrade(runner); GeometryAndCleanup(runner);
        GD.Print("PASS: Battle/Machine Gun/Blaster/Rocket build SVG0/35/70/final, split gun return, selection, no repeat writes/polling, optional upgrade/final art, fallback/isolation, pause/ghost and rectangular geometry");
    }

    // =========================================================
    // Existing three-second work is the only clock; art changes do not change wallet, stats, XP or navigation.
    private static void Build(Node runner)
    {
        using AmmoLoopFixture f = new(runner);
        BattleTower tower = f.Purchase<BattleTower>("battle", false);
        Sprite2D sprite = tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath); var transform = sprite.Transform; Color tint = sprite.Modulate;
        var visual = tower.GetNode<TowerConstructionVisuals>("ConstructionVisuals");
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var sites = f.Main.GetNode<ConstructionSiteHologram>("World/Effects/ConstructionSiteHolograms");
        Require(sites.HasSite(tower) && sites.ActiveSites == 1 && sites.IsProcessing(),
            "CV1 appears on paid foundation immediately before work, not on the placement ghost");
        var stats = tower.FinalStats; int revision = f.Spawns.Paths.Navigation.Grid.NavigationRevision;
        Require(sprite.Texture == Texture("stage0_foundation") && visual.TextureChanges == 1 && !tower.IsOperational, "Actual paid Battle shows foundation at Ready");
        for (int i = 0; i < 30; i++) clock.Advance(.03);
        Require(visual.TextureChanges == 1, "Progress between thresholds does not rewrite textures");
        clock.Advance(.15);
        Require(sprite.Texture == Texture("stage1_frame") && visual.TextureChanges == 2, "35% stage");
        runner.GetTree().Paused = true; clock.Advance(100); runner.GetTree().Paused = false;
        Require(sprite.Texture == Texture("stage1_frame") && tower.ElapsedWorkSeconds < 1.06, "Pause freezes both work and stage");
        clock.Advance(1.05);
        Require(sprite.Texture == Texture("stage2_assembly") && visual.TextureChanges == 3, "70% assembly stage");
        clock.Advance(.9); clock.Advance(100);
        Require(sprite.Texture == Original && visual.TextureChanges == 4 && tower.IsOperational && !visual.IsProcessing() && !visual.IsPhysicsProcessing(), "Completion restores unchanged final art once, with no presenter processing");
        Require(!sites.HasSite(tower) && sites.ActiveSites == 0 && !sites.IsProcessing(),
            "CV1 releases the completed site and sleeps without independent visuals");
        Require(sprite.Transform == transform && sprite.Modulate == tint && tower.FinalStats == stats && tower.RankProgress.TotalXp == 0
            && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 800 && f.Spawns.Paths.Navigation.Grid.NavigationRevision == revision, "Art never changes placement, selection, stats, XP or payment");
        BattleTower direct = f.Add<BattleTower>("battle", new(2200, 2000));
        Require(!sites.HasSite(direct) && !sites.IsProcessing(), "Direct operational instances have no CV1 site");
        Require(direct.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath).Texture == Original && direct.GetNode<TowerConstructionVisuals>("ConstructionVisuals").TextureChanges == 0
            && direct.Config.ConstructionArt == tower.Config.ConstructionArt && direct.Config.Levels[0] != tower.Config.Levels[0], "Loaded art resources shared, mutable numeric rows private, direct instances remain finished");
        f.Hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
        Require(f.Placement.GetNode<Sprite2D>("PlacementGhost").Texture == Original, "Ghost keeps finished icon, not construction art");
    }

    // =========================================================
    // Machine Gun reuses the same presenter; initial staging must hide both split finished layers.
    private static void MachineGunBuild(Node runner)
    {
        const string folder = "res://TOWERS/Offensive/MachineGunTower/Artwork/Construction/";
        using AmmoLoopFixture f = new(runner);
        MachineGunTower tower = f.Purchase<MachineGunTower>("machine_gun", false);
        var sprite = tower.GetNode<Sprite2D>("TurretPivot/ConstructionSprite");
        var baseSprite = tower.GetNode<Sprite2D>("BaseSprite");
        var gun = tower.GetNode<Node2D>("TurretPivot/RecoilRoot");
        var presenter = tower.GetNode<TowerConstructionVisuals>("ConstructionVisuals");
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        Require(sprite.Visible && !baseSprite.Visible && !gun.Visible &&
            sprite.Texture == GD.Load<Texture2D>(folder + "stage0_foundation.svg") && !tower.IsOperational,
            "Machine Gun paid site begins at foundation and hides both finished layers");
        tower.SetSelected(true);
        Require(sprite.Modulate == new Color(1f, .9f, .68f), "Selecting unfinished Machine Gun also tints construction art");
        tower.SetSelected(false);
        clock.Advance(1.5);
        Require(sprite.Texture == GD.Load<Texture2D>(folder + "stage1_frame.svg"), "Machine Gun 35% frame art");
        clock.Advance(1.4);
        Require(sprite.Texture == GD.Load<Texture2D>(folder + "stage2_assembly.svg"), "Machine Gun 70% twin-barrel assembly art");
        clock.Advance(1.1);
        Require(tower.IsOperational && !sprite.Visible && baseSprite.Visible && gun.Visible &&
            sprite.Texture == GD.Load<Texture2D>("res://TOWERS/Offensive/MachineGunTower/Artwork/machine_gun_tower.svg") &&
            presenter.TextureChanges == 4 && !presenter.IsProcessing(),
            "Four seconds returns Machine Gun to original independent split gun/base without idle presenter");
        var direct = f.Add<MachineGunTower>("machine_gun", new(2200, 2064));
        Require(direct.IsOperational && !direct.GetNode<Sprite2D>("TurretPivot/ConstructionSprite").Visible &&
            direct.GetNode<Sprite2D>("BaseSprite").Visible,
            "Direct Machine Gun instances remain finished without a construction transition");
    }

    // =========================================================
    // Paid splash-tower initial builds use the common 5-second staged presenter and original split sprites.
    private static void BlasterAndRocketBuild(Node runner)
    {
        foreach (var (id, dir, filename) in new[] {
            ("blaster_cannon", "BlasterCannon", "blaster_cannon"),
            ("rocket", "RocketTower", "rocket_tower") })
        using (AmmoLoopFixture f = new(runner))
        {
            TowerBase tower = f.Purchase<TowerBase>(id, false);
            string prefix = "res://TOWERS/Offensive/" + dir + "/Artwork/";
            var sprite = tower.GetNode<Sprite2D>("TurretPivot/ConstructionSprite");
            var basis = tower.GetNode<Sprite2D>("BaseSprite");
            var gun = tower.GetNode<Node2D>("TurretPivot/RecoilRoot");
            var presenter = tower.GetNode<TowerConstructionVisuals>("ConstructionVisuals");
            var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
            Require(sprite.Visible && !basis.Visible && !gun.Visible && !tower.IsOperational &&
                sprite.Texture == GD.Load<Texture2D>(prefix + "Construction/stage0_foundation.svg"),
                id + " starts on foundation with finished base/gun hidden");
            tower.SetSelected(true);
            Require(sprite.Modulate == new Color(1f, .9f, .68f), id + " construction selection tint");
            tower.SetSelected(false);
            clock.Advance(1.8);
            Require(sprite.Texture == GD.Load<Texture2D>(prefix + "Construction/stage1_frame.svg"),
                id + " switches to 35% frame");
            clock.Advance(1.8);
            Require(sprite.Texture == GD.Load<Texture2D>(prefix + "Construction/stage2_assembly.svg"),
                id + " switches to 70% weapon assembly");
            clock.Advance(1.4);
            Require(tower.IsOperational && !sprite.Visible && basis.Visible && gun.Visible &&
                sprite.Texture == GD.Load<Texture2D>(prefix + filename + ".svg") &&
                presenter.TextureChanges == 4 && !presenter.IsProcessing(),
                id + " restores original split recoil visuals at 100%");
            TowerBase direct = f.Add<TowerBase>(id, new Vector2(2200, 2064));
            Require(direct.IsOperational && !direct.GetNode<Sprite2D>("TurretPivot/ConstructionSprite").Visible &&
                direct.GetNode<Sprite2D>("BaseSprite").Visible,
                id + " direct operational instance keeps original sprite");
        }
    }

    // =========================================================
    // Arbitrary unsorted counts and invalid cosmetic data are safe, without becoming placement/gameplay validation.
    private static void DataAndFallback(Node runner)
    {
        TowerConstructionArt art = new() { BuildStages = new() {
            null, new() { Progress = float.NaN, Texture = Texture("stage1_frame") },
            new() { Progress = -.1f, Texture = Texture("stage1_frame") }, new() { Progress = 2, Texture = Texture("stage1_frame") },
            new() { Progress = 0 }, new() { Progress = .6f, Texture = Texture("stage2_assembly") },
            new() { Progress = .4f, Texture = Texture("stage1_frame") } } };
        Require(art.BuildTexture(.2, Original) == Original && art.BuildTexture(.45, Original) == Texture("stage1_frame")
            && art.BuildTexture(.8, Original) == Texture("stage2_assembly"), "No fixed stage count, sorting requirement or invalid-entry failure");
        using AmmoLoopFixture f = new(runner);
        foreach (TowerConstructionArt profile in new TowerConstructionArt[] { null, new() })
        {
            BattleTower tower = Create(f, profile, true);
            Require(tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath).Texture == Original && tower.IsBusy, "Null/empty style retains gameplay and normal image");
            f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork").Advance(3);
            Require(tower.IsOperational, "Missing art never delays completion");
        }
    }

    // =========================================================
    // Future optional unique level art is already supported, while unassigned later levels retain the completed model.
    private static void Upgrade(Node runner)
    {
        using AmmoLoopFixture f = new(runner);
        TowerConstructionArt art = new() { LevelArtwork = new() { new() { Level = 2, UpgradeProgress = .5f,
            UpgradeTexture = Texture("stage1_frame"), FinishedTexture = Texture("stage2_assembly") } } };
        BattleTower tower = Create(f, art, false); Sprite2D sprite = tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath);
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && sprite.Texture == Original, "Upgrade begins with old completed artwork");
        clock.Advance(1.9); Require(sprite.Texture == Original, "No intermediate image before its configured threshold");
        clock.Advance(.1); Require(sprite.Texture == Texture("stage1_frame") && tower.UpgradeLevel == 1 && tower.IsOperational, "One optional intermediate image does not change old-tier operation");
        f.Target(tower.GlobalPosition + new Vector2(0, -64)); tower._PhysicsProcess(1);
        Require(tower.Ammunition.Inventory.Amount == 999 && sprite.GetParent<Node2D>().Rotation == 0, "Working turret still aims/fires during optional transition art");
        clock.Advance(2); Require(sprite.Texture == Texture("stage2_assembly") && tower.UpgradeLevel == 2, "Final level2 image applies at actual completion");
        Require(sprite.Visible && !tower.GetNode<Sprite2D>("BaseSprite").Visible && !tower.GetNode<Node2D>("TurretPivot/RecoilRoot").Visible
            && !tower.GetNode<TowerRecoilController>("Recoil").Active, "Custom composite art shows alone and suspends split recoil");
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 2, out _), "Next tier starts");
        clock.Advance(5);
        Require(tower.UpgradeLevel == 3 && sprite.Texture == Texture("stage2_assembly"), "Unassigned next tier retains previous completed art, not Level1 or transition");
        BattleTower instant = Create(f, art, false); instant.NextUpgrade.WorkSeconds = 0;
        f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        Require(TowerUpgradeController.TryUpgrade(instant, f.Wallet, f.Placement.Settings, 1, out _)
            && instant.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath).Texture == Texture("stage2_assembly"), "Zero-duration upgrade art synchronizes immediately");
    }

    // =========================================================
    // Same presenter preserves full-footprint scale/centering, without opting the production Factory into stage art.
    private static void GeometryAndCleanup(Node runner)
    {
        using AmmoLoopFixture f = new(runner);
        AmmunitionFactory factory = f.Add<AmmunitionFactory>("ammo_factory", new(2300, 2000));
        Require(factory.GetNodeOrNull<Node>("ConstructionVisuals") == null && factory.Config.ConstructionArt == null, "Other real buildings do not gain presentation nodes/resources");
        TowerDefinition definition = f.Placement.Catalog.Find("ammo_factory");
        AmmunitionFactory staged = definition.Scene.Instantiate<AmmunitionFactory>(); staged.InitializeDefinition(definition);
        staged.Config.ConstructionArt = f.Placement.Catalog.Find("battle").DefaultConfig.ConstructionArt;
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        staged.StartConstruction(clock); staged.AddChild(new TowerConstructionVisuals { SpritePath = new NodePath("../Sprite") });
        f.Towers.AddChild(staged); staged.GlobalPosition = new(2500, 2000); Disable(staged);
        Sprite2D sprite = staged.GetNode<Sprite2D>("Sprite"); var transform = sprite.GlobalTransform;
        clock.Advance(3);
        Require(sprite.Texture == Texture("stage1_frame") && sprite.GlobalTransform == transform && sprite.Scale == new Vector2(3, 2)
            && staged.ProducedRounds == 0, "Actual stage swap preserves3×2 geometry and dormant production");
        TowerConstructionArt sharedArt = staged.Config.ConstructionArt; staged.Free();
        BattleTower tower = Create(f, sharedArt, true); var visual = tower.GetNode<TowerConstructionVisuals>("ConstructionVisuals");
        visual.Free(); clock.Advance(2.2);
        Require(tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath).Texture == Original && tower.IsBusy, "Removing presenter restores finished image and unsubscribes safely");
        tower.Free(); clock.Advance(100); Require(clock.ActiveCount == 0, "Mid-progress unload leaves no job/visual callback behind");
    }

    // =========================================================
    // Private source overrides are assigned before Ready, matching production placement order.
    private static BattleTower Create(AmmoLoopFixture f, TowerConstructionArt art, bool building)
    {
        TowerDefinition definition = f.Placement.Catalog.Find("battle"); BattleTower tower = definition.Scene.Instantiate<BattleTower>();
        tower.InitializeDefinition(definition); tower.Config.ConstructionArt = art; tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
        if (building) tower.StartConstruction(f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork"));
        f.Towers.AddChild(tower); tower.GlobalPosition = new(2200, 2000); Disable(tower); return tower;
    }
}
