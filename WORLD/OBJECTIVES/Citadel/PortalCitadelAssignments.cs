// Setup-only neutral destination selection and cached radius-aware source validation; never per-enemy A*.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
public sealed class PortalCitadelAssignments
{
    #region Cached Portal Ownership
    private readonly NavigationGrid _nav;
    private readonly Dictionary<Node2D, CitadelDestination> _assigned = new();
    private readonly Dictionary<Node2D, CheckpointNetwork> _networks = new();
    private readonly Dictionary<(Node2D, float), bool> _clearance = new();
    private int _revision = -1;
    private readonly Dictionary<float, int[]> _components = new();
    public long SelectionQueries { get; private set; }
    public double SetupMilliseconds { get; private set; }
    // =========================================================
    // The spawning service owns this cache for exactly one world session.
    public PortalCitadelAssignments(NavigationGrid nav) => _nav = nav;
    // =========================================================
    // Single-objective fast path assigns directly with zero destination-comparison queries.
    public void Capture(IReadOnlyList<Node2D> portals, IReadOnlyList<Vector2> entries)
    {
        long started = Stopwatch.GetTimestamp(); _assigned.Clear(); _networks.Clear(); _clearance.Clear(); _components.Clear(); _revision = _nav.Revision;
        for (int i = 0; i < portals.Count; i++)
        {
            Node2D portal = portals[i]; var typed = portal as EnemyPortal;
            RouteCheckpoint checkpoint = typed?.ResolveCheckpoint();
            var network = typed?.RequiresCheckpoint == true || checkpoint != null ? CheckpointNetwork.Capture(checkpoint) : null;
            _networks[portal] = network;
            CitadelDestination chosen = _nav.SingleDestination;
            if (chosen == null)
            {
                float best = float.PositiveInfinity;
                foreach (var destination in _nav.Destinations)
                {
                    float length = SelectionLength(entries[i], checkpoint, network, destination);
                    if (length < best) { best = length; chosen = destination; }
                }
            }
            _assigned[portal] = chosen;
            if (chosen == null) GD.PushWarning("No valid constrained Citadel destination for portal " + portal.Name);
        }
        _revision = _nav.Revision; SetupMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }
    // =========================================================
    // No proximity checks, reassignment or scene scans during ordinary spawning.
    public CitadelDestination For(Node2D portal) => _assigned.GetValueOrDefault(portal);
    // =========================================================
    // Validate each distinct source/radius on a revision, not with an additional A* per spawned ship.
    public bool CanSpawn(Node2D portal, Vector2 position, float radius)
    {
        var destination = For(portal); if (destination?.Available != true) return false;
        var key = (portal, radius);
        int[] components = Connectivity(radius);
        if (_clearance.TryGetValue(key, out bool valid)) return valid;
        int component = _nav.ComponentAt(components, position);
        valid = GoalInComponent(_nav, destination, components, component);
        if (valid && portal is EnemyPortal typed && typed.RequiresCheckpoint)
            valid = CheckpointsInComponent(_nav, components, component, typed.ResolveCheckpoint(), _networks[portal], radius);
        _clearance[key] = valid; return valid;
    }
    // =========================================================
    // Reuse each radius map across portals and death releases until occupancy/terrain actually changes.
    internal int[] Connectivity(float radius)
    {
        if (_revision != _nav.Revision) { _clearance.Clear(); _components.Clear(); _revision = _nav.Revision; }
        if (!_components.TryGetValue(radius, out var labels)) _components[radius] = labels = _nav.ConnectedComponents(radius: radius);
        return labels;
    }
    // =========================================================
    // A portal/enemy must share one actual component with an assigned approach, not any other Citadel.
    internal static bool GoalInComponent(NavigationGrid nav, CitadelDestination destination, int[] labels, int component)
    {
        if (component <= 0 || destination?.Available != true) return false;
        foreach (var goal in nav.GoalsFor(destination))
            if (nav.ComponentAt(labels, nav.Grid.CellToWorldCenter(goal)) == component) return true;
        return false;
    }
    // =========================================================
    // All remaining captured branch regions must connect to this ship's component, even if other
    // approaches of the same Citadel are reachable from entirely different disconnected components.
    internal static bool CheckpointsInComponent(NavigationGrid nav, int[] labels, int component, RouteCheckpoint root,
        CheckpointNetwork network, float radius, Rect2I proposal = default)
    {
        if (component <= 0) return false;
        HashSet<RouteCheckpoint> nodes = new();
        if (network == null || !network.Remaining(root, nodes)) return false;
        foreach (var node in nodes)
        {
            bool valid = false;
            foreach (Vector2 target in node.Targets(nav, radius))
                if (nav.ComponentAt(labels, target) == component && nav.HasClearance(target, radius, proposal)) { valid = true; break; }
            if (!valid) return false;
        }
        return true;
    }

    #endregion

    #region Neutral Setup Comparison
    // =========================================================
    // Selection policy: cell-center clearance (radius zero), canonical checkpoint representatives,
    // minimum worst-branch neutral journey. All branches must connect; ties retain authored Citadel order.
    private float SelectionLength(Vector2 origin, RouteCheckpoint root, CheckpointNetwork network, CitadelDestination destination)
    {
        int[] labels = Connectivity(0); int component = _nav.ComponentAt(labels, origin);
        if (!GoalInComponent(_nav, destination, labels, component)) return float.PositiveInfinity;
        if (network == null) return Leg(origin, null, destination);
        if (!CheckpointsInComponent(_nav, labels, component, root, network, 0)) return float.PositiveInfinity;
        Dictionary<RouteCheckpoint, Vector2> representatives = new();
        foreach (var node in network.Nodes)
            foreach (Vector2 target in node.Targets(_nav, 0))
                if (_nav.ComponentAt(labels, target) == component) { representatives[node] = target; break; }
        Dictionary<RouteCheckpoint, float> tails = new();
        float Tail(RouteCheckpoint node)
        {
            if (tails.TryGetValue(node, out float cached)) return cached;
            if (!representatives.TryGetValue(node, out Vector2 representative)) return float.PositiveInfinity;
            var links = network.Successors(node); float worst = 0;
            if (links.Count == 0) worst = Leg(representative, null, destination);
            else foreach (var next in links) worst = Math.Max(worst, Leg(representative, representatives[next], destination) + Tail(next));
            tails[node] = worst; return worst;
        }
        return Leg(origin, representatives[root], destination) + Tail(root);
    }
    // =========================================================
    // Reuse the shared neutral grid; distance is physical cell-path length, never weighted diversity cost.
    private float Leg(Vector2 origin, Vector2? target, CitadelDestination destination)
    {
        float best = float.PositiveInfinity;
        if (target.HasValue) Query(_nav.Grid.WorldToCell(target.Value));
        else foreach (var goal in _nav.GoalsFor(destination)) Query(goal);
        return best;
        void Query(Vector2I goal)
        {
            var route = _nav.FindPath(origin, goal, NavigationGrid.NeutralProfile, out _); SelectionQueries++;
            if (route.Length == 0) return;
            float length = 0; Vector2 previous = origin;
            foreach (var point in route) { length += previous.DistanceTo(point); previous = point; }
            best = Math.Min(best, length);
        }
    }
    #endregion
}
