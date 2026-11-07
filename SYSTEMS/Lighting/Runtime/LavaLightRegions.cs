// Build deterministic lava light anchors from connected cells in bounded map sections.
using Godot;
using System.Collections.Generic;

internal static class LavaLightRegions
{
    private const int SectionSize = 4;
    private static readonly Vector2I[] Neighbours = { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down };

    // =========================================================
    // Clip connectivity at section boundaries so long lava flows can have multiple local lights.
    internal static IEnumerable<Vector2I> FindAnchors(IEnumerable<Vector2I> cells)
    {
        List<Vector2I> ordered = new(cells);
        ordered.Sort((a, b) => a.Y == b.Y ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        HashSet<Vector2I> remaining = new(ordered);
        List<Vector2I> region = new(SectionSize * SectionSize);
        foreach (Vector2I start in ordered)
        {
            if (!remaining.Remove(start)) continue;
            Vector2I section = SectionFor(start);
            region.Clear();
            region.Add(start);
            Vector2 sum = Vector2.Zero;
            for (int i = 0; i < region.Count; i++)
            {
                Vector2I cell = region[i];
                sum += (Vector2)cell;
                foreach (Vector2I direction in Neighbours)
                {
                    Vector2I next = cell + direction;
                    if (SectionFor(next) == section && remaining.Remove(next)) region.Add(next);
                }
            }
            Vector2 centre = sum / region.Count;
            Vector2I anchor = start;
            foreach (Vector2I cell in region)
                if (centre.DistanceSquaredTo((Vector2)cell) < centre.DistanceSquaredTo((Vector2)anchor)) anchor = cell;
            yield return anchor;
        }
    }

    private static Vector2I SectionFor(Vector2I cell) =>
        new(Mathf.FloorToInt((float)cell.X / SectionSize), Mathf.FloorToInt((float)cell.Y / SectionSize));
}
