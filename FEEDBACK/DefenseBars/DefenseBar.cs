// Reusable event-driven hull + shield display for enemies and future damageable towers.
// Uses one canvas node for both stacked meters; no per-frame processing or extra scene nodes.
using Godot;

public partial class DefenseBar : Node2D
{
    #region Inspector and State
    [Export] public DefenseBarConfig Style { get; set; }
    [Export] public FeedbackMasterConfig MasterConfig { get; set; }
    public float HealthFraction { get; private set; } = 1f;
    public float ShieldFraction { get; private set; }
    public bool ShieldVisible { get; private set; }
    public bool HullVisible { get; private set; }
    public float WidthPixels { get; private set; }
    private bool _hullAlive;
    #endregion

    #region Lifecycle and State Changes
    // =========================================================
    // Rendering is updated by owner events only; the shield does not introduce a tick.
    public override void _Ready()
    {
        Visible = false;
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    // =========================================================
    // The hull row remains visible alongside a live shield, even at full health.
    // Unshielded ships retain the former damaged-hull-only behavior.
    public void SetHealth(float current, float maximum)
    {
        float safeMax = float.IsFinite(maximum) ? Mathf.Max(0f, maximum) : 0f;
        float safeCurrent = float.IsFinite(current) ? Mathf.Max(0f, current) : 0f;
        HealthFraction = safeMax > 0f ? Mathf.Clamp(safeCurrent / safeMax, 0f, 1f) : 0f;
        _hullAlive = safeMax > 0f && safeCurrent > 0f;
        WidthPixels = CalculateWidth(safeMax);
        RefreshVisibility();
    }

    // =========================================================
    // Called on shielded spawn, shield damage, restoration or depletion, not each frame.
    // The shield row sits ABOVE the existing hull row and uses the same width.
    public void SetShield(float current, float maximum)
    {
        float safeMax = float.IsFinite(maximum) ? Mathf.Max(0f, maximum) : 0f;
        float safeCurrent = float.IsFinite(current) ? Mathf.Max(0f, current) : 0f;
        ShieldFraction = safeMax > 0f ? Mathf.Clamp(safeCurrent / safeMax, 0f, 1f) : 0f;
        RefreshVisibility();
    }

    // =========================================================
    // Reuse the owner's existing movement callback for counter-rotation and world position.
    public void AlignWithOwner(float ownerRotation)
    {
        if (!Visible || Style == null) return;
        Position = Vector2.Up.Rotated(-ownerRotation) * Style.VerticalOffsetPixels;
        Rotation = -ownerRotation;
    }

    // =========================================================
    // Respect the shared feedback switch for both meters.
    private void RefreshVisibility()
    {
        ShieldVisible = _hullAlive && ShieldFraction > 0f;
        HullVisible = _hullAlive && (HealthFraction < 1f || ShieldVisible);
        Visible = Style != null && (MasterConfig == null || MasterConfig.DefenseBarsEnabled)
            && (HullVisible || ShieldVisible);
        if (Visible) QueueRedraw();
    }
    #endregion

    #region Width and Rendering
    // =========================================================
    // Keep existing logarithmic HP sizing; the shield meter shares hull width for alignment.
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
    // One canvas draw draws the hull meter and (when present) a slimmer shield meter above.
    public override void _Draw()
    {
        if (Style == null || !Visible) return;
        float hullHeight = Mathf.Max(2f, Style.HeightPixels);
        if (HullVisible)
        {
            Color hull = HealthFraction <= Style.CriticalFraction ? Style.CriticalColor
                : HealthFraction <= Style.WarningFraction ? Style.WarningColor : Style.HealthyColor;
            DrawMeter(0f, hullHeight, HealthFraction, hull);
        }
        if (ShieldVisible)
        {
            float height = Mathf.Max(2f, Style.ShieldHeightPixels);
            float gap = Mathf.Max(0f, Style.ShieldGapPixels);
            float y = -(hullHeight + height) * 0.5f - gap;
            DrawMeter(y, height, ShieldFraction, Style.ShieldColor);
        }
    }

    // =========================================================
    // Both meters use the same existing border/background and one reusable drawing helper.
    private void DrawMeter(float y, float height, float fraction, Color fill)
    {
        float width = WidthPixels;
        if (width <= 0f) return;
        float border = Mathf.Clamp(Style.BorderPixels, 0f, Mathf.Min(height, width) * 0.5f);
        Rect2 rect = new(-width * 0.5f, y - height * 0.5f, width, height);
        DrawRect(rect, Style.BorderColor);
        Rect2 inner = rect.Grow(-border);
        DrawRect(inner, Style.BackgroundColor);
        if (fraction > 0f && inner.Size.X > 0f && inner.Size.Y > 0f)
            DrawRect(new Rect2(inner.Position, new Vector2(inner.Size.X * fraction, inner.Size.Y)), fill);
    }
    #endregion
}
