// Per-assignment curved flight and hover/orbit state. Advanced only by the existing active builder manager.
// Primitive checkpoint properties preserve curves, easing, heading and deterministic random choices across reloads.
using Godot;
using System;

public sealed class ConstructionDroneMotion
{
    public SavePoint Start { get; set; }
    public SavePoint Control1 { get; set; }
    public SavePoint Control2 { get; set; }
    public SavePoint End { get; set; }
    public SavePoint Velocity { get; set; }
    public SavePoint Radii { get; set; }
    public SavePoint HoverStart { get; set; }
    public float FlightElapsed { get; set; }
    public float FlightSeconds { get; set; }
    public float Heading { get; set; }
    public float Angle { get; set; }
    public float OrbitFrom { get; set; }
    public float OrbitTo { get; set; }
    public float OrbitElapsed { get; set; }
    public float OrbitSeconds { get; set; }
    public float HoldSeconds { get; set; }
    public float HoverBlend { get; set; }
    public float BobPhase { get; set; }
    public uint RandomState { get; set; }

    // =========================================================
    // Save an independent value snapshot, never the live mutable assignment state.
    internal ConstructionDroneMotion Copy() => (ConstructionDroneMotion)MemberwiseClone();

    // =========================================================
    // Tiny per-job PRNG runs only at initialization and orbit decisions, without shared/global random coupling.
    internal float Random()
    {
        uint n = RandomState == 0 ? 0x9e3779b9u : RandomState;
        n ^= n << 13; n ^= n >> 17; n ^= n << 5; RandomState = n;
        return (n >> 8) / 16777216f;
    }

    // =========================================================
    // Cubic flight starts along the supplied departure heading and bends into its arrival direction.
    internal void BeginFlight(Vector2 start, Vector2 end, Vector2 departure, Vector2 arrival, ConstructionDroneMasterConfig config)
    {
        Vector2 offset = end - start;
        float distance = offset.Length(); Vector2 direction = distance > .001f ? offset / distance : Vector2.Right;
        if (departure.LengthSquared() < .001f) departure = direction;
        if (arrival.LengthSquared() < .001f) arrival = direction;
        float bend = Mathf.Min(distance * .45f, ConstructionDroneMasterConfig.Safe(config.FlightCurvePixels, 100, 0, 400));
        Start = SavePoint.From(start); End = SavePoint.From(end);
        Control1 = SavePoint.From(start + departure.Normalized() * bend);
        Control2 = SavePoint.From(end - arrival.Normalized() * bend);
        float estimate = (distance + start.DistanceTo(Control1.Vector()) + Control1.Vector().DistanceTo(Control2.Vector()) + Control2.Vector().DistanceTo(end)) * .5f;
        float speed = ConstructionDroneMasterConfig.Safe(config.TravelSpeed, 420, 32, 2000);
        // Smoothstep's peak derivative is 1.5; keep the configured speed approximately the peak cruise speed.
        FlightSeconds = Mathf.Max(.12f, estimate * 1.5f / speed); FlightElapsed = 0;
        Velocity = default;
    }

    // =========================================================
    // Acceleration/braking are the derivative of one bounded curve, avoiding steering overshoot and catch-up loops.
    internal bool Fly(float dt, ref Vector2 position, ConstructionDroneMasterConfig config)
    {
        Vector2 before = position;
        FlightElapsed = Mathf.Min(FlightSeconds, FlightElapsed + dt);
        float time = FlightSeconds <= 0 ? 1 : FlightElapsed / FlightSeconds;
        float t = Ease(time), inverse = 1 - t;
        position = Start.Vector() * (inverse * inverse * inverse) + Control1.Vector() * (3 * inverse * inverse * t)
            + Control2.Vector() * (3 * inverse * t * t) + End.Vector() * (t * t * t);
        Face(position - before, dt, config); Velocity = SavePoint.From((position - before) / Mathf.Max(dt, .0001f));
        return time >= 1;
    }

    // =========================================================
    // An elevated ellipse is sized by the manager to each building footprint; gentle bobbing is visual motion only.
    internal Vector2 OrbitPoint(Vector2 center) => center + new Vector2(Mathf.Cos(Angle) * Radii.X, Mathf.Sin(Angle) * Radii.Y);
    internal Vector2 Tangent() => new(-Mathf.Sin(Angle) * Radii.X, Mathf.Cos(Angle) * Radii.Y);

    // =========================================================
    // Brief rests alternate with eased arcs to new random angles. The existing work clock never pauses for movement.
    internal void Hover(Vector2 center, float age, float bob, float dt, ref Vector2 position, ConstructionDroneMasterConfig config)
    {
        Vector2 before = position;
        if (OrbitSeconds > 0)
        {
            OrbitElapsed = Mathf.Min(OrbitSeconds, OrbitElapsed + dt);
            Angle = Mathf.Lerp(OrbitFrom, OrbitTo, Ease(OrbitElapsed / OrbitSeconds));
            if (OrbitElapsed >= OrbitSeconds) { Angle = Mathf.Wrap(Angle, -Mathf.Pi, Mathf.Pi); OrbitSeconds = 0; HoldSeconds = NextHold(config); }
        }
        else
        {
            HoldSeconds = Mathf.Max(0, HoldSeconds - dt);
            if (HoldSeconds <= 0)
            {
                OrbitFrom = Angle;
                float arc = Mathf.DegToRad(Mathf.Lerp(55, 155, Random())) * (Random() < .5f ? -1 : 1);
                OrbitTo = Angle + arc; OrbitElapsed = 0;
                float speed = ConstructionDroneMasterConfig.Safe(config.OrbitDegreesPerSecond, 55, 5, 180);
                OrbitSeconds = Mathf.Max(.3f, Mathf.Abs(Mathf.RadToDeg(arc)) * 1.5f / speed);
            }
        }
        HoverBlend = Mathf.Min(1, HoverBlend + dt);
        float phase = BobPhase;
        Vector2 drift = new(Mathf.Sin(age * 1.2f + phase), Mathf.Sin(age * 1.7f + phase) * .35f);
        Vector2 desired = OrbitPoint(center) + drift * Mathf.Min(bob, 4) * Ease(Mathf.Min(1, age));
        position = HoverStart.Vector().Lerp(desired, Ease(HoverBlend));
        Vector2 movement = position - before;
        // During a rest, retain heading instead of chasing tiny bobbing offsets.
        if (OrbitSeconds > 0) Face(movement, dt, config);
        Velocity = SavePoint.From(movement / Mathf.Max(dt, .0001f));
    }

    // =========================================================
    // Each rest gets modest independent variation; bounds are sanitized even for live/programmatic edits.
    internal float NextHold(ConstructionDroneMasterConfig config)
    {
        float minimum = ConstructionDroneMasterConfig.Safe(config.HoverPauseMinimum, .6f, .05f, 10);
        float maximum = Mathf.Max(minimum, ConstructionDroneMasterConfig.Safe(config.HoverPauseMaximum, 1.4f, minimum, 15));
        return Mathf.Lerp(minimum, maximum, Random());
    }

    // =========================================================
    // Rotate the sprite toward travel with a bounded angular speed; the beam's coordinate system stays fixed.
    private void Face(Vector2 movement, float dt, ConstructionDroneMasterConfig config)
    {
        if (movement.LengthSquared() < .000001f) return;
        float turn = Mathf.DegToRad(ConstructionDroneMasterConfig.Safe(config.TurnDegreesPerSecond, 240, 30, 720)) * dt;
        float difference = Mathf.AngleDifference(Heading, movement.Angle());
        Heading = Mathf.Wrap(Heading + Mathf.Clamp(difference, -turn, turn), -Mathf.Pi, Mathf.Pi);
    }

    // =========================================================
    // Zero endpoint velocity gives an unhurried launch/arrival and smooth orbit starts/stops.
    private static float Ease(float t) => t * t * (3 - 2 * t);
    // =========================================================
    // File data cannot inject invalid clocks, positions or unbounded orbit state into the active updater.
    internal void Validate()
    {
        foreach (float number in new[] { FlightElapsed, FlightSeconds, Heading, Angle, OrbitFrom, OrbitTo, OrbitElapsed, OrbitSeconds, HoldSeconds, HoverBlend, BobPhase })
            if (!float.IsFinite(number) || Mathf.Abs(number) > 1e9f) throw new System.IO.InvalidDataException("Invalid saved builder motion.");
        foreach (var point in new[] { Start, Control1, Control2, End, Velocity, Radii, HoverStart })
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || Mathf.Abs(point.X) > 1e9f || Mathf.Abs(point.Y) > 1e9f)
                throw new System.IO.InvalidDataException("Invalid saved builder curve.");
        if (FlightSeconds <= 0 || FlightElapsed < 0 || FlightElapsed > FlightSeconds || Radii.X <= 0 || Radii.Y <= 0 ||
            OrbitSeconds < 0 || OrbitElapsed < 0 || (OrbitSeconds > 0 && OrbitElapsed > OrbitSeconds) || HoldSeconds < 0 || HoverBlend < 0 || HoverBlend > 1 || RandomState == 0)
            throw new System.IO.InvalidDataException("Invalid saved builder movement phase.");
    }
}
