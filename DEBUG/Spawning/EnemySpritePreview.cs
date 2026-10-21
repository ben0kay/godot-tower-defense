// Static 1:1 artwork preview: copies only sprite presentation from an off-tree enemy, never gameplay nodes.
using Godot;

public partial class EnemySpritePreview : Control
{
    #region Artwork
    public Rect2 ArtworkBounds { get; private set; }
    public int SpriteCount => _art?.GetChildCount() ?? 0;
    private Node2D _art;
    private bool _hasBounds;
    // =========================================================
    // Geometry only recenters on resize; there is no per-frame preview logic.
    public override void _Ready()
    { MouseFilter = MouseFilterEnum.Ignore; Resized += CenterArtwork; SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // Inspect the actual scene without entering the tree, then immediately release its gameplay wrappers.
    public void ShowScene(PackedScene scene)
    {
        if (_art != null) { RemoveChild(_art); _art.Free(); }
        _art = new() { Name = "Artwork" }; AddChild(_art); _hasBounds = false; ArtworkBounds = default;
        if (scene != null)
        {
            Node source = scene.Instantiate();
            try
            {
                if (source is Node2D root && root.GetNodeOrNull<Node2D>("Sprite") is Node2D sprite)
                    CopyArtwork(sprite, root.Transform, root.Modulate, root.Visible, root.ZIndex);
            }
            finally { source.Free(); }
        }
        CustomMinimumSize = new(Mathf.Max(340, ArtworkBounds.Size.X + 32), Mathf.Max(300, ArtworkBounds.Size.Y + 32));
        CenterArtwork();
    }
    // =========================================================
    // Preserve transforms, atlas frames, offsets, tint and separate gun layers; no attached script is copied.
    private void CopyArtwork(Node source, Transform2D parent, Color tint, bool visible, int z)
    {
        Transform2D transform = parent;
        if (source is Node2D node) { transform *= node.Transform; z = node.ZAsRelative ? z + node.ZIndex : node.ZIndex; }
        if (source is CanvasItem item) { visible &= item.Visible; tint *= item.Modulate; }
        if (!visible) return;
        if (source is Sprite2D sprite && sprite.Texture != null)
        {
            Sprite2D copy = new() { Texture = sprite.Texture, Centered = sprite.Centered, Offset = sprite.Offset,
                FlipH = sprite.FlipH, FlipV = sprite.FlipV, Hframes = sprite.Hframes, Vframes = sprite.Vframes,
                Frame = sprite.Frame, RegionEnabled = sprite.RegionEnabled, RegionRect = sprite.RegionRect,
                RegionFilterClipEnabled = sprite.RegionFilterClipEnabled, Transform = transform,
                Modulate = tint * sprite.SelfModulate, ZIndex = z, Material = sprite.Material, TextureFilter = sprite.TextureFilter };
            _art.AddChild(copy);
            Rect2 rect = sprite.GetRect();
            foreach (Vector2 point in new[] { rect.Position, rect.Position + new Vector2(rect.Size.X, 0), rect.End, rect.Position + new Vector2(0, rect.Size.Y) })
            {
                Vector2 transformed = transform * point;
                if (!_hasBounds) { ArtworkBounds = new(transformed, Vector2.Zero); _hasBounds = true; }
                else ArtworkBounds = ArtworkBounds.Expand(transformed);
            }
        }
        foreach (Node child in source.GetChildren()) CopyArtwork(child, transform, tint, visible, z);
    }
    // =========================================================
    // Only translation changes; artwork retains its authored scale even in the scrollable preview area.
    private void CenterArtwork()
    {
        if (_art == null) return;
        Vector2 area = new(Mathf.Max(Size.X, CustomMinimumSize.X), Mathf.Max(Size.Y, CustomMinimumSize.Y));
        _art.Position = (area - ArtworkBounds.Size) * .5f - ArtworkBounds.Position;
    }
    #endregion
}
