// Lightweight world-space radius indicator for opt-in enemy shield emitters; no per-frame visual updates.
using Godot;

public partial class ShieldEmitterRangeVisual : Node2D
{
    [ExportGroup("Shield Range Appearance")]
    [Export] public Color FillColor { get; set; } = new(0.62f, 0.38f, 0.96f, 0.018f);
    [Export] public Color EdgeColor { get; set; } = new(0.70f, 0.50f, 1f, 0.24f);
    [Export(PropertyHint.Range, "0.5,4,0.25")] public float EdgeWidth { get; set; } = 1.5f;

    public float RadiusPixels { get; private set; }

    // =========================================================
    // The ability owns visibility and range; this drawing node owns no combat state or timers.
    public override void _Ready()
    {
        Visible = false;
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    // =========================================================
    // Changing the Inspector-configured ability radius rebuilds geometry once, never each frame.
    public void SetRadius(float radius)
    {
        float next = float.IsFinite(radius) && radius > 0 ? radius : 0f;
        if (Mathf.IsEqualApprox(RadiusPixels, next)) return;
        RadiusPixels = next;
        QueueRedraw();
    }

    // =========================================================
    // Dormant, disabled, depleted or dead emitters should not advertise an active support radius.
    public void SetActive(bool active)
    {
        bool show = active && RadiusPixels > 0;
        if (Visible != show) Visible = show;
    }

    // =========================================================
    // Drawn in the emitter's local/world space, not the UI; the parent follows ship movement.
    public override void _Draw()
    {
        if (RadiusPixels <= 0) return;
        DrawCircle(Vector2.Zero, RadiusPixels, FillColor);
        int segments = Mathf.Clamp(Mathf.CeilToInt(RadiusPixels / 2f), 64, 192);
        DrawArc(Vector2.Zero, RadiusPixels, 0f, Mathf.Tau, segments, EdgeColor, EdgeWidth, true);
    }
}
