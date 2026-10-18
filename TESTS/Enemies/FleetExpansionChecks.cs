// Real catalog/spawn/defense/reward and mixed-carrier ownership regression for the expanded fleet.
using Godot;
using System;
using System.Linq;
using static MissionFixture;

public partial class FleetExpansionChecks : Node
{
    #region Runner
    // =========================================================
    // Exercise imported resources in real battlefields, including the multi-job final carrier.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            using (DeathSpawnFixture f = new(this)) CheckDefinitionsAndSpawns(f);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            foreach (string scene in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene })
            {
                using DeathSpawnFixture f = new(this, scene);
                Enemy carrier = f.Parent(true, "heavy_transporter_mk1");
                var destination = carrier.AssignedCitadel;
                carrier.TakeDamage(99999);
                Require(f.Queue.PendingJobs == 2 && f.Waves.PendingDeathSpawns == 10 && f.Waves.AliveCount == 0,
                    "Both payloads reserve their children before the final carrier is removed");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(f.Mission.Session.State == LevelSessionState.Playing, "Pending mixed payload prevents premature victory");
                f.FinishReleases();
                Enemy[] children = f.Spawns.Registry.Living.ToArray();
                Require(children.Length == 10 && children.Count(e => e.Definition.EnemyId == "basic_mk1") == 6
                    && children.Count(e => e.Definition.EnemyId == "scout_mk1") == 4 && f.Waves.AliveCount == 10
                    && children.All(e => e.CurrentShield == 0 && e.SpawnGeneration == 1
                        && e.AssignedCitadel == destination), "Mixed payload has independent stats, wave ownership and inherited destination");
                foreach (Enemy child in children.Take(9)) child.TakeDamage(99999);
                Require(f.Mission.Session.State == LevelSessionState.Playing, "Last child keeps mission active");
                children[9].TakeDamage(99999);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(f.Mission.Session.Snapshot?.Outcome == LevelOutcome.Victory
                    && f.Mission.Session.Snapshot.EnemiesKilled == 11, "Victory accounts for carrier and all ten children");
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: fleet families, all 13 catalog ships, new hulls/shields/rewards/arrival and mixed carrier victory on both maps");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    #endregion

    #region Catalog and Combat
    // =========================================================
    // Metadata must load from disk; real new ships retain the existing shield opt-in and lifecycle.
    private static void CheckDefinitionsAndSpawns(DeathSpawnFixture f)
    {
        var map = f.Spawns.Catalog.ToSceneMap();
        Require(map.Count == 13, "Seven existing and six additional types are registered");
        foreach (var pair in map)
        {
            EnemyDefinition data = f.Spawns.Catalog.DefinitionFor(pair.Key);
            Require(Enum.IsDefined(data.Family), "Every registered type has a valid family");
        }
        EnemyDefinition bad = new() { EnemyId = "invalid_family", Family = (EnemyFamily)999 };
        bool rejected = false;
        try { EnemyCatalog.ValidateDefinition(bad); } catch (ArgumentException) { rejected = true; }
        Require(rejected, "Invalid serialized family values fail catalog validation"); bad.Dispose();
        var cases = new[] {
            ("reinforced_drone_mk1", EnemyFamily.Drone, 110f, 100f, new Vector2(40, 30)),
            ("racer_mk1", EnemyFamily.Interceptor, 20f, 230f, new Vector2(34, 18)),
            ("shield_frigate_mk1", EnemyFamily.ShieldShip, 120f, 65f, new Vector2(72, 48)),
            ("heavy_bulwark_mk1", EnemyFamily.Bulwark, 500f, 50f, new Vector2(88, 64)),
            ("cruiser_mk1", EnemyFamily.CapitalShip, 3000f, 30f, new Vector2(168, 96)),
            ("heavy_transporter_mk1", EnemyFamily.Carrier, 1400f, 35f, new Vector2(144, 100)) };
        var wallet = f.Main.GetNode<EconomyManager>("Economy");
        foreach (var (id, family, hull, speed, size) in cases)
        {
            Enemy ship = f.Spawns.Spawn(id, true);
            Require(ship != null, "Catalog ship can spawn at an actual portal: " + id);
            AmmoLoopFixture.Disable(ship);
            Require(ship.Definition.Family == family && ship.Health == hull && ship.Definition.MoveSpeed == speed
                && ship.CurrentShield == 0 && ship.GetNode<Sprite2D>("Sprite").Texture.GetSize() == size,
                "Authored family/balance/texture and unshielded default: " + id);
            ship.TakeDamage(1); Require(ship.Health == hull - 1, "Shared hull damage: " + id);
            int hp = f.Mission.Health.CurrentHealth;
            ship.Arrive(); ship.Arrive();
            Require(f.Mission.Health.CurrentHealth == hp - ship.Definition.CitadelDamage && f.Queue.PendingJobs == 0,
                "Arrival damages once and never releases carrier payload: " + id);
            f.Mission.Health.Initialize(f.Mission.Health.MaxHealth);
            if (id == "heavy_transporter_mk1") continue;
            Enemy killed = f.Spawns.Spawn(id, true); AmmoLoopFixture.Disable(killed);
            int credits = wallet.GetBalance(wallet.CreditsResource);
            killed.TakeDamage(99999); killed.TakeDamage(99999);
            Require(wallet.GetBalance(wallet.CreditsResource) == credits + killed.Definition.KillRewardCredits,
                "Existing kill reward pays once: " + id);
        }
        Enemy frigate = f.Spawns.SpawnWave("shield_frigate_mk1", new RandomNumberGenerator { Seed = 42 }, spawnWithShield: true);
        AmmoLoopFixture.Disable(frigate);
        Require(frigate.CurrentShield == 350 && frigate.GetNodeOrNull<ShieldVisual>("ShieldVisual") != null,
            "Wave-opted Shield Frigate starts with its full shared shield aura");
        frigate.TakeDamage(350);
        Require(frigate.Health == 120 && frigate.CurrentShield == 0, "Frigate shield absorbs hits before hull");
        frigate.TakeDamage(120);
    }
    #endregion
}
