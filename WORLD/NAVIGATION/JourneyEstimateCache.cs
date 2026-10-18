// Shared checkpoint-tail estimates. A* runs only through PathRequestManager's remaining budget, never in targeting.
using Godot;
using System;
using System.Collections.Generic;

public sealed class JourneyEstimateCache : IDisposable
{
    public sealed class Plan
    {
        internal readonly List<Edge> Edges = new();
        internal int Pending;
        internal float Sum;
        internal bool SourceMoved, Broken;
        public float Distance => Pending == 0 && !SourceMoved && !Broken ? Sum : float.PositiveInfinity;
        internal void Finish(float distance) { Sum += distance; Pending--; }
    }
    internal sealed class Edge
    {
        internal RouteCheckpoint From, To;
        internal CitadelDestination Destination;
        internal int Profile, Goal;
        internal float Radius, BestCost = float.PositiveInfinity, Distance = float.PositiveInfinity;
        internal bool Complete;
        internal readonly List<Plan> Users = new();
    }
    private readonly NavigationGrid _nav;
    private readonly Dictionary<(RouteCheckpoint, RouteCheckpoint, int, float, CitadelDestination), Edge> _edges = new();
    private readonly Dictionary<RouteCheckpoint, Action> _observed = new();
    private readonly HashSet<Plan> _plans = new();
    private readonly Queue<Edge> _pending = new();
    private int _revision, _costRevision;
    public int PendingEdges => _pending.Count;
    public int CachedEdges => _edges.Count;
    public JourneyEstimateCache(NavigationGrid nav) { _nav = nav; _revision = nav.Revision; _costRevision = nav.CostRevision; }

    // =========================================================
    // Follow already-captured links using the exact independent branch formula; never advance any RNG.
    public Plan Attach(CheckpointNetwork network, RouteCheckpoint checkpoint, uint seed, int completed, int profile, float radius, CitadelDestination destination = null)
    {
        destination ??= _nav.SingleDestination;
        profile = profile == NavigationGrid.NeutralProfile ? profile : ((profile % _nav.SharedProfileCount) + _nav.SharedProfileCount) % _nav.SharedProfileCount;
        Plan plan = new(); _plans.Add(plan);
        if (network == null || network.Error.Length > 0) { plan.Broken = true; return plan; }
        RouteCheckpoint from = checkpoint;
        for (int i = 0; i < CheckpointNetwork.MaximumNodes; i++)
        {
            if (!GodotObject.IsInstanceValid(from)) { plan.Broken = true; break; }
            RouteCheckpoint to = network.Next(from, seed, completed++, out string error);
            if (error.Length > 0) { plan.Broken = true; break; }
            var key = (from, to, profile, radius, destination);
            if (!_edges.TryGetValue(key, out Edge edge))
            {
                // Bound optional estimation memory; unavailable estimates cannot win First or Last.
                if (_edges.Count >= 4096) { plan.Broken = true; break; }
                edge = new() { From = from, To = to, Profile = profile, Radius = radius, Destination = destination };
                _edges.Add(key, edge); _pending.Enqueue(edge); Observe(from); if (to != null) Observe(to);
            }
            if (edge.Users.Count == 0 && edge.Complete && !float.IsFinite(edge.Distance))
            { edge.Complete = false; edge.Goal = 0; edge.BestCost = float.PositiveInfinity; _pending.Enqueue(edge); }
            edge.Users.Add(plan); plan.Edges.Add(edge);
            if (edge.Complete) plan.Sum += edge.Distance; else plan.Pending++;
            if (to == null) return plan;
            from = to;
        }
        plan.Broken = true; return plan;
    }

    // =========================================================
    // Repaths/leg transitions and removal release only their own subscriptions.
    public void Detach(Plan plan)
    { if (plan == null) return; foreach (Edge edge in plan.Edges) edge.Users.Remove(plan); _plans.Remove(plan); }
    // =========================================================
    // Geometry edits invalidate estimates, not enemy routes or captured branch choices.
    private void Observe(RouteCheckpoint checkpoint)
    {
        if (_observed.ContainsKey(checkpoint)) return;
        Action changed = () =>
        {
            foreach (Plan plan in _plans)
                if (plan.Edges.Count > 0 && plan.Edges[0].From == checkpoint) plan.SourceMoved = true;
            Invalidate();
        };
        _observed.Add(checkpoint, changed);
        checkpoint.GeometryChanged += changed; checkpoint.TreeExiting += changed;
    }
    // =========================================================
    // Navigation/cost changes requeue existing shared work once; no new per-enemy A* queues.
    public void Refresh()
    { if (_revision != _nav.Revision || _costRevision != _nav.CostRevision) { _revision = _nav.Revision; _costRevision = _nav.CostRevision; Invalidate(); } }
    // =========================================================
    // Reuse storage across terrain edits; current cached comparisons become invalid immediately.
    private void Invalidate()
    {
        _pending.Clear();
        foreach (Plan plan in _plans) { plan.Sum = 0; plan.Pending = plan.Edges.Count; }
        foreach (Edge edge in _edges.Values)
        { edge.Complete = false; edge.Goal = 0; edge.BestCost = edge.Distance = float.PositiveInfinity; _pending.Enqueue(edge); }
    }
    // =========================================================
    // Perform at most one query. Canonical clearance-valid circle representatives make tails shareable.
    public bool Step()
    {
        if (_pending.Count == 0) return false;
        Edge edge = _pending.Peek();
        if (edge.Users.Count == 0 || !GodotObject.IsInstanceValid(edge.From)
            || (edge.To != null && !GodotObject.IsInstanceValid(edge.To))) { Complete(edge); return false; }
        Vector2[] starts = edge.From.Targets(_nav, edge.Radius);
        Vector2[] targets = edge.To == null ? null : edge.To.Targets(_nav, edge.Radius);
        var goals = _nav.GoalsFor(edge.Destination);
        int count = edge.Destination?.Available == true ? targets?.Length ?? goals.Count : 0;
        if (starts.Length == 0 || edge.Goal >= count) { Complete(edge); return false; }
        Vector2 start = starts[0]; float cost;
        Vector2[] points = targets != null
            ? CheckpointRoute.Find(_nav, start, targets[edge.Goal++], edge.Profile, edge.Radius, out cost)
            : _nav.FindPath(start, goals[edge.Goal++], edge.Profile, out cost);
        if (points.Length > 0 && cost < edge.BestCost && Clear(start, points, edge.Radius))
        {
            if (edge.To != null) points = CheckpointRoute.ToEntry(edge.To, start, points);
            float length = 0; Vector2 previous = start;
            foreach (Vector2 point in points) { length += previous.DistanceTo(point); previous = point; }
            edge.BestCost = cost; edge.Distance = length;
        }
        if (edge.Goal >= count) Complete(edge);
        return true;
    }
    // =========================================================
    // Estimates honor enemy clearance; an unavailable safe route is not an infinite Last candidate.
    private bool Clear(Vector2 from, Vector2[] points, float radius)
    { foreach (Vector2 to in points) { if (!_nav.ConnectionClear(from, to, radius)) return false; from = to; } return true; }
    private void Complete(Edge edge)
    { _pending.Dequeue(); edge.Complete = true; foreach (Plan plan in edge.Users) plan.Finish(edge.Distance); }
    // =========================================================
    // Scalar cached reads stay safe even before the next manager tick after a grid change.
    public bool IsCurrent => _revision == _nav.Revision && _costRevision == _nav.CostRevision;
    // =========================================================
    // World unload releases checkpoint subscriptions and all cached plan references.
    public void Dispose()
    {
        foreach (var pair in _observed) if (GodotObject.IsInstanceValid(pair.Key))
        { pair.Key.GeometryChanged -= pair.Value; pair.Key.TreeExiting -= pair.Value; }
        _observed.Clear(); _plans.Clear(); _edges.Clear(); _pending.Clear();
    }
}
