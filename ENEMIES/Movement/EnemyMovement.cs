// Smooth waypoint movement with cached remaining distance; stale routes stop before moving.
using Godot;

public partial class EnemyMovement : Node
{
    public int RouteProfile { get; private set; }
    public Enemy Enemy { get; private set; }
    public float RemainingPathDistance => _paths != null && !_waiting && _revision == _paths.Navigation.Revision
        ? _remainingDistance : float.PositiveInfinity;
    private float _remainingDistance = float.PositiveInfinity;
    private PathRequestManager _paths;
    private Vector2[] _route = System.Array.Empty<Vector2>();
    private float[] _suffix = System.Array.Empty<float>();
    private int _next, _revision = -1;
    private bool _waiting;
    private float _retry;

    // =========================================================
    // Initialization is explicit, so preview/editor scenes do not run enemy logic.
    public override void _Ready() => SetPhysicsProcess(false);

    // =========================================================
    // Request once on spawn, then reuse the assigned route.
    public void Initialize(Enemy enemy, PathRequestManager paths)
    {
        Enemy = enemy; _paths = paths; RouteProfile = paths.AssignProfile(); SetPhysicsProcess(true); RequestRoute();
    }

    // =========================================================
    // A changed map invalidates movement and targeting distance immediately.
    private void RequestRoute()
    {
        _waiting = true; _remainingDistance = float.PositiveInfinity;
        _paths.RequestPath(this);
    }

    // =========================================================
    // Precompute suffix distances so target priorities need no repeated path sums.
    public void AcceptPath(Vector2[] route, int revision)
    {
        _waiting = false; _revision = revision; _route = route; _next = 0;
        _retry = Mathf.Max(0.1f, _paths.Config.FailedPathRetrySeconds);
        _suffix = new float[route.Length];
        for (int i = route.Length - 2; i >= 0; i--)
            _suffix[i] = _suffix[i + 1] + route[i].DistanceTo(route[i + 1]);
        UpdateDistance();
    }

    // =========================================================
    // Consume travel across waypoints, checking every swept segment before committing it.
    public override void _PhysicsProcess(double delta)
    {
        if (!Enemy.IsAlive) return;
        if (_revision != _paths.Navigation.Revision && !_waiting) RequestRoute();
        if (_waiting) return;
        if (_route.Length == 0)
        {
            _retry -= (float)delta;
            if (_retry <= 0f) RequestRoute();
            return;
        }
        float travel = Mathf.Max(0f, Enemy.Definition.MoveSpeed) * (float)delta;
        while (_next < _route.Length)
        {
            Vector2 target = _route[_next];
            float distance = Enemy.GlobalPosition.DistanceTo(target);
            if (distance <= 0.01f) { _next++; continue; }
            if (travel <= 0f) break;
            Vector2 nextPosition = Enemy.GlobalPosition.MoveToward(target, travel);
            if (!_paths.Navigation.SegmentClear(Enemy.GlobalPosition, nextPosition, Enemy.Definition.RadiusPixels))
            {
                _route = System.Array.Empty<Vector2>(); _remainingDistance = float.PositiveInfinity;
                _retry = Mathf.Max(0.1f, _paths.Config.FailedPathRetrySeconds); return;
            }
            Vector2 direction = target - Enemy.GlobalPosition;
            Enemy.Rotation = Mathf.LerpAngle(Enemy.Rotation, direction.Angle(),
                1f - Mathf.Exp(-Mathf.Max(0f, Enemy.Definition.TurnSpeed) * (float)delta));
            Enemy.GlobalPosition = nextPosition; travel -= distance;
            if (nextPosition == target) _next++; else break;
        }
        UpdateDistance();
        if (_next >= _route.Length) Enemy.Arrive();
    }

    // =========================================================
    // Infinity excludes ships without a valid current route from First targeting.
    private void UpdateDistance() => _remainingDistance = _next < _route.Length
        ? Enemy.GlobalPosition.DistanceTo(_route[_next]) + _suffix[_next] : float.PositiveInfinity;
}
