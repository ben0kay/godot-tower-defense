// Scene opt-in for one stationary native light. The world controller alone admits it to the light budget.
using Godot;

public partial class WorldLightSource : PointLight2D
{
    [Export] public LightSourceConfig Preset { get; set; }
    internal bool HasOutput => Preset is { Enabled: true, Energy: > 0, RadiusPixels: > 0 };

    // =========================================================
    // Reuse the world's shared falloff. Godot scales the full texture diameter.
    internal void Configure(Texture2D falloff)
    {
        Texture = falloff;
        TextureScale = 2 * Preset.RadiusPixels / falloff.GetWidth();
        Position = Preset.LocalOffset;
        Color = Preset.Tint;
        Energy = Preset.Energy;
        ShadowEnabled = false;
    }

    // =========================================================
    // A conservative screen AABB accounts for zoom, parent transforms and light overlap at viewport edges.
    internal bool OverlapsViewport(Rect2 viewport)
    {
        Transform2D transform = GetGlobalTransformWithCanvas();
        float radius = Preset.RadiusPixels;
        Vector2 extent = (transform.X.Abs() + transform.Y.Abs()) * radius;
        return viewport.Intersects(new Rect2(transform.Origin - extent, 2 * extent), true);
    }
}
