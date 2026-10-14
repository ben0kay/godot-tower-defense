// Smooth cached movement with stable offsets, selective invalidation and staggered route audits.
using Godot;
using System.Collections.Generic;

public partial class EnemyMovement : Node
{
    #region State
    public int RouteProfile { get; private set; }
    public Enemy Enemy { get; private set; }
    public float RemainingPathDistance => _waiting ? float.PositiveInfinity : _remainingDistance;
    public float LateralOffsetPixels { get; private set; }
    public IReadOnlyDictionary<Vector2I, List<int>> RouteDependencies => _route.Dependencies;
    public int NextWaypoint => _next;
    public Vector2[] Waypoints => _route.Points;
    public bool IsWaitingForPath => _waiting;
    private float _remainingDistance = float.PositiveInfinity, _retry, _auditTimer;
    private PathRequestManager _paths;
    private readonly EnemyRoute _route = new();
    private readonly HashSet<Vector2I> _dirtyCells = new();
    private int _next;
    private bool _waiting, _fullAudit;
    #endregion

    #region Lifecycle and Routes
    // =========================================================
    // Initialization remains explicit so isolated art scenes do not simulate enemies.
    public override void _Ready() => SetPhysicsProcess(false);

    // =========================================================
    // Stable per-spawn lateral preference; independent of weighted routing profile.
    public void Initialize(Enemy enemy, PathRequestManager paths)
    {
        Enemy = enemy; _paths = paths; RouteProfile = paths.AssignProfile();
        uint seed = unchecked((uint)GetInstanceId() * 2654435761u); seed ^= seed >> 16;
        LateralOffsetPixels = ((seed & 65535u) / 65535f * 2f - 1f) * Mathf.Max(0f, paths.Config.PathOffsetMaxPixels);
        _auditTimer = (seed % 997) / 997f * Mathf.Max(0f, paths.Config.RouteAuditIntervalSeconds);
        SetPhysicsProcess(true); RequestRoute(float.PositiveInfinity);
    }

    // =========================================================
    // Only confirmed unsafe routes stop. Other ships never react to a global revision alone.
    private void RequestRoute(float distance)
    {
        _waiting = true; _remainingDistance = float.PositiveInfinity;
        _paths.RequestPath(this, distance);
    }

    // =========================================================
    // A* assignment builds safe offset waypoints once and updates shared dependency indexing.
    public void AcceptPath(Vector2[] route, int revision)
    {
        _paths.UnregisterRoute(this);
        _waiting = false; _next = 0; _dirtyCells.Clear(); _fullAudit = false;
        _retry = Mathf.Max(0.1f, _paths.Config.FailedPathRetrySeconds);
        float offset = _paths.Config.EnablePathOffsets ? Mathf.Clamp(LateralOffsetPixels,
            -Mathf.Max(0f, _paths.Config.PathOffsetMaxPixels), Mathf.Max(0f, _paths.Config.PathOffsetMaxPixels)) : 0f;
        _route.Build(route, Enemy.GlobalPosition, _paths.Navigation, Enemy.Definition.RadiusPixels, offset);
        _paths.RegisterRoute(this); UpdateDistance();
    }

    // =========================================================
    // Remove dependency references when enemies die, arrive or the level unloads.
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(_paths)) _paths.UnregisterRoute(this); }
    #endregion

    #region Validation
    // =========================================================
    // Change notifications only mark relevant remaining segments; expensive checks are queued.
    public void MarkChangedCell(Vector2I? cell)
    {
        if (_waiting || _route.Points.Length == 0) return;
        if (cell == null) _fullAudit = true;
        else if (_route.UsesCell(cell.Value, _next)) _dirtyCells.Add(cell.Value);
        else return;
        _paths.RequestValidation(this);
    }

    // =========================================================
    // Audits use cached cell dependencies, with swept geometry only near actual blockers.
    public void ValidateRoute()
    {
        if (_waiting || !Enemy.IsAlive) { _dirtyCells.Clear(); _fullAudit = false; return; }
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
        float interval = _paths.Config.RouteAuditIntervalSeconds;
        if (interval > 0f && !_waiting)
        {
            _auditTimer -= (float)delta;
            if (_auditTimer <= 0f) { _auditTimer += interval; MarkChangedCell(null); }
        }
        if (_waiting) return;
        if (_route.Points.Length == 0)
        {
            _retry -= (float)delta;
            if (_retry <= 0f) RequestRoute(float.PositiveInfinity);
            return;
        }
        float travel = Mathf.Max(0f, Enemy.Definition.MoveSpeed) * (float)delta;
        while (_next < _route.Points.Length)
        {
            if (_route.CornerBlocked(_next, _paths.Navigation)) { RequestRoute(0f); return; }
            Vector2 target = _route.Points[_next]; float distance = Enemy.GlobalPosition.DistanceTo(target);
            if (distance <= 0.01f) { _next++; continue; }
            if (travel <= 0f) break;
            Vector2 nextPosition = Enemy.GlobalPosition.MoveToward(target, travel);
            if (!_paths.Navigation.SegmentClear(Enemy.GlobalPosition, nextPosition, Enemy.Definition.RadiusPixels))
            { RequestRoute(0f); return; }
            Vector2 direction = target - Enemy.GlobalPosition;
            Enemy.Rotation = Mathf.LerpAngle(Enemy.Rotation, direction.Angle(),
                1f - Mathf.Exp(-Mathf.Max(0f, Enemy.Definition.TurnSpeed) * (float)delta));
            Enemy.GlobalPosition = nextPosition; Enemy.UpdateDefenseBarTransform(); travel -= distance;
            if (nextPosition == target) _next++; else break;
        }
        UpdateDistance();
        if (_next >= _route.Points.Length) Enemy.Arrive();
    }

    // =========================================================
    // First targeting compares physical refined route distance, independent of weighted cost.
    private void UpdateDistance() => _remainingDistance = _next < _route.Points.Length
        ? Enemy.GlobalPosition.DistanceTo(_route.Points[_next]) + _route.Suffix[_next] : float.PositiveInfinity;
    #endregion
}
