// Fighter-jet attack runs: stage, cross the target, extend and alternate broad return lobes into a figure-eight.
using Godot;
internal sealed class FighterDroneMovement
{
    internal Vector2 Velocity;
    internal float Heading = -Mathf.Pi / 2, Phase;
    private Enemy _passTarget;
    private int _generation, _stage;
    private Vector2 _axis = Vector2.Up;
    private float _side = 1, _stageSeconds;
    internal bool OnAttackRun => _passTarget != null && _stage == 1;

    // =========================================================
    // Retained target-relative waypoints make complete passes instead of chasing an orbit point beside the ship.
    internal void Step(FighterDrone fighter, Enemy target, Vector2 home, float dt, System.Func<Vector2, Vector2> clamp)
    {
        var config = fighter.Config;
        float speed = FighterDroneConfig.Safe(config.FlightSpeed, 340, 32, 2000);
        float radius = FighterDroneConfig.Safe(config.OrbitRadius, 150, 24, 600);
        float manoeuvreSpeed = FighterDroneConfig.Safe(config.ManoeuvreSpeed, 260, 24, 1000);
        Phase = Mathf.PosMod(Phase + dt * manoeuvreSpeed / radius, Mathf.Tau * 3);
        Vector2 destination; float cruise = speed;
        if (target == null)
        {
            _passTarget = null; _stage = 0;
            destination = home + new Vector2(Mathf.Cos(Phase), Mathf.Sin(Phase)) * radius * .65f;
            cruise = manoeuvreSpeed * .55f;
        }
        else
        {
            Vector2 center = target.GlobalPosition;
            if (_passTarget != target || _generation != target.SpawnGeneration)
            {
                _passTarget = target; _generation = target.SpawnGeneration; _stage = 0; _stageSeconds = 0;
                _axis = (center - fighter.GlobalPosition).Normalized();
                if (_axis.LengthSquared() < .1f) _axis = Vector2.FromAngle(Heading);
                _side = fighter.Slot % 2 == 0 ? 1 : -1;
            }
            float runUp = FighterDroneConfig.Safe(config.AttackDistance, 220, 32, 1200);
            float exit = FighterDroneConfig.Safe(config.PassExitDistance, 220, 80, 800);
            float tolerance = Mathf.Max(28, radius * .28f);
            float along = (fighter.GlobalPosition - center).Dot(_axis);
            Vector2 lateral = _axis.Orthogonal() * _side;
            Vector2 staging = clamp(center - _axis * runUp);
            _stageSeconds += dt;
            // Timeout advances a blocked/clamped return leg rather than circling forever on tiny maps.
            if (_stage == 0 && (fighter.GlobalPosition.DistanceTo(staging) < tolerance || _stageSeconds > 7))
                Advance(1);
            if (_stage == 1 && along >= -24) Advance(2);
            if (_stage == 2 && (along >= exit * .8f || _stageSeconds > 3)) Advance(3);
            if (_stage == 3 && (fighter.GlobalPosition.DistanceTo(clamp(center - _axis * (runUp + radius) + lateral * radius)) < tolerance || _stageSeconds > 5))
            { _side = -_side; Advance(0); }

            if (_stage == 1)
            {
                float bulletSpeed = FighterDroneConfig.Safe(config.BulletSpeed, 1200, 40, 3000);
                float lead = Mathf.Min(fighter.GlobalPosition.DistanceTo(center) / bulletSpeed,
                    FighterDroneConfig.Safe(config.InterceptLeadSeconds, .8f, 0, 3));
                destination = center + Vector2.FromAngle(target.Rotation) * target.EffectiveMoveSpeed * lead;
            }
            else if (_stage == 2)
                destination = center + _axis * exit + lateral * radius * .35f;
            else if (_stage == 3)
            {
                destination = center - _axis * runUp + lateral * radius;
                cruise = manoeuvreSpeed;
            }
            else destination = staging;
        }
        destination = clamp(destination);
        Vector2 offset = destination - fighter.GlobalPosition;
        float braking = FighterDroneConfig.Safe(config.Deceleration, 1000, 32, 10000);
        // Maintain forward speed through the firing pass and pull-away; only guard/staging can slow on arrival.
        bool passing = target != null && (_stage == 1 || _stage == 2 || _stage == 3);
        float desiredSpeed = passing ? cruise : Mathf.Min(cruise, Mathf.Sqrt(2 * braking * offset.Length()));
        float desiredHeading = offset.LengthSquared() > .01f ? offset.Angle() : Heading;
        float turn = Mathf.DegToRad(FighterDroneConfig.Safe(config.TurnDegreesPerSecond, 150, 30, 720)) * dt;
        Heading = Mathf.Wrap(Heading + Mathf.Clamp(Mathf.AngleDifference(Heading, desiredHeading), -turn, turn), -Mathf.Pi, Mathf.Pi);
        Vector2 desiredVelocity = Vector2.FromAngle(Heading) * desiredSpeed;
        float rate = desiredSpeed < Velocity.Length() ? braking : FighterDroneConfig.Safe(config.Acceleration, 1000, 32, 10000);
        Velocity = Velocity.MoveToward(desiredVelocity, rate * dt);
        Vector2 position = fighter.GlobalPosition + Velocity * dt, bounded = clamp(position);
        if (bounded != position) Velocity = (bounded - fighter.GlobalPosition) / dt;
        fighter.GlobalPosition = bounded;
    }
    internal void ResetPass() { _passTarget = null; _stage = 0; _stageSeconds = 0; }
    private void Advance(int stage) { _stage = stage; _stageSeconds = 0; }
}
