// One editor/F3 drawing owner for above-tile checkpoint circles, labels and authored connection arrows.
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class CheckpointDesignOverlay : Node2D
{
    [Export] public NodePath LevelPath { get; set; } = new("../..");
    [Export] public bool ShowInEditor { get; set; } = true;
    public int CircleCount => _checkpoints.Count;
    public int ConnectionCount => _links.Count;
    public int RefreshCount { get; private set; }
    private readonly List<RouteCheckpoint> _checkpoints = new();
    private readonly List<EnemyPortal> _portals = new();
    private readonly List<(Node2D From, RouteCheckpoint To, Color Color)> _links = new();
    private readonly Dictionary<RouteCheckpoint, Color> _colors = new();
    private readonly List<(Node2D Node, string Error)> _errors = new();
    private bool _runtimeVisible;
    private float _refreshTimer;
    private int _stamp;

    // =========================================================
    // Absolute z keeps markers above tiles independently of their scene-tree position/parent z.
    public override void _Ready()
    { ZIndex = 4090; ZAsRelative = false; SetProcess(Engine.IsEditorHint()); RefreshNow(); }

    // =========================================================
    // Runtime graph visualization exists only while the debug F3 panel is open.
    public void SetRuntimeVisible(bool visible)
    { _runtimeVisible = visible; SetProcess(Engine.IsEditorHint() || visible); RefreshNow(); QueueRedraw(); }

    // =========================================================
    // A single throttled authoring/debug scan; the level contains markers/tiles, never the enemy registry.
    public override void _Process(double delta)
    {
        _refreshTimer -= (float)delta;
        if (_refreshTimer > 0) return;
        _refreshTimer = 0.15f; RefreshNow();
    }

    // =========================================================
    // Rebuild cached draw data only when marker/link/transform/name or topology actually changes.
    public void RefreshNow()
    {
        Node root = GetNodeOrNull<Node>(LevelPath);
        if (root == null) return;
        List<RouteCheckpoint> checkpoints = new(); List<EnemyPortal> portals = new(); HashCode hash = new();
        Collect(root); hash.Add(ShowInEditor); hash.Add(_runtimeVisible); hash.Add(GlobalTransform);
        int stamp = hash.ToHashCode(); if (RefreshCount > 0 && stamp == _stamp) return;
        _stamp = stamp; RefreshCount++;
        foreach (RouteCheckpoint old in _checkpoints)
            if (GodotObject.IsInstanceValid(old) && !checkpoints.Contains(old)) { old.DrawnByDesignOverlay = false; old.QueueRedraw(); }
        _checkpoints.Clear(); _checkpoints.AddRange(checkpoints); _portals.Clear(); _portals.AddRange(portals);
        _links.Clear(); _colors.Clear(); _errors.Clear();
        int group = 0;
        foreach (EnemyPortal portal in _portals)
        {
            if (!portal.RequiresCheckpoint) continue;
            Color color = Color.FromHsv((group++ * 0.618034f) % 1, 0.6f, 1);
            RouteCheckpoint first = portal.ResolveCheckpoint();
            if (first == null) { _errors.Add((portal, "BROKEN CHECKPOINT LINK")); continue; }
            Node2D start = portal.GetNodeOrNull<Node2D>("SpawnExit") ?? portal; _links.Add((start, first, color));
            CheckpointNetwork graph = CheckpointNetwork.Capture(first);
            foreach (RouteCheckpoint node in graph.Nodes) if (!_colors.ContainsKey(node)) _colors[node] = color;
            if (graph.Error.Length > 0) _errors.Add((portal, graph.Error));
        }
        foreach (RouteCheckpoint checkpoint in _checkpoints)
        {
            checkpoint.DrawnByDesignOverlay = Engine.IsEditorHint(); checkpoint.QueueRedraw();
            Color color = ColorFor(checkpoint);
            if (checkpoint.NextCheckpoints != null)
                foreach (NodePath path in checkpoint.NextCheckpoints)
                {
                    RouteCheckpoint next = path == null || path.IsEmpty ? null : checkpoint.GetNodeOrNull<RouteCheckpoint>(path);
                    if (next == null) _errors.Add((checkpoint, "BROKEN NEXT LINK"));
                    else _links.Add((checkpoint, next, color));
                }
            if (Engine.IsEditorHint()) checkpoint.UpdateConfigurationWarnings();
        }
        QueueRedraw();
        void Collect(Node node)
        {
            if (node is RouteCheckpoint checkpoint) { checkpoints.Add(checkpoint); hash.Add(checkpoint.GetInstanceId()); hash.Add(checkpoint.DesignStamp); }
            if (node is EnemyPortal portal) { portals.Add(portal); hash.Add(portal.GetInstanceId()); hash.Add(portal.GlobalTransform); hash.Add(portal.CheckpointPath.ToString()); hash.Add(portal.Name.ToString());
                Node2D exit = portal.GetNodeOrNull<Node2D>("SpawnExit"); if (exit != null) hash.Add(exit.GlobalPosition); }
            foreach (Node child in node.GetChildren()) Collect(child);
        }
    }

    // =========================================================
    // Circles are faintly filled; arrows are guidance links, not computed navigation routes through obstacles.
    public override void _Draw()
    {
        if (!(Engine.IsEditorHint() ? ShowInEditor : _runtimeVisible)) return;
        foreach (var link in _links)
            if (GodotObject.IsInstanceValid(link.From) && GodotObject.IsInstanceValid(link.To))
                Arrow(link.From.GlobalPosition, link.To.GlobalPosition, link.From is RouteCheckpoint from ? from.RadiusPixels : 16, link.To.RadiusPixels, link.Color);
        foreach (RouteCheckpoint checkpoint in _checkpoints)
        {
            if (!GodotObject.IsInstanceValid(checkpoint)) continue;
            Color color = ColorFor(checkpoint); Vector2[] ring = new Vector2[65];
            for (int i = 0; i < ring.Length; i++) ring[i] = ToLocal(checkpoint.GlobalPosition + Vector2.FromAngle(i * Mathf.Tau / 64) * checkpoint.RadiusPixels);
            DrawColoredPolygon(ring, new Color(color, 0.07f)); DrawPolyline(ring, color, 2, true);
            Vector2 center = ToLocal(checkpoint.GlobalPosition); DrawCircle(center, 4, color);
            DrawString(ThemeDB.FallbackFont, center + new Vector2(8, -10), checkpoint.Name.ToString(), fontSize: 17, modulate: color);
        }
        foreach (var error in _errors)
            if (GodotObject.IsInstanceValid(error.Node)) DrawString(ThemeDB.FallbackFont, ToLocal(error.Node.GlobalPosition) + new Vector2(8, 26), error.Error, fontSize: 15, modulate: Colors.OrangeRed);
    }

    // =========================================================
    // End at region boundaries for readable arrows; overlapping circles fall back to center-to-center guidance.
    private void Arrow(Vector2 from, Vector2 to, float fromRadius, float toRadius, Color color)
    {
        Vector2 direction = (to - from).Normalized(); if (direction == Vector2.Zero) return;
        if (from.DistanceTo(to) > fromRadius + toRadius + 24) { from += direction * fromRadius; to -= direction * toRadius; }
        Vector2 a = ToLocal(from), b = ToLocal(to); DrawLine(a, b, new Color(color, 0.8f), 2, true);
        Vector2 wing = direction.Orthogonal() * 7;
        DrawColoredPolygon(new[] { b, ToLocal(to - direction * 18 + wing), ToLocal(to - direction * 18 - wing) }, color);
    }
    // =========================================================
    // Unlinked markers remain visible in the same purple design language.
    private Color ColorFor(RouteCheckpoint node) => _colors.TryGetValue(node, out Color color) ? color : new Color(0.75f, 0.4f, 1);
    // =========================================================
    // Restore standalone marker drawing when the shared owner is removed in the editor.
    public override void _ExitTree()
    { foreach (RouteCheckpoint node in _checkpoints) if (GodotObject.IsInstanceValid(node)) { node.DrawnByDesignOverlay = false; node.QueueRedraw(); } }
}
