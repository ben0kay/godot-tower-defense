// Authored Level020 scene and tile connectivity contract; tests static map content without mutating Main/Level001.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Level020Checks
{
    // =========================================================
    // Verify editor-authored tiles and portals before they become a live world grid.
    public static void Run()
    {
        PackedScene packed = GD.Load<PackedScene>("res://LEVELS/Level020/Level020.tscn");
        Require(packed != null, "Level020 scene loads");
        WorldLevel level = packed.Instantiate<WorldLevel>();
        try
        {
            Require(level.Settings != null && level.Settings.WaveSetId == "level020" &&
                level.Settings.ActivePortals.Count == 5 && level.Settings.StartingCredits == 1700,
                "Level020 has independent wave set, wallet, five portals");
            var ground = new HashSet<Vector2I>(level.Passable.GetUsedCells());
            var walls = new HashSet<Vector2I>(level.Impassable.GetUsedCells());
            var foundation = new HashSet<Vector2I>(level.Interactive.GetUsedCells());
            Require(ground.Count > 4500 && foundation.Count > 2800 && walls.Count > 300,
                "The western open ground and eastern maze are fully painted");
            Require(ground.Contains(new(30, 40)) && !foundation.Contains(new(30, 40)) &&
                foundation.Contains(new(52, 40)) && !ground.Contains(new(52, 40)),
                "Left is passable and east uses raised solid buildable platforms");
            TileData wallTile = level.Interactive.GetCellTileData(new Vector2I(52, 40));
            Require(wallTile != null && wallTile.GetCustomData("can_build").AsBool() &&
                wallTile.GetCustomData("blocks_enemies").AsBool(),
                "Maze walls are buildable Foundation, not ordinary unbuildable obstacles");
            Require(ground.Contains(new(60, 25)) && foundation.Contains(new(61, 27)) &&
                ground.Contains(new(74, 37)) && ground.Contains(new(73, 37)) && ground.Contains(new(72, 37)),
                "Single-lane and three-wide corridors exist in the maze");
            var approaches = new HashSet<Vector2I>() { new(89, 40), new(91, 38), new(93, 40), new(91, 42) };
            foreach (Vector2I cell in approaches)
                Require(ground.Contains(cell), "Citadel approach marker must be passable: " + cell);
            Node2D portals = level.GetNode<Node2D>("Portals");
            Require(portals.GetChildCount() == 5, "Exactly five real portal nodes");
            var blocked = new HashSet<Vector2I>(foundation);
            blocked.UnionWith(walls);
            for (int y = 39; y <= 41; y++)
                for (int x = 90; x <= 92; x++) blocked.Add(new(x, y));
            foreach (NodePath path in level.Settings.ActivePortals)
            {
                EnemyPortal portal = level.GetNode<EnemyPortal>(path);
                Require(portal != null && portal.GetNodeOrNull<Marker2D>("SpawnExit") != null,
                    "Configured portal exists and has a SpawnExit: " + path);
                Vector2 position = portal.Position + portal.GetNode<Marker2D>("SpawnExit").Position;
                Vector2I start = new(Mathf.FloorToInt(position.X / 64), Mathf.FloorToInt(position.Y / 64));
                Require(start.X < 20 && ground.Contains(start) && CanReach(start, approaches, blocked),
                    "Each west portal reaches the east Citadel through the labyrinth: " + path);
            }
            VerifySevenWaveAssault();
            GD.Print("PASS: Level020 five-portal painted western field/eastern buildable maze and walkable Citadel routes");
        }
        finally { level.Free(); }
    }

    // =========================================================
    // Seven-wave content contract: hordes first, escort carriers second, major armoured threats last.
    // Validate real enemy scene/catalog IDs rather than assuming weight entries resolve at runtime.
    private static void VerifySevenWaveAssault()
    {
        WaveDefinition[] waves = WaveCatalog.GetWaves("level020");
        Require(waves.Length == 7 && waves.Select(w => w.Number).SequenceEqual(Enumerable.Range(1, 7)),
            "The Five Gates defines seven consecutive waves");
        Require(waves.Select(w => w.Batches.Sum(b => b.Count)).SequenceEqual(new[] { 52, 68, 80, 72, 72, 60, 58 }),
            "Authored enemy counts progress from light swarms to the heavy finale");
        WaveCatalog.Validate(waves, WaveCatalog.LoadScenes());

        string[] light = { "basic_mk1", "scout_mk1", "racer_mk1", "reinforced_drone_mk1" };
        Require(waves.Take(2).SelectMany(w => w.Batches).SelectMany(b => b.Entries)
            .All(e => light.Contains(e.EnemyId)), "Waves 1-2 consist of small ships only");
        Require(waves[2].Batches.SelectMany(b => b.Entries).Any(e => e.EnemyId == "medium_mk1")
            && waves.Take(3).SelectMany(w => w.Batches).SelectMany(b => b.Entries)
                .All(e => e.EnemyId != "transporter_mk1" && e.EnemyId != "heavy_transporter_mk1"),
            "Wave 3 adds defensive escorts before transports arrive");
        Require(waves[3].Batches.SelectMany(b => b.Entries).Any(e => e.EnemyId == "transporter_mk1")
            && waves[4].Batches.SelectMany(b => b.Entries).Any(e => e.EnemyId == "heavy_transporter_mk1")
            && waves[5].Batches.SelectMany(b => b.Entries).Any(e => e.EnemyId == "heavy_bulwark_mk1" || e.EnemyId == "big_mk1"),
            "Waves 4-6 progressively introduce carriers, then the armoured fleet");
        Require(waves.Take(6).SelectMany(w => w.Batches).SelectMany(b => b.Entries)
                .All(e => e.EnemyId != "cruiser_mk1")
            && waves[6].Batches.Last().Entries.Any(e => e.EnemyId == "cruiser_mk1" && e.SpawnWithShield),
            "Shielded Cruisers are reserved for the wave-7 finale");
        var rng = new RandomNumberGenerator { Seed = 20 };
        var finale = WaveCatalog.Compose(waves[6].Batches.Last(), rng);
        Require(finale.Count(e => e.EnemyId == "cruiser_mk1" && e.SpawnWithShield) == 2,
            "Final batch guarantees exactly two shielded Cruisers");
        Require(waves.SelectMany(w => w.Batches).SelectMany(b => b.Entries).All(e => e.Portal == ""),
            "All waves retain random eligible distribution across the five level portals");
    }

    // =========================================================
    // One conservative cardinal flood fill; no diagonal corner cutting can mask a sealed route.
    private static bool CanReach(Vector2I start, HashSet<Vector2I> goals, HashSet<Vector2I> blocked)
    {
        Queue<Vector2I> queue = new(); HashSet<Vector2I> seen = new() { start }; queue.Enqueue(start);
        Vector2I[] dirs = { Vector2I.Right, Vector2I.Left, Vector2I.Up, Vector2I.Down };
        while (queue.Count > 0)
        {
            Vector2I p = queue.Dequeue();
            if (goals.Contains(p)) return true;
            foreach (Vector2I d in dirs)
            {
                Vector2I n = p + d;
                if (n.X <= 0 || n.Y <= 0 || n.X >= 99 || n.Y >= 79 || blocked.Contains(n) || !seen.Add(n)) continue;
                queue.Enqueue(n);
            }
        }
        return false;
    }

    // =========================================================
    // Fail fast if a painted world or linked scene is broken.
    private static void Require(bool valid, string message)
    { if (!valid) throw new InvalidOperationException(message); }
}
