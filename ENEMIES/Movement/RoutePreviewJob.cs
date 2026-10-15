// Bounded debug-only route planning over shared navigation; no spawn counters, live routes or gameplay RNG.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public sealed class RoutePreviewJob
{
    public sealed record Result(int Sample, int Profile, Vector2[] Points, float Length, int DuplicateOf, Vector2? CheckpointEntry = null);
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
    private readonly RouteCheckpoint _checkpoint;
    private readonly bool _required;
    private readonly int _checkpointStamp;
    private readonly Vector2[] _checkpointTargets;
    private bool _searchingCheckpoint, _checkpointFailed;
    private Vector2 _legStart;
    private Vector2[] _firstLeg = Array.Empty<Vector2>();
    public RouteCheckpoint Checkpoint => _checkpoint;
    public bool RequiresCheckpoint => _required;
    private int _sample = -1, _goal;
    private float _bestCost = float.PositiveInfinity;
    private Vector2[] _best = Array.Empty<Vector2>();

    // =========================================================
    // Capture inputs without duplicating script resources or consuming gameplay assignment streams.
    public RoutePreviewJob(NavigationGrid nav, EnemyDefinition enemy, Vector2 start, int count, bool refined, RouteCheckpoint checkpoint = null, bool requiresCheckpoint = false)
    {
        _checkpoint = checkpoint; _required = requiresCheckpoint || checkpoint != null;
        _checkpointStamp = GodotObject.IsInstanceValid(checkpoint) ? checkpoint.GeometryStamp : 0;
        _nav = nav; _config = nav.Config; nav.Refresh(); _start = start; _radius = enemy.RadiusPixels;
        _refined = refined; _offsets = enemy.AllowPathOffsets; _corridors = enemy.AllowCorridorVariation;
        _diversity = enemy.AllowPathDiversity; _count = Mathf.Clamp(count, 1, 50);
        _goals = nav.Goals.ToArray(); _revision = nav.Revision; _costRevision = nav.CostRevision;
        _settings = SettingsHash(_config); _crowdRevision = nav.CongestionRevision;
        if (!float.IsFinite(_radius) || _radius <= 0 || _radius >= nav.Grid.Config.CellSize * 0.5f)
        { Cancel("Enemy radius does not fit the shared navigation model."); return; }
        if (_goals.Length == 0 || !nav.HasClearance(start, _radius))
        { Cancel("No valid Citadel goals or portal clearance."); return; }
        if (_required)
        {
            if (!GodotObject.IsInstanceValid(checkpoint)) { Cancel("Required checkpoint link is missing/invalid."); return; }
            _checkpointTargets = checkpoint.Targets(nav, _radius);
            if (_checkpointTargets.Length == 0 && !checkpoint.Contains(start))
            { Cancel("Required checkpoint has no clearance-valid sampled targets. Move/enlarge the circle."); return; }
        }
        ResetStage();
    }

    // =========================================================
    // Separate debug count/time limits; one native query plus refinement is an indivisible soft-budget item.
    public void Step(int maxCalculations = 1, double maxMilliseconds = 1)
    {
        LastCalculations = 0;
        if (Complete) return;
        if (_required && (!GodotObject.IsInstanceValid(_checkpoint) || _checkpoint.GeometryStamp != _checkpointStamp))
        { Cancel("Checkpoint changed. Simulate Paths again."); return; }
        if (_nav.Revision != _revision || _nav.CostRevision != _costRevision || SettingsHash(_config) != _settings
            || (_config.CongestionInfluence > 0 && _nav.CongestionRevision != _crowdRevision))
        { Cancel("Inputs changed during calculation. Simulate Paths again."); return; }
        long started = Stopwatch.GetTimestamp();
        while (!Complete && LastCalculations < Mathf.Clamp(maxCalculations, 1, 8))
        {
            int profile = _sample < 0 || !_diversity || !_config.EnablePathDiversity ? NavigationGrid.NeutralProfile
                : _sample % Mathf.Clamp(_config.RouteProfileCount, 1, 16);
            int targets = _searchingCheckpoint ? _checkpointTargets.Length : _goals.Length;
            Vector2[] route = _searchingCheckpoint
                ? CheckpointRoute.Find(_nav, _legStart, _checkpointTargets[_goal++], profile, _radius, out float cost, true)
                : _nav.FindPreviewPath(_legStart, _goals[_goal++], profile, out cost);
            LastCalculations++; TotalCalculations++;
            if (cost < _bestCost && Safe(route, _legStart)) { _best = route; _bestCost = cost; }
            if (_goal == targets)
            {
                if (_searchingCheckpoint && _best.Length > 0)
                {
                    _firstLeg = CheckpointRoute.ToEntry(_checkpoint, _start, Refine(_best, _start));
                    _legStart = _firstLeg[^1]; _searchingCheckpoint = false;
                }
                else
                {
                    if (_searchingCheckpoint && _best.Length == 0) _checkpointFailed = true;
                    FinishSample(profile);
                    if (++_sample == _count)
                    {
                        Complete = true;
                        Message = _checkpointFailed ? $"Required checkpoint unreachable for some samples: {Results.Count}/{_count} routes; {Failed} failed."
                            : $"{Results.Count}/{_count} routes; {Failed} failed. Plans, not traffic predictions.";
                    }
                    ResetStage();
                }
                _goal = 0; _best = Array.Empty<Vector2>(); _bestCost = float.PositiveInfinity;
            }
            if (maxMilliseconds > 0 && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= maxMilliseconds) break;
        }
    }

    // =========================================================
    // Refine only through production builders, using stable preview-only seed/offset samples.
    private void FinishSample(int profile)
    {
        Vector2[] points = _best;
        if (points.Length > 0)
        {
            points = Refine(points, _legStart);
            if (_firstLeg.Length > 0) points = _firstLeg.Concat(points).ToArray();
        }
        if (_sample < 0) { Neutral = points; NeutralLength = Length(points); return; }
        if (!Safe(points, _start)) { Failed++; return; }
        int duplicate = Results.FindIndex(r => r.Points.SequenceEqual(points));
        Results.Add(new(_sample + 1, profile, points, Length(points), duplicate < 0 ? 0 : Results[duplicate].Sample,
            _firstLeg.Length > 0 ? _firstLeg[^1] : _required ? _start : null));
    }

    // =========================================================
    // Every sample uses the same deterministic preferences for both legs; neutral never refines.
    private Vector2[] Refine(Vector2[] points, Vector2 origin)
    {
        if (_sample < 0 || !_refined || points.Length == 0) return points;
        if (origin.DistanceSquaredTo(points[0]) > 0.0001f)
        {
            Vector2[] connected = new Vector2[points.Length + 1]; connected[0] = origin;
            Array.Copy(points, 0, connected, 1, points.Length); points = connected;
        }
        uint seed = CorridorPreference.Seed((uint)_sample, _config.RoutePreferenceSeed);
        float offset = _offsets && _config.EnablePathOffsets
            ? ((CorridorPreference.Hash(seed ^ 0x1781u) & 65535u) / 65535f * 2 - 1) * Mathf.Max(0, _config.PathOffsetMaxPixels) : 0;
        Vector2 forward = _firstLeg.Length > 1 ? (_firstLeg[^1] - _firstLeg[^2]).Normalized() : Vector2.Zero;
        EnemyRoute route = new();
        if (_corridors && CorridorPreference.Selected(seed, _config) && _config.CorridorVariationStrength > 0)
            route.BuildCorridor(points, origin, _nav, _radius, offset, seed, _config, forward);
        else route.Build(points, origin, _nav, _radius, offset, forward);
        return route.Points;
    }

    // =========================================================
    // A checkpoint already containing the actual spawn needs no first leg, just like a real initialized ship.
    private void ResetStage()
    {
        _legStart = _start; _firstLeg = Array.Empty<Vector2>();
        _searchingCheckpoint = _required && GodotObject.IsInstanceValid(_checkpoint) && !_checkpoint.Contains(_start);
    }

    // =========================================================
    // Cell-center plans still need actual-radius swept and corner safety, including the SpawnExit connector.
    private bool Safe(Vector2[] points, Vector2 origin)
    {
        if (points.Length == 0) return false;
        Vector2 from = origin;
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
