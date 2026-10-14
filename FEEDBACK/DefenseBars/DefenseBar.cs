// Lightweight reusable hull-health display for enemies and future damageable towers.
// Draws only on health changes; never polls health or runs its own Process callback.
using Godot;

public partial class DefenseBar : Node2D
{
    #region Inspector and State
    [Export] public DefenseBarConfig Style { get; set; }
    [Export] public FeedbackMasterConfig MasterConfig { get; set; }
    public float HealthFraction { get; private set; } = 1f;
    public float WidthPixels { get; private set; }
    #endregion

    #region Lifecycle and Health
    // =========================================================
    // A full-health or uninitialized bar is invisible; no per-bar ticking is required.
    public override void _Ready()
    {
        Visible = false;
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    // =========================================================
    // Health providers call this only when initialized, damaged or healed.
    // A dead or fully healed target has no bar; no timer-based disappearance.
    public void SetHealth(float current, float maximum)
    {
        if (Style == null) { Visible = false; return; }
        float safeMax = Mathf.Max(0f, maximum);
        HealthFraction = safeMax > 0f ? Mathf.Clamp(current / safeMax, 0f, 1f) : 0f;
        WidthPixels = CalculateWidth(safeMax);
        Visible = (MasterConfig == null || MasterConfig.DefenseBarsEnabled)
            && safeMax > 0f && current > 0f && HealthFraction < 1f;
        if (Visible) QueueRedraw();
    }

    // =========================================================
    // Per-enemy rotation cancellation keeps the bar horizontal and above its hull.
    // Call only during actual movement when the bar is visible; the bar doesn't tick.
    public void AlignWithOwner(float ownerRotation)
    {
        if (!Visible) return;
        Position = Vector2.Up.Rotated(-ownerRotation) * Style.VerticalOffsetPixels;
        Rotation = -ownerRotation;
    }
    #endregion

    #region Width and Rendering
    // =========================================================
    // Each doubling of max HP adds a small amount of width, up to a hard cap.
    public float CalculateWidth(float maximum)
    {
        if (Style == null) return 0f;
        float min = Mathf.Max(1f, Style.MinWidthPixels);
        float max = Mathf.Max(min, Style.MaxWidthPixels);
        float reference = Mathf.Max(1f, Style.ReferenceHealth);
        float doublings = Mathf.Log(Mathf.Max(1f, maximum) / reference) / Mathf.Log(2f);
        return Mathf.Clamp(min + Mathf.Max(0f, doublings) * Mathf.Max(0f, Style.PixelsPerHealthDoubling), min, max);
    }

    // =========================================================
    // A dark border/background and colored fill; rendered only after damage changes.
    public override void _Draw()
    {
        if (Style == null || !Visible) return;
        float w = WidthPixels, h = Mathf.Max(2f, Style.HeightPixels);
        float border = Mathf.Clamp(Style.BorderPixels, 0f, Mathf.Min(h, w) * 0.5f);
        Rect2 rect = new(-w * 0.5f, -h * 0.5f, w, h);
        DrawRect(rect, Style.BorderColor);
        Rect2 inner = rect.Grow(-border);
        DrawRect(inner, Style.BackgroundColor);
        if (HealthFraction <= 0f || inner.Size.X <= 0f || inner.Size.Y <= 0f) return;
        Color fill = HealthFraction <= Style.CriticalFraction ? Style.CriticalColor
            : HealthFraction <= Style.WarningFraction ? Style.WarningColor : Style.HealthyColor;
        DrawRect(new Rect2(inner.Position, new Vector2(inner.Size.X * HealthFraction, inner.Size.Y)), fill);
    }
    #endregion
}
