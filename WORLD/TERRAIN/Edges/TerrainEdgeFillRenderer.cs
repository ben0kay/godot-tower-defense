// One cached draw-command list per participating tile layer; no per-cell nodes or per-frame generation.
using Godot;
using System.Collections.Generic;

public partial class TerrainEdgeFillRenderer : Node2D
{
    #region Cached Quads
    private readonly record struct Stamp(Texture2D Texture, Rect2 Destination, Rect2 Source);
    private readonly List<Stamp> _stamps = new();
    public int StampCount => _stamps.Count;
    // =========================================================
    // Static geometry participates only in Godot's cached canvas drawing.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // Called during terrain generation only; Godot coalesces queued redraw requests.
    public void Add(Texture2D texture, Rect2 destination, Rect2 source)
    { _stamps.Add(new(texture, destination, source)); QueueRedraw(); }
    // =========================================================
    // Replay cached texture quads after native canvas invalidation, without reading terrain or generating masks.
    public override void _Draw()
    { foreach (Stamp stamp in _stamps) DrawTextureRectRegion(stamp.Texture, stamp.Destination, stamp.Source); }
    #endregion
}
