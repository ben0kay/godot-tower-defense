// Shared deterministic 64px-cell silhouette math; no material names, scene reads or gameplay rules.
using Godot;

public static class TerrainEdgeMask
{
    #region Shape
    // =========================================================
    // Cut only against empty terrain; painted neighbours of any material keep a closed boundary.
    public static float Coverage(Vector2 p, TerrainEdgeNeighbours occupied, TerrainEdgeProfile profile, Vector2I cell, int seed)
    {
        if (((int)occupied & 15) == 15 || profile.Shape == TerrainEdgeShape.Straight) return 1;
        float radius = Mathf.Lerp(.02f, .34f, Mathf.Clamp(profile.CornerSoftness, 0, 1));
        float irregular = Mathf.Clamp(profile.Irregularity, 0, 1);
        float variation = Mathf.Clamp(profile.Variation, 0, 1);
        float x = cell.X + p.X, y = cell.Y + p.Y;
        float waveX = Wave(x, seed), waveY = Wave(y, seed + 31);
        float depthX = .012f + irregular * (.025f + .025f * waveX) * (1 + variation);
        float depthY = .012f + irregular * (.025f + .025f * waveY) * (1 + variation);
        bool n = Has(occupied, TerrainEdgeNeighbours.North), e = Has(occupied, TerrainEdgeNeighbours.East);
        bool s = Has(occupied, TerrainEdgeNeighbours.South), w = Has(occupied, TerrainEdgeNeighbours.West);
        float distance = 1;
        if (!n) distance = Mathf.Min(distance, p.Y - depthX);
        if (!s) distance = Mathf.Min(distance, 1 - p.Y - depthX);
        if (!w) distance = Mathf.Min(distance, p.X - depthY);
        if (!e) distance = Mathf.Min(distance, 1 - p.X - depthY);
        Corner(p, !w && !n); Corner(new(1 - p.X, p.Y), !e && !n);
        Corner(new(p.X, 1 - p.Y), !w && !s); Corner(Vector2.One - p, !e && !s);
        return Alpha(distance);
        void Corner(Vector2 q, bool exposed)
        { if (exposed && q.X < radius && q.Y < radius) distance = Mathf.Min(distance, radius - q.DistanceTo(Vector2.One * radius)); }
    }
    // =========================================================
    // An empty cell receives a fillet only where three matching painted neighbours surround its corner.
    public static float InnerCorner(Vector2 p, TerrainEdgeProfile profile)
    {
        if (profile.Shape == TerrainEdgeShape.Straight) return 0;
        float r = Mathf.Lerp(.02f, .34f, Mathf.Clamp(profile.CornerSoftness, 0, 1));
        return p.X < r && p.Y < r ? Alpha(p.DistanceTo(Vector2.One * r) - r) : 0;
    }
    // =========================================================
    // A continuous world-coordinate wave keeps adjacent exposed edges seamless and stable across runs.
    private static float Wave(float value, int seed) => .5f + .32f * Mathf.Sin(value * 3.1f + seed % 997) + .18f * Mathf.Sin(value * 7.7f + seed % 71);
    // =========================================================
    // One-pixel transition in the authored 64px coordinate system; texture filtering smooths the cached mask.
    private static float Alpha(float distance) => Mathf.Clamp(distance * 64 + .5f, 0, 1);
    // =========================================================
    // Shared direction flag helper.
    private static bool Has(TerrainEdgeNeighbours mask, TerrainEdgeNeighbours bit) => (mask & bit) != 0;
    #endregion
}
