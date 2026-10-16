// Smooth cached movement with stable offsets, selective invalidation and staggered route audits.
using Godot;
using System.Collections.Generic;

public partial class EnemyMovement : Node
{
    #region State
    public RouteCheckpoint Checkpoint { get; private set; }
    public bool RequiresCheckpoint { get; private set; }
    public bool CheckpointCompleted { get; private set; }
    public bool HeadingToCheckpoint => RequiresCheckpoint && !CheckpointCompleted;
    public int DestinationRevision => HeadingToCheckpoint
        ? System.HashCode.Combine(_destinationSerial, GodotObject.IsInstanceValid(Checkpoint) ? Checkpoint.GeometryStamp : int.MinValue) : 0;
    public string RouteStatus { get; private set; } = "Citadel";
    private int _checkpointStamp, _destinationSerial;
    public int CompletedCheckpointCount { get; private set; }
    private CheckpointNetwork _checkpointNetwork;
    private string _checkpointError = "";
    // =========================================================
    // Radius-aware target snapshots are shared by checkpoint geometry, never recomputed per enemy tick.
    public Vector2[] CheckpointTargets() => _checkpointError.Length == 0 && GodotObject.IsInstanceValid(Checkpoint)
        ? Checkpoint.Targets(_paths.Navigation, Enemy.Definition.RadiusPixels) : System.Array.Empty<Vector2>();

    public bool UsesCorridorVariation { get; private set; }
    public int CorridorSamplingChecks => _route.CorridorSamples;
    public bool CorridorVariationApplied => _route.CorridorVariationApplied;
    public uint CorridorSeed { get; private set; }
    public int RouteProfile => _allowDiversity ? _assignedProfile : NavigationGrid.NeutralProfile;
    private int _assignedProfile;
    private bool _allowOffsets, _allowDiversity;
    public Enemy Enemy { get; private set; }
    public float RemainingPathDistance => _replacementNeeded ? float.PositiveInfinity : _remainingDistance;
    public float LateralOffsetPixels { get; private set; }
    public IReadOnlyDictionary<Vector2I, List<int>> RouteDependencies => _route.Dependencies;
    public int NextWaypoint => _next;
    public Vector2[] Waypoints => _route.Points;
    public bool IsWaitingForPath => _waiting;
    public float PendingObstacleDistance => _blockedFrom < _route.Points.Length
        ? (_next <= _blockedFrom ? Mathf.Max(0f, _remainingDistance - _route.Suffix[_blockedFrom]) : 0f)
        : float.PositiveInfinity;
    private float _remainingDistance = float.PositiveInfinity, _retry, _auditTimer;
    private PathRequestManager _paths;
    private readonly EnemyRoute _route = new();
    private readonly HashSet<Vector2I> _dirtyCells = new();
    private int _next;
    private bool _waiting, _fullAudit, _stopped, _replacementNeeded;
    private int _blockedFrom = int.MaxValue;
    private Vector2 _travelDirection;
    #endregion

    #region Lifecycle and Routes
    // =========================================================
    // Initialization remains explicit so isolated art scenes do not simulate enemies.
    public override void _Ready() => SetPhysicsProcess(false);

    // =========================================================
    // Stable per-spawn lateral preference; independent of weighted routing profile.
    public void Initialize(Enemy enemy, PathRequestManager paths, RouteCheckpoint checkpoint = null, bool requiresCheckpoint = false, EnemyRouteHandoff handoff = null)
    {
        Enemy = enemy; _paths = paths;
        Checkpoint = checkpoint; RequiresCheckpoint = requiresCheckpoint || checkpoint != null;
        CheckpointCompleted = false; CompletedCheckpointCount = 0; _destinationSerial = 0;
        _checkpointNetwork = RequiresCheckpoint ? CheckpointNetwork.Capture(checkpoint) : null;
        _checkpointError = _checkpointNetwork?.Error ?? "";
        if (handoff != null)
        {
            Checkpoint = handoff.Checkpoint; RequiresCheckpoint = handoff.Required; CheckpointCompleted = handoff.Complete;
            CompletedCheckpointCount = handoff.CompletedCount; _checkpointNetwork = handoff.Network; _checkpointError = handoff.Error;
        }
        _checkpointStamp = DestinationRevision;
        RouteStatus = HeadingToCheckpoint ? "Routing through required checkpoints" : "Citadel"; _assignedProfile = paths.AssignProfile();
        _allowOffsets = enemy.Definition.AllowPathOffsets; _allowDiversity = enemy.Definition.AllowPathDiversity;
        CorridorSeed = paths.AssignCorridorSeed();
        UsesCorridorVariation = enemy.Definition.AllowCorridorVariation && CorridorPreference.Selected(CorridorSeed, paths.Config);
        uint seed = unchecked((uint)GetInstanceId() * 2654435761u); seed ^= seed >> 16;
        LateralOffsetPixels = ((seed & 65535u) / 65535f * 2f - 1f) * Mathf.Max(0f, paths.Config.PathOffsetMaxPixels);
        _auditTimer = (seed % 997) / 997f * Mathf.Max(0f, paths.Config.RouteAuditIntervalSeconds);
        SetPhysicsProcess(true);
        if (HeadingToCheckpoint && _checkpointError.Length == 0 && GodotObject.IsInstanceValid(Checkpoint)
            && Checkpoint.Contains(enemy.GlobalPosition) && paths.Navigation.HasClearance(enemy.GlobalPosition, enemy.Definition.RadiusPixels)) CompleteCheckpoint();
        else RequestRoute(float.PositiveInfinity);
    }

    // =========================================================
    // Capture only remaining destination obligations, never physical waypoints, movement permissions or path seeds.
    public EnemyRouteHandoff CaptureRouteHandoff() => new(Checkpoint, RequiresCheckpoint, CheckpointCompleted,
        CompletedCheckpointCount, _checkpointNetwork, _checkpointError);

    // =========================================================
    // Queue a replacement while retaining safe old movement; stop only on immediate danger.
    private void RequestRoute(float distance, bool stop = false)
    {
        _waiting = true; _replacementNeeded = true;
        _stopped |= stop || _route.Points.Length == 0;
        _paths.RequestPath(this, distance);
    }

    // =========================================================
    // A* assignment builds safe offset waypoints once and updates shared dependency indexing.
    public void AcceptPath(Vector2[] route, int revision)
    {
        _waiting = false;
        if (HeadingToCheckpoint && (_checkpointError.Length > 0 || !GodotObject.IsInstanceValid(Checkpoint) || route.Length > 0 && !Checkpoint.Contains(route[^1])))
            route = System.Array.Empty<Vector2>();
        RouteStatus = _checkpointError.Length > 0 ? _checkpointError : HeadingToCheckpoint ? (route.Length == 0 ? "Required checkpoint unavailable: waiting/retrying" : "Routing to required checkpoint") : "Citadel";
        _retry = Mathf.Max(0.1f, _paths.Config.FailedPathRetrySeconds);
        if (route.Length == 0)
        {
            // A failed replacement does not discard a still-safe old prefix.
            _replacementNeeded = true; _stopped |= _route.Points.Length == 0;
            return;
        }
        Vector2[] connected = ConnectRoute(route);
        if (connected == null)
        {
            if (!_paths.Navigation.HasClearance(Enemy.GlobalPosition, Enemy.Definition.RadiusPixels))
            { _stopped = true; _replacementNeeded = true; return; }
            RequestRoute(0f); return;
        }
        _paths.UnregisterRoute(this);
        _next = 0; _dirtyCells.Clear(); _fullAudit = false; _blockedFrom = int.MaxValue;
        _stopped = false; _replacementNeeded = false;
        float offset = _allowOffsets && _paths.Config.EnablePathOffsets ? Mathf.Clamp(LateralOffsetPixels,
            -Mathf.Max(0f, _paths.Config.PathOffsetMaxPixels), Mathf.Max(0f, _paths.Config.PathOffsetMaxPixels)) : 0f;
        if (UsesCorridorVariation && _paths.Config.EnableCorridorVariation && _paths.Config.CorridorVariationStrength > 0)
            _route.BuildCorridor(connected, Enemy.GlobalPosition, _paths.Navigation, Enemy.Definition.RadiusPixels,
                offset, CorridorSeed, _paths.Config, _travelDirection);
        else _route.Build(connected, Enemy.GlobalPosition, _paths.Navigation, Enemy.Definition.RadiusPixels, offset, _travelDirection);
        _paths.RegisterRoute(this); UpdateDistance();
    }

    // =========================================================
    // Only physical entry completes a stage. Drop the old chain before requesting the Citadel leg.
    private void CompleteCheckpoint()
    {
        CompletedCheckpointCount++;
        RouteCheckpoint next = _checkpointNetwork.Next(Checkpoint, CorridorSeed, CompletedCheckpointCount - 1, out string error);
        _checkpointError = error; CheckpointCompleted = next == null && error.Length == 0;
        if (!CheckpointCompleted) Checkpoint = next;
        _destinationSerial++; _checkpointStamp = DestinationRevision;
        RouteStatus = error.Length > 0 ? error : CheckpointCompleted ? "Checkpoint chain complete; routing to Citadel" : "Routing to next required checkpoint";
        _paths.UnregisterRoute(this);
        _route.Build(System.Array.Empty<Vector2>(), Enemy.GlobalPosition, _paths.Navigation, Enemy.Definition.RadiusPixels, 0);
        _next = 0; _blockedFrom = int.MaxValue; _dirtyCells.Clear(); _fullAudit = false; UpdateDistance();
        RequestRoute(0, true);
    }

    // =========================================================
    // Join near current progress, preferring a forward visible waypoint over a cell-center U-turn.
    private Vector2[] ConnectRoute(Vector2[] route)
    {
        Vector2 position = Enemy.GlobalPosition;
        int nearest = 0; float distance = float.PositiveInfinity;
        for (int i = 0; i < route.Length; i++)
        {
            float candidate = position.DistanceSquaredTo(route[i]);
            if (candidate < distance) { nearest = i; distance = candidate; }
        }
        int end = Mathf.Min(route.Length - 1, nearest + 6), join = -1;
        Vector2? bend = null;
        for (int pass = 0; pass < 2 && join < 0; pass++)
            for (int i = nearest; i <= end; i++)
            {
                Vector2 direction = route[i] - position;
                if (pass == 0 && _travelDirection != Vector2.Zero && direction.Dot(_travelDirection) < -0.01f) continue;
                if (RouteConnectionClear(position, route[i], Enemy.Definition.RadiusPixels))
                { join = i; break; }
                // A clearance-tested elbow can turn around a nearby wall without retreating
                // to the cell center. Try keeping either current coordinate for the first leg.
                for (int axis = 0; axis < 2; axis++)
                {
                    Vector2 corner = axis == 0 ? new Vector2(position.X, route[i].Y) : new Vector2(route[i].X, position.Y);
                    if (corner.DistanceSquaredTo(position) < 0.0001f || corner.DistanceSquaredTo(route[i]) < 0.0001f) continue;
                    if (pass == 0 && _travelDirection != Vector2.Zero && (corner - position).Dot(_travelDirection) < -0.01f) continue;
                    if (!RouteConnectionClear(position, corner, Enemy.Definition.RadiusPixels)
                        || !RouteConnectionClear(corner, route[i], Enemy.Definition.RadiusPixels)) continue;
                    bend = corner; join = i; break;
                }
                if (join >= 0) break;
            }
        if (join < 0) return null;
        bool insertPosition = position.DistanceSquaredTo(route[join]) > 0.0001f;
        int prefix = (insertPosition ? 1 : 0) + (bend.HasValue ? 1 : 0);
        Vector2[] connected = new Vector2[route.Length - join + prefix];
        int offset = prefix;
        if (insertPosition) connected[0] = position;
        if (bend.HasValue) connected[prefix - 1] = bend.Value;
        System.Array.Copy(route, join, connected, offset, route.Length - join);
        return connected;
    }

    // =========================================================
    // Only active selected N4 routes need the analytic grazing-corner supplement; old behavior is unchanged.
    private bool RouteConnectionClear(Vector2 from, Vector2 to, float radius) => UsesCorridorVariation &&
        _paths.Config.EnableCorridorVariation && _paths.Config.CorridorVariationStrength > 0
        ? CorridorClearance.ConnectionClear(_paths.Navigation, from, to, radius)
        : _paths.Navigation.ConnectionClear(from, to, radius);

    // =========================================================
    // Remove dependency references when enemies die, arrive or the level unloads.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_paths)) _paths.UnregisterRoute(this); Checkpoint = null; _checkpointNetwork = null; }
    #endregion

    #region Validation
    // =========================================================
    // Change notifications only mark relevant remaining segments; expensive checks are queued.
    public void MarkChangedCell(Vector2I? cell)
    {
        if (_route.Points.Length == 0) return;
        if (cell == null) _fullAudit = true;
        else if (_route.UsesCell(cell.Value, _next)) _dirtyCells.Add(cell.Value);
        else return;
        _paths.RequestValidation(this);
    }

    // =========================================================
    // Audits use cached cell dependencies, with swept geometry only near actual blockers.
    public void ValidateRoute()
    {
        if (!Enemy.IsAlive) { _dirtyCells.Clear(); _fullAudit = false; return; }
        NavigationGrid nav = _paths.Navigation;
        int blocked = -1;
        if (_fullAudit)
        {
            foreach (var dependency in _route.Dependencies)
                if (dependency.Value[^1] >= _next && nav.Grid.BlocksEnemies(dependency.Key))
                {
                    blocked = _route.BlockedSegment(dependency.Key, _next, Enemy.GlobalPosition, nav, Enemy.Definition.RadiusPixels);
                    if (blocked >= 0) break;
                }
        }
        else foreach (Vector2I cell in _dirtyCells)
        {
            blocked = _route.BlockedSegment(cell, _next, Enemy.GlobalPosition, nav, Enemy.Definition.RadiusPixels);
            if (blocked >= 0) break;
        }
        _dirtyCells.Clear(); _fullAudit = false;
        if (blocked >= 0)
        {
            float distance = Enemy.GlobalPosition.DistanceTo(_route.Points[_next]);
            for (int i = _next; i < blocked; i++) distance += _route.Points[i].DistanceTo(_route.Points[i + 1]);
            _blockedFrom = Mathf.Min(_blockedFrom, blocked);
            RequestRoute(distance);
        }
    }
    #endregion

    #region Movement
    // =========================================================
    // Check immediate swept clearance every tick; audits never trigger A* on valid routes.
    public override void _PhysicsProcess(double delta)
    {
        if (!Enemy.IsAlive) return;
        if (HeadingToCheckpoint)
        {
            if (_checkpointError.Length == 0 && GodotObject.IsInstanceValid(Checkpoint) && Checkpoint.Contains(Enemy.GlobalPosition)
                && _paths.Navigation.HasClearance(Enemy.GlobalPosition, Enemy.Definition.RadiusPixels))
            { CompleteCheckpoint(); return; }
            if (_checkpointStamp != DestinationRevision)
            { _checkpointStamp = DestinationRevision; RequestRoute(0, true); }
        }
        float interval = _paths.Config.RouteAuditIntervalSeconds;
        if (interval > 0f && !_waiting)
        {
            _auditTimer -= (float)delta;
            if (_auditTimer <= 0f) { _auditTimer += interval; MarkChangedCell(null); }
        }
        if (_replacementNeeded && !_waiting)
        {
            _retry -= (float)delta;
            if (_retry <= 0f) RequestRoute(_stopped ? 0f : RemainingPathDistance);
        }
        if (_stopped) return;
        if (_route.Points.Length == 0)
        {
            _retry -= (float)delta;
            if (_retry <= 0f) RequestRoute(float.PositiveInfinity);
            return;
        }
        float speed = Mathf.Max(0f, Enemy.Definition.MoveSpeed);
        float travel = speed * (float)delta;
        double terrainElapsed = 0;
        while (_next < _route.Points.Length)
        {
            if (_route.CornerBlocked(_next, _paths.Navigation))
            { RequestRoute(0f, true); return; }
            Vector2 target = _route.Points[_next]; float distance = Enemy.GlobalPosition.DistanceTo(target);
            if (distance <= 0.01f) { _next++; continue; }
            if (travel <= 0f) break;
            Vector2 nextPosition = Enemy.GlobalPosition.MoveToward(target, travel);
            if (!_paths.Navigation.SegmentClear(Enemy.GlobalPosition, nextPosition, Enemy.Definition.RadiusPixels))
            { RequestRoute(0f, true); return; }
            float entry = 0;
            bool reachesCheckpoint = HeadingToCheckpoint && _checkpointError.Length == 0 && GodotObject.IsInstanceValid(Checkpoint)
                && Checkpoint.Entry(Enemy.GlobalPosition, nextPosition, out entry);
            if (reachesCheckpoint) nextPosition = Enemy.GlobalPosition.Lerp(nextPosition, entry);
            Vector2 direction = target - Enemy.GlobalPosition;
            _travelDirection = direction.Normalized();
            Enemy.Rotation = Mathf.LerpAngle(Enemy.Rotation, direction.Angle(),
                1f - Mathf.Exp(-Mathf.Max(0f, Enemy.Definition.TurnSpeed) * (float)delta));
            Vector2 previous = Enemy.GlobalPosition;
            double segmentSeconds = speed > 0 ? previous.DistanceTo(nextPosition) / speed : 0;
            if (GodotObject.IsInstanceValid(Enemy.Terrain) && Enemy.Terrain.LavaCellCount > 0
                && !Enemy.Terrain.Traverse(Enemy, previous, nextPosition, segmentSeconds, terrainElapsed)) return;
            terrainElapsed += segmentSeconds;
            Enemy.SetMovementPosition(nextPosition); Enemy.Registry.UpdatePosition(Enemy); Enemy.UpdateDefenseBarTransform(); travel -= distance;
            if (reachesCheckpoint) { CompleteCheckpoint(); return; }
            if (nextPosition == target) _next++; else break;
        }
        UpdateDistance();
        if (_next >= _route.Points.Length)
        {
            if (HeadingToCheckpoint) RequestRoute(0, true);
            else Enemy.Arrive();
        }
    }

    // =========================================================
    // First targeting compares physical refined route distance, independent of weighted cost.
    private void UpdateDistance() => _remainingDistance = _next < _route.Points.Length
        ? Enemy.GlobalPosition.DistanceTo(_route.Points[_next]) + _route.Suffix[_next] : float.PositiveInfinity;
    #endregion
}
