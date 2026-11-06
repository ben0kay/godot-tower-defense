// Continuous airborne steering and target-relative loops; shared finite flight still owns launch/dock legs.
using Godot;
internal sealed class FighterDroneMovement
{
    internal Vector2 Velocity;
    internal float Heading = -Mathf.Pi / 2, Phase;
    internal void Step(FighterDrone fighter, Enemy target, Vector2 home, float dt, System.Func<Vector2, Vector2> clamp)
    {
        FighterDroneConfig config = fighter.Config;
        float speed = FighterDroneConfig.Safe(config.FlightSpeed, 340, 32, 2000);
        float radius = FighterDroneConfig.Safe(config.OrbitRadius, 100, 24, 600);
        float manoeuvreSpeed = FighterDroneConfig.Safe(config.ManoeuvreSpeed, 170, 24, 1000);
        Phase = Mathf.PosMod(Phase + dt * manoeuvreSpeed / radius, Mathf.Tau * 3);
        Vector2 destination;
        float cruise = speed;
        if (target == null)
        {
            destination = home + new Vector2(Mathf.Cos(Phase), Mathf.Sin(Phase)) * radius * .65f;
            cruise = manoeuvreSpeed * .55f;
        }
        else
        {
            Vector2 center = target.GlobalPosition;
            Vector2 forward = Vector2.FromAngle(target.Rotation);
            float distance = fighter.GlobalPosition.DistanceTo(center);
            float standOff = FighterDroneConfig.Safe(config.AttackDistance, 160, 32, 1200);
            float lead = Mathf.Min(distance / speed, FighterDroneConfig.Safe(config.InterceptLeadSeconds, .8f, 0, 3));
            if (distance > standOff + radius * 1.8f)
                destination = center + forward * target.EffectiveMoveSpeed * lead - forward * standOff;
            else
            {
                // Alternate broad figure-eight and round orbit passes; stand-off retreat produces a kite on close approaches.
                float side = (fighter.Slot % 2 == 0 ? 1 : -1);
                Vector2 lateral = forward.Orthogonal() * side;
                bool orbit = Phase >= Mathf.Tau * 2;
                Vector2 loop = forward * (Mathf.Cos(Phase) * radius * .55f)
                    + lateral * ((orbit ? Mathf.Sin(Phase) : Mathf.Sin(Phase * 2)) * radius);
                destination = center - forward * standOff + loop;
                if (distance < standOff * .55f)
                    destination += (fighter.GlobalPosition - center).Normalized() * standOff;
                cruise = manoeuvreSpeed;
            }
        }
        destination = clamp(destination);
        Vector2 offset = destination - fighter.GlobalPosition;
        float braking = FighterDroneConfig.Safe(config.Deceleration, 1000, 32, 10000);
        float desiredSpeed = Mathf.Min(cruise, Mathf.Sqrt(2 * braking * offset.Length()));
        float desiredHeading = offset.LengthSquared() > .01f ? offset.Angle() : Heading;
        float turn = Mathf.DegToRad(FighterDroneConfig.Safe(config.TurnDegreesPerSecond, 300, 30, 720)) * dt;
        Heading = Mathf.Wrap(Heading + Mathf.Clamp(Mathf.AngleDifference(Heading, desiredHeading), -turn, turn), -Mathf.Pi, Mathf.Pi);
        Vector2 desiredVelocity = Vector2.FromAngle(Heading) * desiredSpeed;
        float rate = desiredSpeed < Velocity.Length() ? braking : FighterDroneConfig.Safe(config.Acceleration, 1000, 32, 10000);
        Velocity = Velocity.MoveToward(desiredVelocity, rate * dt);
        Vector2 position = fighter.GlobalPosition + Velocity * dt;
        Vector2 bounded = clamp(position);
        if (bounded != position) Velocity = (bounded - fighter.GlobalPosition) / dt;
        fighter.GlobalPosition = bounded;
    }
}
