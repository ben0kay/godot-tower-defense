// Per-fighter patrol order and distance-based route follower.
using Godot;
using System;

public partial class FighterDrone
{
    internal FighterPatrolRoute PatrolRoute { get; private set; }
    internal bool IsPatrolling { get; private set; }
    private bool _patrolJoined, _patrolSeeking;
    private float _patrolDistance;

    internal void SetPatrolMode(bool patrol, FighterPatrolRoute replacement, FighterDroneTargeting targeting)
    {
        if (replacement != null) PatrolRoute = replacement;
        if (patrol == IsPatrolling && replacement == null) return;
        IsPatrolling = patrol;
        _patrolJoined = _patrolSeeking = false;
        ClearTarget(targeting);
        _steering.ResetPass();
        _search = 0;
    }

    // =========================================================
    // Follow the circuit without borrowing Auto-pilot's movement target. Guns acquire independently.
    internal bool StepPatrol(float dt, bool hasEnemies, Func<Vector2, Vector2> clamp)
    {
        if (!IsFlying || InTransit || PatrolRoute == null) return false;
        Vector2 position = GlobalPosition;
        float speed = Mathf.Min(FighterDroneConfig.Safe(Config.PatrolSpeed, 145, 32, 600),
            FighterDroneConfig.Safe(Config.FlightSpeed, 300, 32, 2000));
        float tolerance = FighterDroneConfig.Safe(Config.PatrolArrivalTolerance, 48, 16, 160);
        float leash = Mathf.Max(tolerance, FighterDroneConfig.Safe(Config.PatrolLeash, 110, 48, 300));
        if (!_patrolJoined && !_patrolSeeking)
        {
            _patrolDistance = PatrolRoute.NearestDistance(position);
            _patrolSeeking = true;
        }

        Vector2 routePoint = PatrolRoute.PointAt(_patrolDistance);
        float deviation = routePoint.DistanceTo(position);
        if (deviation <= tolerance) _patrolJoined = true;
        bool following = _patrolJoined && deviation <= leash;
        Vector2 destination = routePoint;
        if (following)
        {
            float lookAhead = Mathf.Clamp(speed * .35f, 22, 75);
            Vector2 previous = PatrolRoute.PointAt(_patrolDistance - lookAhead);
            Vector2 next = PatrolRoute.PointAt(_patrolDistance + lookAhead);
            float turn = Mathf.Abs((routePoint - previous).AngleTo(next - routePoint));
            speed /= 1 + turn * .65f;
            _patrolDistance = Mathf.PosMod(_patrolDistance + speed * dt, PatrolRoute.Length);
            destination = PatrolRoute.PointAt(_patrolDistance + Mathf.Clamp(speed * .35f, 22, 75));
        }
        FighterDroneState before = State;
        State = FighterDroneState.Holding;
        _steering.Steer(this, clamp(destination), speed, following, dt, clamp, patrol: true);
        Present(dt, true);
        bool fired = _weapon?.Step(dt, hasEnemies) == true;
        return fired || before != State;
    }
}
