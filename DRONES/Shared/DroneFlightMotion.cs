// Shared curved flight with fixed acceleration/cruise/braking; cargo, site orbit, callbacks and dispatch ownership stay outside.
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
    // Optional flat checkpoint fields: zero in legacy saves retains their original smoothstep leg.
    public float FlightDistance { get; set; }
    public float FlightCruiseSpeed { get; set; }
    public float FlightAcceleration { get; set; }
    public float FlightDeceleration { get; set; }
    private float[] _arcLengths;
    private const int ArcSamples = 64;
    internal DroneFlightMotion CopyFlight() => (DroneFlightMotion)MemberwiseClone();
    internal void BeginFlight(Vector2 start, Vector2 end, Vector2 departure, Vector2 arrival, float speed, float curvePixels = 100, float acceleration = 800, float deceleration = 800)
    {
        Vector2 offset = end - start;
        float distance = offset.Length(); Vector2 direction = distance > .001f ? offset / distance : Vector2.Right;
        if (departure.LengthSquared() < .001f) departure = direction;
        if (arrival.LengthSquared() < .001f) arrival = direction;
        float bend = Mathf.Min(distance * .45f, Safe(curvePixels, 100, 0, 400));
        Start = SavePoint.From(start); End = SavePoint.From(end);
        Control1 = SavePoint.From(start + departure.Normalized() * bend);
        Control2 = SavePoint.From(end - arrival.Normalized() * bend);
        BuildArcLengths();
        FlightDistance = _arcLengths[ArcSamples];
        speed = Safe(speed, 280, 1, 2000);
        FlightAcceleration = Safe(acceleration, 800, 32, 10000);
        FlightDeceleration = Safe(deceleration, 800, 32, 10000);
        // Short legs use a triangular profile: rates stay fixed, only peak speed is lower.
        FlightCruiseSpeed = Mathf.Min(speed, Mathf.Sqrt(2 * FlightDistance /
            (1 / FlightAcceleration + 1 / FlightDeceleration)));
        FlightSeconds = FlightDistance <= .0001f ? .12f :
            FlightCruiseSpeed / FlightAcceleration + FlightCruiseSpeed / FlightDeceleration +
            Mathf.Max(0, FlightDistance - FlightCruiseSpeed * FlightCruiseSpeed *
                (.5f / FlightAcceleration + .5f / FlightDeceleration)) / FlightCruiseSpeed;
        FlightElapsed = 0;
        Velocity = default;
    }

    // =========================================================
    // Integrate a fixed-rate accelerate/cruise/brake profile, then map distance onto the cached curved path.
    internal bool Fly(float dt, ref Vector2 position, float turnDegrees = 240)
    {
        Vector2 before = position;
        FlightElapsed = Mathf.Min(FlightSeconds, FlightElapsed + dt);
        float time = FlightSeconds <= 0 ? 1 : FlightElapsed / FlightSeconds;
        float t = FlightDistance > .0001f && FlightCruiseSpeed > 0
            ? ParameterAtDistance(TravelledDistance()) : Ease(time);
        position = CurvePoint(t);
        Face(position - before, dt, turnDegrees); Velocity = SavePoint.From((position - before) / Mathf.Max(dt, .0001f));
        return time >= 1;
    }

    // =========================================================
    // One retained 64-segment distance table per leg; snapshots share it immutably, restored legs rebuild once.
    private Vector2 CurvePoint(float t)
    {
        float inverse = 1 - t;
        return Start.Vector() * (inverse * inverse * inverse) + Control1.Vector() * (3 * inverse * inverse * t)
            + Control2.Vector() * (3 * inverse * t * t) + End.Vector() * (t * t * t);
    }
    private void BuildArcLengths()
    {
        _arcLengths = new float[ArcSamples + 1];
        Vector2 previous = Start.Vector();
        for (int i = 1; i <= ArcSamples; i++)
        {
            Vector2 next = CurvePoint((float)i / ArcSamples);
            _arcLengths[i] = _arcLengths[i - 1] + previous.DistanceTo(next); previous = next;
        }
    }
    private float TravelledDistance()
    {
        float accelerate = FlightCruiseSpeed / FlightAcceleration;
        float brake = FlightCruiseSpeed / FlightDeceleration;
        if (FlightElapsed < accelerate) return .5f * FlightAcceleration * FlightElapsed * FlightElapsed;
        if (FlightElapsed > FlightSeconds - brake)
        {
            float remaining = Mathf.Max(0, FlightSeconds - FlightElapsed);
            return FlightDistance - .5f * FlightDeceleration * remaining * remaining;
        }
        return .5f * FlightCruiseSpeed * accelerate + FlightCruiseSpeed * (FlightElapsed - accelerate);
    }
    private float ParameterAtDistance(float distance)
    {
        if (_arcLengths == null) BuildArcLengths();
        int low = 0, high = ArcSamples;
        distance = Mathf.Clamp(distance, 0, FlightDistance);
        while (high - low > 1)
        {
            int middle = (low + high) / 2;
            if (_arcLengths[middle] < distance) low = middle; else high = middle;
        }
        if (distance <= 0) return 0;
        if (distance >= FlightDistance) return 1;
        // Refine within the small segment: uniform t interpolation alone slows long curves near their endpoints.
        float from = (float)low / ArcSamples, to = (float)high / ArcSamples;
        Vector2 segmentStart = CurvePoint(from);
        float target = distance - _arcLengths[low];
        for (int i = 0; i < 8; i++)
        {
            float middle = (from + to) * .5f;
            if (segmentStart.DistanceTo(CurvePoint(middle)) < target) from = middle; else to = middle;
        }
        return (from + to) * .5f;
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
        bool profile = FlightDistance != 0 || FlightCruiseSpeed != 0 || FlightAcceleration != 0 || FlightDeceleration != 0;
        if (profile && (!float.IsFinite(FlightDistance) || FlightDistance < 0 || FlightDistance > 1e9f ||
            !float.IsFinite(FlightCruiseSpeed) || FlightCruiseSpeed < 0 || FlightCruiseSpeed > 2000 ||
            !float.IsFinite(FlightAcceleration) || FlightAcceleration < 32 || FlightAcceleration > 10000 ||
            !float.IsFinite(FlightDeceleration) || FlightDeceleration < 32 || FlightDeceleration > 10000 ||
            (FlightDistance > .0001f && FlightCruiseSpeed <= 0)))
            throw new System.IO.InvalidDataException("Invalid saved drone speed profile.");
        foreach (var point in new[] { Start, Control1, Control2, End, Velocity })
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || Mathf.Abs(point.X) > 1e9f || Mathf.Abs(point.Y) > 1e9f)
                throw new System.IO.InvalidDataException("Invalid saved drone curve.");
    }
}
