// Combine tightly grouped tower lamps while retaining each tower's own reveal footprint.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private sealed class TowerGlowGroup(Source representative, Vector2 seed, float radius)
    {
        public readonly Source Representative = representative;
        public readonly Vector2 Seed = seed;
        public readonly float Radius = radius;
        public Vector2 Sum = seed;
        public int Count = 1;
        public float Furthest;
    }

    private readonly List<TowerGlowGroup> _glowGroups = new();
    private readonly List<Source> _towerGlows = new();
    private readonly List<Source> _constructionGlows = new();
    private readonly Dictionary<Vector2I, Texture2D> _constructionFalloffs = new();
    private bool _towerGlowsDirty = true;

    private static Vector2 VisualPositionFor(Source source) =>
        source.ConstructionFootprint != Vector2I.Zero ? source.Anchor.GlobalPosition : source.GroupCentre ?? PositionFor(source);
    private float VisualRadiusFor(Source source) => source.GroupCentre.HasValue ? source.GroupRadius : RadiusFor(source);

    // =========================================================
    // Reuse groups until source state changes. When slots run short, favour the viewed area over placement age.
    private void PrepareTowerGlows(bool cull, Transform2D canvas, Rect2 viewport)
    {
        if (_towerGlowsDirty) RebuildTowerGlows();
        if (!cull) return;
        Vector2 centre = viewport.GetCenter();
        foreach (Source source in _towerGlows)
            source.CameraDistance = (canvas * VisualPositionFor(source)).DistanceSquaredTo(centre);
        foreach (Source source in _constructionGlows)
            source.CameraDistance = (canvas * VisualPositionFor(source)).DistanceSquaredTo(centre);
        _towerGlows.Sort(CompareTowerGlowDistance);
        _constructionGlows.Sort(CompareTowerGlowDistance);
    }

    private static int CompareTowerGlowDistance(Source a, Source b)
    {
        int distance = a.CameraDistance.CompareTo(b.CameraDistance);
        return distance != 0 ? distance : a.Anchor.GetInstanceId().CompareTo(b.Anchor.GetInstanceId());
    }

    // =========================================================
    // Anchor each group to its first tower so a chain of overlapping radii cannot swallow a whole base.
    private void RebuildTowerGlows()
    {
        _towerGlowsDirty = false;
        _glowGroups.Clear();
        _towerGlows.Clear();
        _constructionGlows.Clear();
        foreach (Source source in _towers)
        {
            source.GroupCentre = null;
            source.GroupEnergy = 1;
            source.ConstructionFootprint = Vector2I.Zero;
            if (!IsAvailable(source)) continue;
            if (source.UnderConstruction?.Invoke() == true && source.Anchor is TowerBase tower)
            {
                source.ConstructionFootprint = tower.Footprint;
                _constructionGlows.Add(source);
                continue;
            }

            Vector2 position = PositionFor(source);
            float radius = RadiusFor(source);
            TowerGlowGroup match = null;
            float best = float.PositiveInfinity;
            foreach (TowerGlowGroup group in _glowGroups)
            {
                if (Mathf.Abs(radius - group.Radius) > group.Radius * .15f) continue;
                float reach = Mathf.Min(radius, group.Radius) * .55f;
                float distance = position.DistanceSquaredTo(group.Seed);
                if (distance <= reach * reach && distance < best) { match = group; best = distance; }
            }
            if (match == null) _glowGroups.Add(new TowerGlowGroup(source, position, radius));
            else
            {
                match.Count++;
                match.Sum += position;
                match.Furthest = Mathf.Max(match.Furthest, Mathf.Sqrt(best));
            }
        }

        foreach (TowerGlowGroup group in _glowGroups)
        {
            Source source = group.Representative;
            source.GroupCentre = group.Sum / group.Count;
            source.GroupRadius = group.Radius + group.Furthest * .40f;
            _towerGlows.Add(source);
        }

        // Tame overlap between neighbouring groups without boosting a merged lamp's peak.
        foreach (Source source in _towerGlows)
        {
            float overlap = 0;
            foreach (Source neighbour in _towerGlows)
            {
                if (ReferenceEquals(source, neighbour)) continue;
                float distance = source.GroupCentre.Value.DistanceTo(neighbour.GroupCentre.Value);
                if (distance >= neighbour.GroupRadius) continue;
                float strength = 1 - distance / neighbour.GroupRadius;
                overlap += strength * strength;
            }
            source.GroupEnergy = 1 / (1 + overlap);
        }
    }

    // =========================================================
    // Bake one rectangular mask per footprint, with a one-cell fade measured from the building edge.
    private Texture2D GetConstructionFalloff(Vector2I footprint)
    {
        if (_constructionFalloffs.TryGetValue(footprint, out Texture2D texture)) return texture;

        Vector2I cells = footprint + new Vector2I(2, 2);
        int samplesPerCell = Mathf.Clamp(512 / Mathf.Max(cells.X, cells.Y), 1, 16);
        int width = cells.X * samplesPerCell;
        int height = cells.Y * samplesPerCell;
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            int edgeY = Mathf.Min(y, height - 1 - y);
            for (int x = 0; x < width; x++)
            {
                int edge = Mathf.Min(edgeY, Mathf.Min(x, width - 1 - x));
                float weight = Mathf.SmoothStep(0, 1, (edge + .5f) / samplesPerCell);
                int index = (y * width + x) * 4;
                pixels[index] = pixels[index + 1] = pixels[index + 2] = 255;
                pixels[index + 3] = (byte)Mathf.RoundToInt(weight * 255);
            }
        }

        using Image image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);
        texture = ImageTexture.CreateFromImage(image);
        _constructionFalloffs.Add(footprint, texture);
        return texture;
    }
}
