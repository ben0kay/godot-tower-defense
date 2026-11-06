// Cached selected-only sight shading; no processing loop, per-tower effect or per-frame visibility scan.
using Godot;
using System;
using System.Collections.Generic;

public partial class TowerRangeOverlay : Node2D
{
    private const int Samples = 384;
    private WorldGrid _grid;
    private TowerMasterConfig _settings;
    private TowerBase _tower;
    private ArrayMesh _fill;
    private Vector2[] _visibleLines = Array.Empty<Vector2>(), _blockedLines = Array.Empty<Vector2>();
    private readonly Vector2[] _outer = new Vector2[Samples + 1], _inner = new Vector2[Samples + 1];
    private readonly float[] _fractions = new float[Samples + 1];
    private Vector2 _origin;
    private float _radius = -1;
    private bool _usesSight;

    // =========================================================
    // Explicit ownership from placement; one dormant helper survives selection changes.
    public void Configure(WorldGrid grid, TowerMasterConfig settings)
    {
        _grid = grid; _settings = settings;
        _grid.TerrainRebuilt += TerrainChanged;
        _settings.Changed += SettingsChanged;
        Hide(); SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Follow only the chosen placed tower; no world lookup or subscription for every tower.
    public void Present(TowerBase tower)
    {
        if (_tower == tower) { Rebuild(); return; }
        Clear();
        if (!GodotObject.IsInstanceValid(tower) || tower.IsQueuedForDeletion()) return;
        _tower = tower;
        _tower.StatsChanged += StatsChanged;
        _tower.SpatialChanged += TowerMoved;
        _tower.TreeExiting += Clear;
        Rebuild(true);
    }

    // =========================================================
    // Closing the Inspector/deselecting releases subscriptions and native geometry immediately.
    public void Clear()
    {
        if (GodotObject.IsInstanceValid(_tower))
        {
            _tower.StatsChanged -= StatsChanged; _tower.SpatialChanged -= TowerMoved;
            _tower.TreeExiting -= Clear;
        }
        _tower = null; _radius = -1; ReleaseGeometry(); Hide(); QueueRedraw();
    }
    private void StatsChanged() => Rebuild();
    private void TowerMoved(TowerBase tower) => Rebuild();
    private void TerrainChanged() => Rebuild(true);
    private void SettingsChanged() => Rebuild(true);

    // =========================================================
    // Range/position snapshots avoid regenerating on unrelated rank, heat, ammo or health events.
    private void Rebuild(bool force = false)
    {
        if (!GodotObject.IsInstanceValid(_tower)) return;
        float radius = _tower.InspectionRange ?? 0;
        if (!_settings.ShowSelectionRange || !float.IsFinite(radius) || radius <= 0)
        { ReleaseGeometry(); _radius = -1; Hide(); QueueRedraw(); return; }
        Vector2 origin = _tower.GlobalPosition;
        // The hostile aimed policies use terrain sight. Support/passive/untargeted circles stay honest.
        bool sight = _tower.AuthoredConfig is ITowerTargetingPolicy;
        if (!force && _radius == radius && _origin == origin && _usesSight == sight) return;
        ReleaseGeometry(); _radius = radius; _origin = origin; _usesSight = sight;
        if (!sight) { Show(); QueueRedraw(); return; }
        bool blockers = _grid.TowerSightBlockerCount > 0;
        for (int i = 0; i < Samples; i++)
        {
            Vector2 end = origin + Vector2.FromAngle(Mathf.Tau * i / Samples) * radius;
            float fraction = blockers ? VisibleFraction(origin, end) : 1;
            _fractions[i] = fraction;
            _outer[i] = ToLocal(end); _inner[i] = ToLocal(origin.Lerp(end, fraction));
        }
        _outer[Samples] = _outer[0]; _inner[Samples] = _inner[0]; _fractions[Samples] = _fractions[0];
        var vertices = new List<Vector3>(Samples * 9);
        var colors = new List<Color>(Samples * 9);
        var visible = new List<Vector2>(Samples * 2);
        var blocked = new List<Vector2>(Samples * 2);
        Vector2 center = ToLocal(origin);
        Color cyan = WithAlpha(_settings.VisibleRangeColor, _settings.VisibleRangeFillOpacity);
        Color red = WithAlpha(_settings.BlockedRangeColor, _settings.BlockedRangeFillOpacity);
        for (int i = 0; i < Samples; i++)
        {
            Add(center, _inner[i], _inner[i + 1], cyan);
            bool obstructed = _fractions[i] < 1 || _fractions[i + 1] < 1;
            if (obstructed)
            {
                Add(_inner[i], _outer[i], _outer[i + 1], red);
                Add(_inner[i], _outer[i + 1], _inner[i + 1], red);
            }
            var lines = obstructed ? blocked : visible;
            lines.Add(_outer[i]); lines.Add(_outer[i + 1]);
        }
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        _fill = new ArrayMesh(); _fill.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        _visibleLines = visible.ToArray(); _blockedLines = blocked.ToArray();
        Show(); QueueRedraw();

        void Add(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            vertices.Add(new(a.X, a.Y, 0)); vertices.Add(new(b.X, b.Y, 0)); vertices.Add(new(c.X, c.Y, 0));
            colors.Add(color); colors.Add(color); colors.Add(color);
        }
    }

    // =========================================================
    // Bounded visual approximation reuses the authoritative supercover query; gameplay remains exact.
    // Binary refinement costs at most ten short traversals per ray, only when rebuilding selection.
    private float VisibleFraction(Vector2 origin, Vector2 end)
    {
        if (_grid.HasWeaponLineOfSight(origin, end)) return 1;
        float low = 0, high = 1;
        for (int i = 0; i < 10; i++)
        {
            float middle = (low + high) * 0.5f;
            if (_grid.HasWeaponLineOfSight(origin, origin.Lerp(end, middle))) low = middle;
            else high = middle;
        }
        return low;
    }

    // =========================================================
    // Recorded mesh/line commands follow the camera naturally without rebuilding for pan/zoom.
    public override void _Draw()
    {
        if (_radius <= 0 || !GodotObject.IsInstanceValid(_tower)) return;
        if (!_usesSight)
        {
            DrawArc(ToLocal(_origin), _radius, 0, Mathf.Tau, 96, _settings.VisibleRangeColor, 2);
            return;
        }
        if (_fill != null) DrawMesh(_fill, null, Transform2D.Identity, Colors.White);
        if (_visibleLines.Length > 0) DrawMultiline(_visibleLines, _settings.VisibleRangeColor, 2);
        if (_blockedLines.Length > 0) DrawMultiline(_blockedLines, _settings.BlockedRangeColor, 2);
    }

    // =========================================================
    // Native buffers are replaced on explicit invalidation and released at deselect/unload.
    private void ReleaseGeometry()
    {
        _fill?.Dispose(); _fill = null;
        _visibleLines = Array.Empty<Vector2>(); _blockedLines = Array.Empty<Vector2>();
    }
    public override void _ExitTree()
    {
        Clear();
        if (GodotObject.IsInstanceValid(_grid)) _grid.TerrainRebuilt -= TerrainChanged;
        if (GodotObject.IsInstanceValid(_settings)) _settings.Changed -= SettingsChanged;
        _grid = null; _settings = null;
    }
    private static Color WithAlpha(Color color, float alpha) => new(color.R, color.G, color.B, Mathf.Clamp(alpha, 0, 1));
}
