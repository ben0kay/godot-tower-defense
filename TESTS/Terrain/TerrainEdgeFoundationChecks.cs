// Foundation-only terrain edge checks: authored bindings, neutral defaults and pure neighbour masks.
using Godot;
using System;
using System.Collections.Generic;

public static class TerrainEdgeFoundationChecks
{
    // =========================================================
    // No live renderer is attached; these checks are safe during existing terrain regression tests.
    public static void Run()
    {
        TerrainEdgeMasterConfig config = GD.Load<TerrainEdgeMasterConfig>(
            "res://WORLD/TERRAIN/Edges/DefaultTerrainEdgeConfig.tres");
        Require(config != null && !config.Enabled && config.Profiles.Count == 2,
            "Terrain edge foundation is authored but defaults to fully disabled");

        TerrainEdgeProfile dust = config.Find(TerrainEdgeLayer.Passable, 2, new(0, 0));
        TerrainEdgeProfile rock = config.Find(TerrainEdgeLayer.Impassable, 2, new(1, 0));
        Require(dust != null && dust.MaterialId == "mars_dust" && dust.Shape == TerrainEdgeShape.Soft,
            "Dust profile identifies its passable material");
        Require(rock != null && rock.MaterialId == "mars_rock" && rock.Shape == TerrainEdgeShape.Jagged,
            "Rock profile identifies its impassable material");
        Require(config.Find(TerrainEdgeLayer.Passable, 2, new(1, 0)) == null &&
                config.Find(TerrainEdgeLayer.Impassable, 2, new(0, 0)) == null,
            "Visual material profiles never confuse the two movement layers");
        Require(config.Find(TerrainEdgeLayer.Passable, 1, new(2, 0)) == null,
            "Unregistered metal profile does not implicitly enable rendering");

        var map = new Dictionary<Vector2I, string> {
            [new(0, 0)] = "mars_dust", [new(1, 0)] = "mars_dust",
            [new(1, 1)] = "mars_rock", [new(0, -1)] = "mars_dust",
            [new(-1, 1)] = "mars_dust"
        };
        TerrainEdgeNeighbours mask = TerrainEdgeNeighbourhood.Matching(new(0, 0), "mars_dust", map);
        Require(mask == (TerrainEdgeNeighbours.North | TerrainEdgeNeighbours.East | TerrainEdgeNeighbours.SouthWest),
            "Eight-way neighbours match material identities, not generic painted cells");
        Require(TerrainEdgeNeighbourhood.Matching(new(0, 0), "mars_rock", map) == TerrainEdgeNeighbours.SouthEast,
            "Different materials produce distinct neighbour masks");
        Require(TerrainEdgeNeighbourhood.Matching(new(0, 0), "", map) == TerrainEdgeNeighbours.None,
            "Missing material identities generate no edges");
        GD.Print("PASS: terrain edge configuration is disabled, source mappings are isolated, and eight-neighbour masks are deterministic");
    }

    // =========================================================
    // Fail integration suite loudly on malformed exported profiles or broken corner classification.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
