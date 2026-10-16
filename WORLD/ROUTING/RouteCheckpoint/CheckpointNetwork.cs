// Immutable validated link snapshot; deterministic equal-probability choices never consume wave/spawn RNG.
using Godot;
using System;
using System.Collections.Generic;

public sealed class CheckpointNetwork
{
    public const int MaximumNodes = 256;
    public string Error { get; private set; } = "";
    public bool HasBranches { get; private set; }
    public readonly List<RouteCheckpoint> Nodes = new();
    private readonly Dictionary<RouteCheckpoint, RouteCheckpoint[]> _next = new();
    private readonly Dictionary<RouteCheckpoint, int> _stamps = new();

    // =========================================================
    // Capture only when spawning/planning/authoring changes, never per enemy physics tick.
    public static CheckpointNetwork Capture(RouteCheckpoint root)
    {
        CheckpointNetwork graph = new(); Dictionary<RouteCheckpoint, byte> state = new();
        Visit(root, 0); return graph;
        void Visit(RouteCheckpoint node, int depth)
        {
            if (graph.Error.Length > 0) return;
            if (!GodotObject.IsInstanceValid(node)) { graph.Error = "Required checkpoint link is missing or removed."; return; }
            if (state.TryGetValue(node, out byte color))
            { if (color == 1) graph.Error = "Checkpoint cycle detected at " + node.Name; return; }
            if (depth >= MaximumNodes || graph.Nodes.Count >= MaximumNodes)
            { graph.Error = "Checkpoint network exceeds the 256-node authoring limit."; return; }
            state[node] = 1; graph.Nodes.Add(node); graph._stamps[node] = node.DesignStamp;
            List<RouteCheckpoint> links = new(); HashSet<RouteCheckpoint> unique = new();
            if (node.NextCheckpoints != null)
                foreach (NodePath path in node.NextCheckpoints)
                {
                    RouteCheckpoint target = path == null || path.IsEmpty ? null : node.GetNodeOrNull<RouteCheckpoint>(path);
                    if (target == null) { graph.Error = "Broken Next Checkpoints link on " + node.Name; return; }
                    if (!unique.Add(target)) { graph.Error = "Duplicate checkpoint branch on " + node.Name; return; }
                    links.Add(target);
                }
            graph._next[node] = links.ToArray(); graph.HasBranches |= links.Count > 1;
            foreach (RouteCheckpoint target in links) Visit(target, depth + 1);
            state[node] = 2;
        }
    }

    // =========================================================
    // Pick once after physical entry; keeping the destination on EnemyMovement prevents rerolls on repath.
    public RouteCheckpoint Next(RouteCheckpoint reached, uint seed, int completedCount, out string error)
    {
        error = Error;
        if (error.Length > 0) return null;
        if (!_next.TryGetValue(reached, out RouteCheckpoint[] links)) { error = "Checkpoint is outside the captured network."; return null; }
        if (links.Length == 0) return null;
        RouteCheckpoint next = links[Choice(seed, completedCount, links.Length)];
        if (!GodotObject.IsInstanceValid(next)) { error = "Chosen required checkpoint was removed."; return null; }
        return next;
    }

    // =========================================================
    // Stable independent branch stream; names and physical path-profile IDs are not routing instructions.
    public static int Choice(uint seed, int completedCount, int count) => count <= 1 ? 0
        : (int)(CorridorPreference.Hash(seed ^ unchecked((uint)(completedCount + 1) * 0x9e3779b9u) ^ 0xb5297a4du) % (uint)count);

    // =========================================================
    // Preview/overlay invalidation only. Live enemies retain their captured links through retries and edits.
    public bool Unchanged()
    {
        foreach (RouteCheckpoint node in Nodes)
            if (!GodotObject.IsInstanceValid(node) || node.DesignStamp != _stamps[node]) return false;
        return true;
    }
}
