// Reusable direct-flight courier. Motion/visuals only: callers own payloads, reservations and transactions.
using Godot;
using System;

public partial class TransportDrone : Node2D
{
    public bool Returning { get; private set; }
    public float Speed { get; private set; }
    private bool _loaded = true;
    public void SetLoaded(bool loaded) { _loaded = loaded; QueueRedraw(); }
    private Node2D _home, _destination;
    private Action _arrived, _returned, _aborted;
    private bool _finished;

    // =========================================================
    // Configure before entering the tree; source slots remain busy through the return flight.
    public void Initialize(Node2D home, Node2D destination, float speed, Action arrived, Action returned, Action aborted)
    { _home = home; _destination = destination; Speed = Mathf.Max(1, speed); _arrived = arrived; _returned = returned; _aborted = aborted; }

    // =========================================================
    // Begin at home; only active drones receive physics updates, with no enemy/pathfinding queries.
    public override void _Ready() { ZIndex = 12; GlobalPosition = _home.GlobalPosition; SetProcess(false); }

    // =========================================================
    // Swept movement clamps to the endpoint, so a long physics frame never skips arrival.
    public override void _PhysicsProcess(double delta)
    {
        if (_finished || !double.IsFinite(delta) || delta <= 0) return;
        if (!Live(_home)) { Abort(); return; }
        Node2D target = Returning ? _home : _destination;
        if (!Live(target)) { _arrived?.Invoke(); return; }
        Vector2 offset = target.GlobalPosition - GlobalPosition;
        if (offset.LengthSquared() > .01f) Rotation = offset.Angle();
        GlobalPosition = GlobalPosition.MoveToward(target.GlobalPosition, Speed * (float)delta);
        if (GlobalPosition.DistanceSquaredTo(target.GlobalPosition) > .01f) return;
        if (!Returning) _arrived?.Invoke();
        else { _finished = true; _returned?.Invoke(); QueueFree(); }
    }

    // =========================================================
    // Keep the empty vehicle visibly travelling home instead of teleporting/freeing its source slot.
    public void ReturnHome() { Returning = true; QueueRedraw(); }

    // =========================================================
    // Endpoint loss/external drone destruction is reported exactly once to the logistics owner.
    private void Abort() { if (_finished) return; _finished = true; _aborted?.Invoke(); QueueFree(); }
    public override void _ExitTree() { if (!_finished) { _finished = true; _aborted?.Invoke(); } }
    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();

    // =========================================================
    // Small readable vector courier; amber cargo outward, cyan frame on the return leg.
    public override void _Draw()
    {
        Color edge = new("#72e5ee"), cargo = !_loaded ? new Color("#244356") : new Color("#ffc06b");
        DrawColoredPolygon(new[] { new Vector2(14, 0), new Vector2(-10, -9), new Vector2(-5, 0), new Vector2(-10, 9) }, new Color("#102d41"));
        DrawPolyline(new[] { new Vector2(14, 0), new Vector2(-10, -9), new Vector2(-5, 0), new Vector2(-10, 9), new Vector2(14, 0) }, edge, 2, true);
        DrawRect(new Rect2(-5, -4, 8, 8), cargo); DrawLine(new Vector2(-10, 0), new Vector2(-16, 0), edge, 3);
    }
}
