// Visual-only placement ghost that mirrors completed Level 1 tower scene artwork, not its catalog icon.
using Godot;

public partial class TowerPlacementGhost : Node2D
{
    private TowerDefinition _shown;

    // =========================================================
    // Build an inert visual copy only when the chosen tower changes; gameplay nodes never enter the tree.
    public void LoadFinishedLevelOne(TowerDefinition definition)
    {
        if (_shown == definition) return;
        foreach (Node child in GetChildren()) child.Free();
        _shown = definition;
        if (definition?.Scene == null) return;

        Node source = definition.Scene.Instantiate();
        try
        {
            if (source is Node2D visualRoot)
                CopyVisibleArt(visualRoot, visualRoot, definition, Transform2D.Identity, Colors.White, 0);
        }
        finally { source.Free(); }
    }

    // =========================================================
    // Flatten visible scene sprites into a few inert preview layers with the authored transforms and draw order.
    private void CopyVisibleArt(Node node, Node2D root, TowerDefinition definition,
        Transform2D parentTransform, Color inheritedTint, int parentZ)
    {
        if (node is CanvasItem canvas && !canvas.Visible) return;
        Transform2D transform = parentTransform;
        Color tint = inheritedTint;
        int z = parentZ;
        if (node is CanvasItem item)
        {
            tint *= item.Modulate;
            z = item.ZAsRelative ? parentZ + item.ZIndex : item.ZIndex;
        }
        // Placement supplies the root's world position; only its children's authored transforms matter.
        if (node is Node2D visual && node != root) transform *= visual.Transform;

        if (node is Sprite2D original && original.Texture != null)
        {
            Sprite2D copy = new()
            {
                Name = "FinishedLayer",
                Texture = FinishedTexture(original, definition),
                Transform = transform,
                Centered = original.Centered,
                Offset = original.Offset,
                FlipH = original.FlipH,
                FlipV = original.FlipV,
                Hframes = original.Hframes,
                Vframes = original.Vframes,
                Frame = original.Frame,
                RegionEnabled = original.RegionEnabled,
                RegionRect = original.RegionRect,
                NormalMap = original.NormalMap,
                TextureFilter = original.TextureFilter,
                Material = original.Material,
                Modulate = tint,
                SelfModulate = original.SelfModulate,
                ZIndex = z,
                ZAsRelative = false
            };
            AddChild(copy);
        }
        foreach (Node child in node.GetChildren())
            CopyVisibleArt(child, root, definition, transform, tint, z);
    }

    // =========================================================
    // Follow any optional Level 1 finished-art override configured on the actual tower resource.
    private static Texture2D FinishedTexture(Sprite2D sprite, TowerDefinition definition)
    {
        TowerConstructionArt art = definition.DefaultConfig?.ConstructionArt;
        if (art == null) return sprite.Texture;
        string name = sprite.Name.ToString();
        if (name == "BaseSprite") return art.FinishedBaseTexture(1, sprite.Texture);
        if (name == "Sprite" && sprite.GetParent()?.Name.ToString() == "RecoilRoot")
            return art.FinishedGunTexture(1, sprite.Texture);
        if (name == "Sprite" && sprite.GetParent() is TowerBase)
            return art.FinishedTexture(1, sprite.Texture);
        return sprite.Texture;
    }
}
