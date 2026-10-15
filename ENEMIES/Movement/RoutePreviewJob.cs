// Bounded debug-only route planning over shared navigation; no spawn counters, live routes or gameplay RNG.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public sealed class RoutePreviewJob
{
    public sealed record Result(int Sample, int Profile, Vector2[] Points, float Length, int DuplicateOf);
    public readonly List<Result> Results = new();
    public Vector2[] Neutral { get; private set; } = Array.Empty<Vector2>();
    public float NeutralLength { get; private set; }
    public bool Complete { get; private set; }
    public int LastCalculations { get; private set; }
    public int Failed { get; private set; }
    public int TotalCalculations { get; private set; }
    public string Message { get; private set; } = "Calculating route plans…";
    private readonly NavigationGrid _nav;
    private readonly EnemyMasterConfig _config;
    private readonly Vector2 _start;
    private readonly Vector2I[] _goals;
    private readonly float _radius;
    private readonly bool _refined, _offsets, _corridors, _diversity;
    private readonly int _count, _revision, _costRevision, _settings, _crowdRevision;
    private int _sample = -1, _goal;
    private float _bestCost = float.PositiveInfinity;
    private Vector2[] _best = Array.Empty<Vector2>();

    // =========================================================
    // Capture inputs without duplicating script resources or consuming gameplay assignment streams.
    public RoutePreviewJob(NavigationGrid nav, EnemyDefinition enemy, Vector2 start, int count, bool refined)
    {
        _nav = nav; _config = nav.Config; nav.Refresh(); _start = start; _radius = enemy.RadiusPixels;
        _refined = refined; _offsets = enemy.AllowPathOffsets; _corridors = enemy.AllowCorridorVariation;
        _diversity = enemy.AllowPathDiversity; _count = Mathf.Clamp(count, 1, 12);
        _goals = nav.Goals.ToArray(); _revision = nav.Revision; _costRevision = nav.CostRevision;
        _settings = SettingsHash(_config); _crowdRevision = nav.CongestionRevision;
        if (!float.IsFinite(_radius) || _radius <= 0 || _radius >= nav.Grid.Config.CellSize * 0.5f)
        { Cancel("Enemy radius does not fit the shared navigation model."); return; }
        if (_goals.Length == 0 || !nav.HasClearance(start, _radius)) Cancel("No valid Citadel goals or portal clearance.");
    }

    // =========================================================
    // Separate debug count/time limits; one native query plus refinement is an indivisible soft-budget item.
    public void Step(int maxCalculations = 1, double maxMilliseconds = 1)
    {
        LastCalculations = 0;
        if (Complete) return;
        if (_nav.Revision != _revision || _nav.CostRevision != _costRevision || SettingsHash(_config) != _settings
            || (_config.CongestionInfluence > 0 && _nav.CongestionRevision != _crowdRevision))
        { Cancel("Inputs changed during calculation. Simulate Paths again."); return; }
        long started = Stopwatch.GetTimestamp();
        while (!Complete && LastCalculations < Mathf.Clamp(maxCalculations, 1, 8))
        {
            int profile = _sample < 0 || !_diversity || !_config.EnablePathDiversity ? NavigationGrid.NeutralProfile
                : _sample % Mathf.Clamp(_config.RouteProfileCount, 1, 8);
            Vector2[] route = _nav.FindPreviewPath(_start, _goals[_goal++], profile, out float cost);
            LastCalculations++; TotalCalculations++;
            if (cost < _bestCost && Safe(route)) { _best = route; _bestCost = cost; }
            if (_goal == _goals.Length)
            {
                FinishSample(profile); _goal = 0; _best = Array.Empty<Vector2>(); _bestCost = float.PositiveInfinity;
                if (++_sample == _count) { Complete = true; Message = $"{Results.Count}/{_count} routes; {Failed} failed. Plans, not traffic predictions."; }
            }
            if (maxMilliseconds > 0 && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= maxMilliseconds) break;
        }
    }

    // =========================================================
    // Refine only through production builders, using stable preview-only seed/offset samples.
    private void FinishSample(int profile)
    {
        if (_sample < 0) { Neutral = _best; NeutralLength = Length(_best); return; }
        Vector2[] points = _best;
        if (points.Length > 0 && _refined)
        {
            // Initial live route acceptance also prepends an unsnapped spawn position before refinement.
            if (_start.DistanceSquaredTo(points[0]) > 0.0001f)
            {
                Vector2[] connected = new Vector2[points.Length + 1]; connected[0] = _start;
                Array.Copy(points, 0, connected, 1, points.Length); points = connected;
            }
            uint seed = CorridorPreference.Seed((uint)_sample, _config.RoutePreferenceSeed);
            float offset = _offsets && _config.EnablePathOffsets
                ? ((CorridorPreference.Hash(seed ^ 0x1781u) & 65535u) / 65535f * 2 - 1) * Mathf.Max(0, _config.PathOffsetMaxPixels) : 0;
            EnemyRoute route = new();
            if (_corridors && CorridorPreference.Selected(seed, _config) && _config.CorridorVariationStrength > 0)
                route.BuildCorridor(points, _start, _nav, _radius, offset, seed, _config);
            else route.Build(points, _start, _nav, _radius, offset);
            points = route.Points;
        }
        if (!Safe(points)) { Failed++; return; }
        int duplicate = Results.FindIndex(r => r.Points.SequenceEqual(points));
        Results.Add(new(_sample + 1, profile, points, Length(points), duplicate < 0 ? 0 : Results[duplicate].Sample));
    }

    // =========================================================
    // Cell-center plans still need actual-radius swept and corner safety, including the SpawnExit connector.
    private bool Safe(Vector2[] points)
    {
        if (points.Length == 0) return false;
        Vector2 from = _start;
        foreach (Vector2 to in points)
        { if (!_nav.ConnectionClear(from, to, _radius)) return false; from = to; }
        return true;
    }
    // =========================================================
    // Physical distance includes the real portal position rather than an invented snapped start.
    private float Length(Vector2[] points)
    {
        float length = 0; Vector2 from = _start;
        foreach (Vector2 point in points) { length += from.DistanceTo(point); from = point; }
        return length;
    }
    // =========================================================
    // Clear partial output on cancellation so results from different inputs never mix.
    public void Cancel(string reason)
    { Results.Clear(); Neutral = Array.Empty<Vector2>(); NeutralLength = 0; Complete = true; Message = reason; }

    // =========================================================
    // Cheap explicit settings signature detects Inspector edits without reflection or shared resource mutation.
    public static int SettingsHash(EnemyMasterConfig c)
    {
        HashCode h = new();
        h.Add(c.EnablePathDiversity); h.Add(c.PathDiversityStrength); h.Add(c.RouteProfileCount); h.Add(c.PreferencePatchCells);
        h.Add(c.RoutePreferenceSeed); h.Add(c.CongestionInfluence); h.Add(c.EnablePathOffsets); h.Add(c.PathOffsetMaxPixels);
        h.Add(c.EnableCorridorVariation); h.Add(c.CorridorVariationPercent); h.Add(c.CorridorVariationStrength); h.Add(c.CorridorVariationSpacingCells);
        return h.ToHashCode();
    }
}
