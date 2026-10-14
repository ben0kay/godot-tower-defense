// Central, revision-aware A* queue; even competing goal searches obey the frame budget.
using Godot;
using System.Collections.Generic;

public partial class PathRequestManager : Node
{
    [Export] public EnemyMasterConfig Config { get; set; }
    public NavigationGrid Navigation { get; private set; }
    public int PendingCount => _queue.Count;
    public int LastFrameCalculations { get; private set; }
    private int _nextProfile;
    private readonly Queue<Request> _queue = new();
    private readonly HashSet<EnemyMovement> _pending = new();
    private sealed class Request
    {
        public EnemyMovement Movement;
        public int Revision, CostRevision, GoalIndex;
        public Vector2[] Best = System.Array.Empty<Vector2>();
        public float Length = float.PositiveInfinity;
    }

    // =========================================================
    // World services are siblings and scoped to this loaded battlefield.
    public override void _Ready() => Navigation = GetNode<NavigationGrid>("../Navigation");

    // =========================================================
    // Deterministic round-robin assignment keeps spawn batches balanced across shared profiles.
    public int AssignProfile() => _nextProfile++ % Mathf.Clamp(Config.RouteProfileCount, 1, 8);

    // =========================================================
    // At most one outstanding request per living ship.
    public void RequestPath(EnemyMovement movement)
    {
        if (_pending.Add(movement)) _queue.Enqueue(new Request { Movement = movement, Revision = Navigation.Revision, CostRevision = Navigation.CostRevision });
    }

    // =========================================================
    // Round-robin goal tests select the shortest valid route without bursts of unbounded A*.
    public override void _PhysicsProcess(double delta)
    {
        Navigation.Refresh(); Navigation.UpdateCongestion(delta); LastFrameCalculations = 0;
        int budget = Mathf.Max(1, Config.MaxPathCalculationsPerFrame);
        while (_queue.Count > 0 && LastFrameCalculations < budget)
        {
            Request request = _queue.Dequeue();
            EnemyMovement movement = request.Movement;
            if (!GodotObject.IsInstanceValid(movement) || !movement.Enemy.IsAlive)
            { _pending.Remove(movement); continue; }
            if (request.Revision != Navigation.Revision || request.CostRevision != Navigation.CostRevision)
            {
                request.Revision = Navigation.Revision; request.CostRevision = Navigation.CostRevision; request.GoalIndex = 0;
                request.Best = System.Array.Empty<Vector2>(); request.Length = float.PositiveInfinity;
            }
            if (request.GoalIndex < Navigation.Goals.Count)
            {
                Vector2[] route = Navigation.FindPath(movement.Enemy.GlobalPosition, Navigation.Goals[request.GoalIndex++], movement.RouteProfile, out float length);
                LastFrameCalculations++;
                request.Length = Navigation.RouteCost(movement.Enemy.GlobalPosition, request.Best, movement.RouteProfile);
                if (length < request.Length) { request.Best = route; request.Length = length; }
            }
            if (request.GoalIndex < Navigation.Goals.Count) _queue.Enqueue(request);
            else { _pending.Remove(movement); movement.AcceptPath(request.Best, request.Revision); }
        }
    }
}
