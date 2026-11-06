// Ammunition courier flight adapter. Logistics alone owns cargo, source slots and destination reservations.
using Godot;
using System;
public partial class CargoDrone : Node2D
{
    [Export] public float FlightAcceleration { get; set; } = 800;
    [Export] public float FlightDeceleration { get; set; } = 800;
    [Export] public float FlightCurvePixels { get; set; } = 100;
    [Export] public float TurnDegreesPerSecond { get; set; } = 240;
    public bool Returning { get; private set; }
    public float Speed { get; private set; }
    public float RemainingFlightSeconds => Mathf.Max(0, _motion.FlightSeconds - _motion.FlightElapsed);
    private DroneFlightMotion _motion = new();
    private DroneBody _body;
    private bool _loaded = true, _finished, _arrivalReported;
    private Node2D _home, _destination;
    private Action _arrived, _returned, _aborted;
    public void Initialize(Node2D home, Node2D destination, float speed, Action arrived, Action returned, Action aborted)
    { _home = home; _destination = destination; Speed = float.IsFinite(speed) ? Mathf.Max(1, speed) : 1; _arrived = arrived; _returned = returned; _aborted = aborted; }
    public override void _Ready()
    {
        _body = GetNode<DroneBody>("Body"); ZIndex = 12; SetProcess(false);
        if (!Live(_home)) { Abort(); return; }
        GlobalPosition = _home.GlobalPosition;
        if (Live(_destination)) BeginLeg(_destination.GlobalPosition);
    }
    // =========================================================
    // Independent active courier tick; no builder dispatcher, work gate, navigation or inventory access.
    public override void _PhysicsProcess(double delta)
    {
        if (_finished || !IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (!Live(_home)) { Abort(); return; }
        Node2D target = Returning ? _home : _destination;
        if (!Live(target)) { ReportArrival(); return; }
        if (_motion.End.Vector().DistanceSquaredTo(target.GlobalPosition) > .01f) BeginLeg(target.GlobalPosition);
        Vector2 position = GlobalPosition; float dt = (float)Math.Min(delta, 3600);
        bool arrived = _motion.Fly(dt, ref position, TurnDegreesPerSecond); GlobalPosition = position;
        _body.Present(_motion.Heading, _motion.Velocity.Vector(), Speed, true, Returning ? 1 : 0, _loaded, dt);
        if (!arrived) return;
        if (!Returning) ReportArrival();
        else { _finished = true; _returned?.Invoke(); if (GodotObject.IsInstanceValid(this)) QueueFree(); }
    }
    private void BeginLeg(Vector2 end)
    {
        Vector2 direction = end - GlobalPosition;
        bool first = _motion.FlightSeconds <= 0;
        Vector2 departure = _motion.Velocity.Vector();
        if (departure.LengthSquared() < 1) departure = first ? direction.Rotated(.35f) : Vector2.FromAngle(_motion.Heading);
        _motion.BeginFlight(GlobalPosition, end, departure, direction.Rotated(-.3f), Speed, FlightCurvePixels, FlightAcceleration, FlightDeceleration);
        if (first && direction.LengthSquared() > .001f) _motion.Heading = departure.Angle();
        _body?.Present(_motion.Heading, Vector2.Zero, Speed, true, Returning ? 1 : 0, _loaded, 0);
    }
    private void ReportArrival() { if (_arrivalReported || _finished) return; _arrivalReported = true; _arrived?.Invoke(); }
    public void ReturnHome() { if (_finished || Returning || !GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return; Returning = true; if (Live(_home)) BeginLeg(_home.GlobalPosition); }
    public void SetLoaded(bool loaded) { _loaded = loaded; }
    private void Abort() { if (_finished) return; _finished = true; _aborted?.Invoke(); if (GodotObject.IsInstanceValid(this)) QueueFree(); }
    public override void _ExitTree()
    { if (!_finished) { _finished = true; _aborted?.Invoke(); } _arrived = _returned = _aborted = null; _home = _destination = null; }
    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    // =========================================================
    // Restore exact new curves, or build an eased remaining leg from legacy saved positions, without callbacks.
    internal DroneFlightMotion CaptureFlight() => _motion.CopyFlight();
    internal void RestoreFlight(bool returning, Vector2 position, DroneFlightMotion motion)
    {
        Returning = returning; _arrivalReported = returning; GlobalPosition = position;
        Node2D target = returning ? _home : _destination;
        if (!Live(target)) throw new System.IO.InvalidDataException("Saved courier target is missing.");
        if (motion != null)
        {
            motion.ValidateFlight();
            if (motion.End.Vector().DistanceSquaredTo(target.GlobalPosition) > .01f) throw new System.IO.InvalidDataException("Saved courier endpoint changed.");
            _motion = motion.CopyFlight();
        }
        else { _motion = new DroneFlightMotion(); BeginLeg(target.GlobalPosition); }
        _body.Reset(); _body.Present(_motion.Heading, _motion.Velocity.Vector(), Speed, true, Returning ? 1 : 0, _loaded, 0, ignite: false);
    }
}
