// Combine tightly grouped tower lamps while retaining each tower's own reveal footprint.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private sealed class TowerGlowGroup(Source representative, Vector2 seed, float radius, bool building)
    {
        public readonly Source Representative = representative;
        public readonly Vector2 Seed = seed;
        public readonly float Radius = radius;
        public readonly bool Building = building;
        public Vector2 Sum = seed;
        public int Count = 1;
        public float Furthest;
    }

    private readonly List<TowerGlowGroup> _glowGroups = new();
    private readonly List<Source> _towerGlows = new();
    private bool _towerGlowsDirty = true;

    private static Vector2 VisualPositionFor(Source source) => source.GroupCentre ?? PositionFor(source);
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
        _towerGlows.Sort(CompareTowerGlowDistance);
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
        foreach (Source source in _towers)
        {
            source.GroupCentre = null;
            source.GroupEnergy = 1;
            if (!IsAvailable(source)) continue;
            Vector2 position = PositionFor(source);
            float radius = RadiusFor(source);
            bool building = source.UnderConstruction?.Invoke() == true;
            TowerGlowGroup match = null;
            float best = float.PositiveInfinity;
            foreach (TowerGlowGroup group in _glowGroups)
            {
                if (building != group.Building || Mathf.Abs(radius - group.Radius) > group.Radius * .15f) continue;
                float reach = Mathf.Min(radius, group.Radius) * .55f;
                float distance = position.DistanceSquaredTo(group.Seed);
                if (distance <= reach * reach && distance < best) { match = group; best = distance; }
            }
            if (match == null) _glowGroups.Add(new TowerGlowGroup(source, position, radius, building));
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
}
