// Stable session destination identity; cached approach cells share the existing navigation/profile pool.
using Godot;
using System.Collections.Generic;
public sealed class CitadelDestination : System.IDisposable
{
    #region Identity and Cached Goals
    public string Id { get; }
    public int Number { get; }
    public string DisplayName => $"CITADEL {Number}";
    public Color DisplayColor => Color.FromHsv(((Number - 1) * .618034f + .5f) % 1, .65f, 1);
    public Node2D Root { get; }
    public CitadelHealth Health { get; }
    internal bool Registered = true;
    private bool _live, _disposed;
    public bool Available => Registered && _live;
    public readonly List<Vector2I> Goals = new();
    // =========================================================
    // Relative scene paths remain stable for authoring; enemies retain this object across occupancy edits.
    internal CitadelDestination(WorldLevel level, Node2D root)
    {
        for (int i = 0; i < level.Citadels.Count; i++) if (level.Citadels[i] == root) { Number = i + 1; break; }
        Root = root; Id = level.GetPathTo(root).ToString(); Health = root.GetNode<CitadelHealth>("Health");
        _live = root.IsInsideTree() && Health.IsInsideTree();
        Root.TreeExiting += Retire; Health.TreeExiting += Retire;
    }
    // =========================================================
    // Cached lifecycle availability keeps enemy ticks and tower scoring free of native objective scans.
    private void Retire() => Dispose();
    // =========================================================
    // Preview pools and level teardown release subscriptions; a removed identity never revives or redirects.
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _live = false;
        if (GodotObject.IsInstanceValid(Root)) Root.TreeExiting -= Retire;
        if (GodotObject.IsInstanceValid(Health)) Health.TreeExiting -= Retire;
    }
    #endregion
}
