// Pure eight-neighbour material mask: no Godot nodes, no TileMap reads, no physics.
using Godot;
using System;
using System.Collections.Generic;

[Flags]
public enum TerrainEdgeNeighbours : byte
{
    None = 0, North = 1, East = 2, South = 4, West = 8,
    NorthEast = 16, SouthEast = 32, SouthWest = 64, NorthWest = 128
}

public static class TerrainEdgeNeighbourhood
{
    // =========================================================
    // Compare MATERIAL IDs, not tile atlas coordinates; variants within one material behave as one region.
    public static TerrainEdgeNeighbours Matching(Vector2I cell, string materialId,
        IReadOnlyDictionary<Vector2I, string> materials)
    {
        if (materials == null || string.IsNullOrEmpty(materialId)) return TerrainEdgeNeighbours.None;
        TerrainEdgeNeighbours mask = TerrainEdgeNeighbours.None;
        Match(0, -1, TerrainEdgeNeighbours.North); Match(1, 0, TerrainEdgeNeighbours.East);
        Match(0, 1, TerrainEdgeNeighbours.South); Match(-1, 0, TerrainEdgeNeighbours.West);
        Match(1, -1, TerrainEdgeNeighbours.NorthEast); Match(1, 1, TerrainEdgeNeighbours.SouthEast);
        Match(-1, 1, TerrainEdgeNeighbours.SouthWest); Match(-1, -1, TerrainEdgeNeighbours.NorthWest);
        return mask;

        void Match(int x, int y, TerrainEdgeNeighbours direction)
        {
            if (materials.TryGetValue(cell + new Vector2I(x, y), out string other) && other == materialId)
                mask |= direction;
        }
    }
}
