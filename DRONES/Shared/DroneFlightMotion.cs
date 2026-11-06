// Shared cubic/eased flight state only; cargo, site orbit, callbacks and dispatch ownership stay outside.
using Godot;
using System;
public class DroneFlightMotion
{
    public SavePoint Start { get; set; }
    public SavePoint Control1 { get; set; }
    public SavePoint Control2 { get; set; }
    public SavePoint End { get; set; }
    public SavePoint Velocity { get; set; }
    public float FlightElapsed { get; set; }
    public float FlightSeconds { get; set; }
    public float Heading { get; set; }
    internal DroneFlightMotion CopyFlight() => (DroneFlightMotion)MemberwiseClone();
    internal void BeginFlight(Vector2 start, Vector2 end, Vector2 departure, Vector2 arrival, float speed, float curvePixels = 100)
    {
        Vector2 offset = end - start;
        float distance = offset.Length(); Vector2 direction = distance > .001f ? offset / distance : Vector2.Right;
        if (departure.LengthSquared() < .001f) departure = direction;
        if (arrival.LengthSquared() < .001f) arrival = direction;
        float bend = Mathf.Min(distance * .45f, Safe(curvePixels, 100, 0, 400));
        Start = SavePoint.From(start); End = SavePoint.From(end);
        Control1 = SavePoint.From(start + departure.Normalized() * bend);
        Control2 = SavePoint.From(end - arrival.Normalized() * bend);
        float estimate = (distance + start.DistanceTo(Control1.Vector()) + Control1.Vector().DistanceTo(Control2.Vector()) + Control2.Vector().DistanceTo(end)) * .5f;
        speed = float.IsFinite(speed) && speed > 0 ? speed : 280;
        // Smoothstep's peak derivative is 1.5; keep the configured speed approximately the peak cruise speed.
        FlightSeconds = Mathf.Max(.12f, estimate * 1.5f / speed); FlightElapsed = 0;
        Velocity = default;
    }

    // =========================================================
    // Acceleration/braking are the derivative of one bounded curve, avoiding steering overshoot and catch-up loops.
    internal bool Fly(float dt, ref Vector2 position, float turnDegrees = 240)
    {
        Vector2 before = position;
        FlightElapsed = Mathf.Min(FlightSeconds, FlightElapsed + dt);
        float time = FlightSeconds <= 0 ? 1 : FlightElapsed / FlightSeconds;
        float t = Ease(time), inverse = 1 - t;
        position = Start.Vector() * (inverse * inverse * inverse) + Control1.Vector() * (3 * inverse * inverse * t)
            + Control2.Vector() * (3 * inverse * t * t) + End.Vector() * (t * t * t);
        Face(position - before, dt, turnDegrees); Velocity = SavePoint.From((position - before) / Mathf.Max(dt, .0001f));
        return time >= 1;
    }

    protected void Face(Vector2 movement, float dt, float turnDegrees)
    {
        if (movement.LengthSquared() < .000001f) return;
        float turn = Mathf.DegToRad(Safe(turnDegrees, 240, 30, 720)) * dt;
        float difference = Mathf.AngleDifference(Heading, movement.Angle());
        Heading = Mathf.Wrap(Heading + Mathf.Clamp(difference, -turn, turn), -Mathf.Pi, Mathf.Pi);
    }

    // =========================================================
    // Zero endpoint velocity gives an unhurried launch/arrival and smooth orbit starts/stops.
    protected static float Ease(float t) => t * t * (3 - 2 * t);

    protected static float Safe(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
    // =========================================================
    // Optional courier snapshots use the same finite bounds as existing builder curves.
    internal void ValidateFlight()
    {
        if (!float.IsFinite(FlightElapsed) || !float.IsFinite(FlightSeconds) || !float.IsFinite(Heading)
            || FlightSeconds <= 0 || FlightSeconds > 1e9f || FlightElapsed < 0 || FlightElapsed > FlightSeconds || Mathf.Abs(Heading) > 1e9f)
            throw new System.IO.InvalidDataException("Invalid saved drone flight.");
        foreach (var point in new[] { Start, Control1, Control2, End, Velocity })
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || Mathf.Abs(point.X) > 1e9f || Mathf.Abs(point.Y) > 1e9f)
                throw new System.IO.InvalidDataException("Invalid saved drone curve.");
    }
}
