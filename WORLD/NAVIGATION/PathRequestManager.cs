// Central bounded A* and route-validation work; urgency plus regular oldest-request service.
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public partial class PathRequestManager : Node
{
    [Export] public EnemyMasterConfig Config { get; set; }
    public NavigationGrid Navigation { get; private set; }
    public int PendingCount => _requests.Count;
    public int PendingValidationCount => _validationPending.Count;
    public int LastFrameCalculations { get; private set; }
    public int LastFrameValidations { get; private set; }
    public double PeakPathfindingMilliseconds { get; private set; }
    public double PeakValidationMilliseconds { get; private set; }
    public double LastFrameAStarMilliseconds { get; private set; }
    public double LastPathfindingMilliseconds { get; private set; }
    public double LastValidationMilliseconds { get; private set; }
    public long TotalCalculations { get; private set; }
    private int _nextProfile;
    private uint _corridorSpawnOrdinal;
    private long _serviceOrder;
    private readonly List<Request> _requests = new();
    private readonly Dictionary<EnemyMovement, Request> _pending = new();
    private readonly Queue<EnemyMovement> _validations = new();
    private readonly HashSet<EnemyMovement> _validationPending = new();
    private readonly Dictionary<Vector2I, HashSet<EnemyMovement>> _watchers = new();
    private sealed class Request
    {
        public EnemyMovement Movement;
        public int Revision, CostRevision, GoalIndex;
        public long LastServed;
        public float Urgency, Length = float.PositiveInfinity;
        public Vector2[] Best = System.Array.Empty<Vector2>();
        public Vector2 Origin;
        public bool HasOrigin;
    }

    #region Services and Dependency Index
    // =========================================================
    // One cell-change subscription for all routes, rather than a full scan per ship per tick.
    public override void _Ready()
    {
        Navigation = GetNode<NavigationGrid>("../Navigation");
        Navigation.Grid.NavigationChanged += OnGridChanged;
    }

    // =========================================================
    // Disconnect world-scoped subscriptions on unload.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(Navigation?.Grid)) Navigation.Grid.NavigationChanged -= OnGridChanged; }

    // =========================================================
    // Profiles are balanced independently from offset randomization.
    public int AssignProfile() => _nextProfile++ % Mathf.Clamp(Config.RouteProfileCount, 1, 8);

    // =========================================================
    // One independent ordinal per initialized ship; does not change profile/wave/portal random streams.
    public uint AssignCorridorSeed() => CorridorPreference.Seed(_corridorSpawnOrdinal++, Config.RoutePreferenceSeed);

    // =========================================================
    // Cache conservative swept segment cells when assigning a path, never every physics frame.
    public void RegisterRoute(EnemyMovement movement)
    {
        foreach (Vector2I cell in movement.RouteDependencies.Keys)
        {
            if (!_watchers.TryGetValue(cell, out HashSet<EnemyMovement> watchers)) _watchers[cell] = watchers = new();
            watchers.Add(movement);
        }
    }

    // =========================================================
    // Remove old dependencies before path replacement or ship deletion.
    public void UnregisterRoute(EnemyMovement movement)
    {
        foreach (Vector2I cell in movement.RouteDependencies.Keys)
            if (_watchers.TryGetValue(cell, out HashSet<EnemyMovement> watchers))
            { watchers.Remove(movement); if (watchers.Count == 0) _watchers.Remove(cell); }
    }

    // =========================================================
    // Only relevant indexed ships are notified on a block; removing a block preserves valid routes.
    private void OnGridChanged(Vector2I? cell, bool blocked)
    {
        if (!blocked) return;
        if (cell.HasValue)
        {
            if (_watchers.TryGetValue(cell.Value, out HashSet<EnemyMovement> watchers))
                foreach (EnemyMovement movement in watchers) movement.MarkChangedCell(cell);
        }
        else foreach (Enemy enemy in GetNode<EnemyRegistry>("../Registry").Living) enemy.Movement.MarkChangedCell(null);
    }
    #endregion

    #region Queues
    // =========================================================
    // No duplicate path requests; immediate-clearance failure can upgrade an existing urgency.
    public void RequestPath(EnemyMovement movement, float distanceToObstacle = float.PositiveInfinity)
    {
        if (_pending.TryGetValue(movement, out Request existing))
        { existing.Urgency = Mathf.Min(existing.Urgency, distanceToObstacle); return; }
        Request request = new() { Movement = movement, Revision = Navigation.Revision,
            CostRevision = Navigation.CostRevision, Urgency = distanceToObstacle, LastServed = _serviceOrder++ };
        _pending.Add(movement, request); _requests.Add(request);
    }

    // =========================================================
    // Cell changes and periodic audits coalesce into one validation per ship.
    public void RequestValidation(EnemyMovement movement)
    { if (_validationPending.Add(movement)) _validations.Enqueue(movement); }

    // =========================================================
    // Bound validity work independently; unaffected routes keep moving while checks wait.
    private void ProcessValidations()
    {
        long started = Stopwatch.GetTimestamp(); LastFrameValidations = 0;
        while (_validations.Count > 0 && LastFrameValidations < Mathf.Max(1, Config.MaxRouteValidationsPerFrame))
        {
            EnemyMovement movement = _validations.Dequeue(); _validationPending.Remove(movement);
            if (!GodotObject.IsInstanceValid(movement) || !movement.Enemy.IsAlive) continue;
            movement.ValidateRoute(); LastFrameValidations++;
            if (Config.MaxRouteValidationMillisecondsPerFrame > 0f
                && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= Config.MaxRouteValidationMillisecondsPerFrame) break;
        }
        LastValidationMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        PeakValidationMilliseconds = System.Math.Max(PeakValidationMilliseconds, LastValidationMilliseconds);
    }

    // =========================================================
    // Lightweight bounded-list selection avoids mutable-priority heap tickets and duplicate queues.
    private int NextRequestIndex(bool fair)
    {
        int best = 0;
        for (int i = 1; i < _requests.Count; i++)
        {
            Request candidate = _requests[i], chosen = _requests[best];
            float candidateUrgency = Mathf.Min(candidate.Urgency, candidate.Movement.PendingObstacleDistance);
            float chosenUrgency = Mathf.Min(chosen.Urgency, chosen.Movement.PendingObstacleDistance);
            if (fair ? candidate.LastServed < chosen.LastServed
                : candidateUrgency < chosenUrgency || (candidateUrgency == chosenUrgency && candidate.LastServed < chosen.LastServed)) best = i;
        }
        return best;
    }

    // =========================================================
    // Time limit is soft: an individual native A* call cannot be interrupted, then work yields.
    public override void _PhysicsProcess(double delta)
    {
        Navigation.Refresh(); Navigation.UpdateCongestion(delta); ProcessValidations();
        long started = Stopwatch.GetTimestamp(); LastFrameCalculations = 0; LastFrameAStarMilliseconds = 0;
        int budget = Mathf.Max(1, Config.MaxPathCalculationsPerFrame);
        while (_requests.Count > 0 && LastFrameCalculations < budget)
        {
            bool fair = TotalCalculations % Mathf.Max(1, Config.FairRequestEveryCalculations) == 0;
            int index = NextRequestIndex(fair); Request request = _requests[index]; EnemyMovement movement = request.Movement;
            if (!GodotObject.IsInstanceValid(movement) || !movement.Enemy.IsAlive)
            { _requests.RemoveAt(index); _pending.Remove(movement); continue; }
            if (request.Revision != Navigation.Revision || request.CostRevision != Navigation.CostRevision)
            {
                request.Revision = Navigation.Revision; request.CostRevision = Navigation.CostRevision;
                request.GoalIndex = 0; request.HasOrigin = false; request.Best = System.Array.Empty<Vector2>(); request.Length = float.PositiveInfinity;
            }
            if (request.GoalIndex < Navigation.Goals.Count)
            {
                if (!request.HasOrigin) { request.Origin = movement.Enemy.GlobalPosition; request.HasOrigin = true; }
                Vector2[] route = Navigation.FindPath(request.Origin, Navigation.Goals[request.GoalIndex++], movement.RouteProfile, out float length);
                LastFrameAStarMilliseconds += Navigation.LastAStarMilliseconds;
                LastFrameCalculations++; TotalCalculations++; request.LastServed = _serviceOrder++;
                request.Length = Navigation.RouteCost(request.Origin, request.Best, movement.RouteProfile);
                if (length < request.Length) { request.Best = route; request.Length = length; }
            }
            if (request.GoalIndex >= Navigation.Goals.Count)
            { _requests.RemoveAt(index); _pending.Remove(movement); movement.AcceptPath(request.Best, request.Revision); }
            if (Config.MaxPathfindingMillisecondsPerFrame > 0f
                && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= Config.MaxPathfindingMillisecondsPerFrame) break;
        }
        LastPathfindingMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        PeakPathfindingMilliseconds = System.Math.Max(PeakPathfindingMilliseconds, LastPathfindingMilliseconds);
    }
    #endregion
}
