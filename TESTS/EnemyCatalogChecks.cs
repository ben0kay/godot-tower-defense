// Catalog-driven mixed enemies, finite wave ownership and baseline navigation/lifecycle coverage.
using Godot;
using System;
using System.Linq;
using static WaveBuilder;
public static class EnemyCatalogChecks
{
    // =========================================================
    // Registered IDs/definitions remain canonical; real waves spawn both plain enemy types safely.
    public static void Run(Node main, SpawnManager spawns)
    {
        // Numbered scene filenames are development labels; wave identity and UI names are independent.
        Require(GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy001/Enemy001.tres").EnemyId == "basic_mk1"
            && GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy001/Enemy001.tres").DisplayName == "Standard Drone"
            && GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy002/Enemy002.tres").EnemyId == "scout_mk1"
            && GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy002/Enemy002.tres").DisplayName == "Swift Interceptor"
            && GD.Load<EnemyDefinition>("res://ENEMIES/Types/ShieldGenerator/ShieldGenerator.tres").EnemyId == "shield_emitter_mk1"
            && GD.Load<EnemyDefinition>("res://ENEMIES/Types/ShieldGenerator/ShieldGenerator.tres").DisplayName == "Shield Generator",
            "Numbered scene assets have independent stable gameplay IDs and display names");
        EnemyCatalog catalog = spawns.Catalog;
        var scenes = WaveCatalog.LoadScenes();
        Require(catalog.Find("basic_mk1") == scenes["basic_mk1"] && catalog.Find("scout_mk1") == scenes["scout_mk1"] && catalog.Find("unknown") == null,
            "Wave adapter reads actual enemy registration");
        Invalid(() => new EnemyCatalog { Scenes = new() { scenes["basic_mk1"], scenes["basic_mk1"] } }.Rebuild());
        Invalid(() => new EnemyCatalog { Scenes = new() { null } }.Rebuild());
        foreach (Action<Enemy> edit in new Action<Enemy>[] { e => e.Definition.EnemyId = "", e => e.Definition.MaxHealth = float.NaN,
            e => e.Definition.RadiusPixels = -1, e => e.Definition.MoveSpeed = 0, e => e.Definition.MaxShield = -1,
            e => e.GetNode("Movement").Free() })
        { PackedScene bad = Edited(scenes["scout_mk1"], edit); Invalid(() => new EnemyCatalog { Scenes = new() { bad } }.Rebuild()); }
        var mismatch = new System.Collections.Generic.Dictionary<string, PackedScene> { ["basic_mk1"] = scenes["scout_mk1"] };
        Invalid(() => WaveCatalog.Validate(new[] { Wave(1, Batch(1, 0, 0, Enemy("basic_mk1"))) }, mismatch));
        Require(spawns.Spawn("unknown", true) == null && spawns.LastMessage.Contains("Unknown EnemyId"), "Unknown spawn cannot fall back to Enemy001");
        PackedScene oversized = Edited(scenes["scout_mk1"], e => e.Definition.RadiusPixels = 100);
        Require(spawns.SpawnWave(oversized, new RandomNumberGenerator { Seed = 1 }) == null && spawns.LastMessage.Contains("radius"),
            "Oversized ship is held with a clear navigation-fit error");
        WaveManager waves = main.GetNode<WaveManager>("Waves");
        waves.Configure(new[] { Wave(1, Batch(8, 0, 0, Enemy("basic_mk1", weight: 75),
            Enemy("scout_mk1", weight: 25, portal: "Portals/EnemyPortal2", shield: true))) }, scenes);
        Require(waves.StartWave(), "Catalog mixed wave starts");
        for (int i = 0; waves.PlannedRemaining > 0 && i < 10; i++) waves._PhysicsProcess(0);
        Enemy[] ships = spawns.Registry.Living.ToArray();
        foreach (Enemy ship in ships) ship.Movement.SetPhysicsProcess(false);
        Require(ships.Length == 8 && ships.Count(e => e.Definition.EnemyId == "scout_mk1") == 2 && waves.AliveCount == 8,
            "Exact75/25 quotas and owned counts support two registered IDs");
        Enemy fast = ships.First(e => e.Definition.EnemyId == "scout_mk1"), basic = ships.First(e => e.Definition.EnemyId == "basic_mk1");
        Require(fast.Health == 30 && fast.CurrentShield == 40 && basic.Health == 60 && basic.CurrentShield == 0 &&
            fast.Definition.MoveSpeed == 160 && fast.Definition.RadiusPixels == 8, "Weaker/faster/smaller Enemy002 has independent hull/shield state");
        Vector2 exit = spawns.Paths.Navigation.Grid.Level.GetNode<Marker2D>("Portals/EnemyPortal2/SpawnExit").GlobalPosition;
        Require(fast.GlobalPosition.DistanceTo(exit) <= 12, "Explicit Enemy002 portal remains respected");
        for (int i = 0; i < 30; i++) spawns.Paths._PhysicsProcess(1.0 / 60);
        Vector2 before = fast.GlobalPosition;
        for (int i = 0; i < 10; i++) fast.Movement._PhysicsProcess(1.0 / 60);
        Require(fast.GlobalPosition.DistanceTo(before) > 0 && spawns.Paths.Navigation.HasClearance(fast.GlobalPosition, fast.Definition.RadiusPixels),
            "Enemy002 follows a budgeted valid route using its authored radius/speed");
        Require(TowerTargeting.Find(spawns.Registry, fast.GlobalPosition, 1, TowerTargeting.Priority.Closest) == fast,
            "Shared towers can target the new enemy type");
        float otherHealth = basic.Health;
        fast.ApplyDamage(new DamageInfo(10, DamageType.Energy));
        Require(fast.CurrentShield == 20 && fast.Health == 30 && basic.Health == otherHealth, "Typed hits preserve two-type instance isolation");
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
        fast.TakeDamage(10000); fast.TakeDamage(10000);
        Require(wallet.GetBalance(wallet.CreditsResource) == credits + 2, "Enemy002's own kill reward pays once");
        Enemy arrival = ships.Last(e => e.Definition.EnemyId == "scout_mk1");
        CitadelHealth health = main.GetNode<CitadelSession>("CitadelSession").Health; int hp = health.CurrentHealth;
        arrival.Arrive(); arrival.Arrive();
        Require(health.CurrentHealth == hp - 1 && waves.AliveCount == 6, "Enemy002 arrival damages Citadel/owned count once");
        foreach (Enemy ship in ships) if (ship.IsAlive) ship.TakeDamage(10000);
        Require(waves.State == WaveManager.WaveState.AllWavesComplete, "Mixed wave finishes only after every owned ship resolves");
        waves.Configure(WaveCatalog.GetWaves(spawns.Paths.Navigation.Grid.Level.Settings.WaveSetId), scenes);
        wallet.Initialize(spawns.Paths.Navigation.Grid.Level.Settings); health.Initialize(health.MaxHealth);
        GD.Print("PASS: enemy catalog validation, two-type exact waves/portals, Enemy002 movement/state/targeting/reward/arrival and oversized spawn rejection");
    }
    // =========================================================
    // Create invalid/oversized setup fixtures without altering shared authored resources.
    private static PackedScene Edited(PackedScene source, Action<Enemy> edit)
    {
        Enemy root = source.Instantiate<Enemy>(); root.Definition = (EnemyDefinition)root.Definition.Duplicate(); edit(root);
        PackedScene result = new(); result.Pack(root); root.Free(); return result;
    }
    // =========================================================
    // Invalid registration/setup must fail before any wave/spawn mutation.
    private static void Invalid(Action action)
    { try { action(); } catch (ArgumentException) { return; } throw new InvalidOperationException("Invalid enemy registration accepted."); }
    // =========================================================
    // Stop the integration checkpoint on multi-type regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
