// Reusable event-driven hull + shield display for enemies and future damageable towers.
// Uses one canvas node for both stacked meters; no per-frame processing or extra scene nodes.
using Godot;

public partial class DefenseBar : Node2D
{
    #region Inspector and State
    private DefenseBarConfig _style;
    [Export] public DefenseBarConfig Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            _style = value;
            _widthDirty = _layoutDirty = true;
            if (IsInsideTree()) RefreshVisibility(true);
        }
    }
    [Export] public FeedbackMasterConfig MasterConfig { get; set; }
    public float HealthFraction { get; private set; } = 1f;
    public float ShieldFraction { get; private set; }
    public bool ShieldVisible { get; private set; }
    public bool HullVisible { get; private set; }
    public float WidthPixels
    {
        get
        {
            if (_widthDirty)
            {
                _width = CalculateWidth(_maxHealth);
                _widthDirty = false;
            }
            return _width;
        }
    }
    private float _maxHealth, _width, _offset;
    private float _alignedAngle = float.NaN;
    private Rect2 _hullRect, _hullInner, _shieldRect, _shieldInner;
    private bool _widthDirty, _layoutDirty = true, _fogConcealed, _redrawQueued;
    private bool _hullAlive;
    private float _ownerHalfHeight;
    private float _ownerVisualScale = 1f;
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
        float fraction = safeMax > 0f ? Mathf.Clamp(safeCurrent / safeMax, 0f, 1f) : 0f;
        bool alive = safeMax > 0f && safeCurrent > 0f;
        bool widthChanged = _maxHealth != safeMax;
        bool changed = widthChanged || HealthFraction != fraction || _hullAlive != alive;
        if (widthChanged)
        {
            _maxHealth = safeMax;
            _widthDirty = _layoutDirty = true;
        }
        if (HealthFraction != fraction) HealthFraction = fraction;
        _hullAlive = alive;
        RefreshVisibility(changed);
    }

    // =========================================================
    // Called on shielded spawn, shield damage, restoration or depletion, not each frame.
    // The shield row sits ABOVE the existing hull row and uses the same width.
    public void SetShield(float current, float maximum)
    {
        float safeMax = float.IsFinite(maximum) ? Mathf.Max(0f, maximum) : 0f;
        float safeCurrent = float.IsFinite(current) ? Mathf.Max(0f, current) : 0f;
        float fraction = safeMax > 0f ? Mathf.Clamp(safeCurrent / safeMax, 0f, 1f) : 0f;
        bool changed = ShieldFraction != fraction;
        if (changed) ShieldFraction = fraction;
        RefreshVisibility(changed);
    }

    // =========================================================
    // A large sprite can exceed the default bar offset; cache its height once at spawn.
    // Small ships keep their existing Inspector-authored minimum vertical offset.
    public void SetOwnerVisualHeight(float height, float visualScale = 1f)
    {
        float halfHeight = float.IsFinite(height) ? Mathf.Max(0f, height) * 0.5f : 0f;
        if (_ownerHalfHeight == halfHeight && _ownerVisualScale == visualScale) return;
        _ownerHalfHeight = halfHeight;
        _ownerVisualScale = visualScale;
        _layoutDirty = true;
    }

    // =========================================================
    // Reuse the owner's movement callback for counter-rotation and world position.
    public void AlignWithOwner(float ownerRotation)
    {
        if (_fogConcealed || !Visible || Style == null) return;
        EnsureLayout();
        float angle = -ownerRotation;
        if (_alignedAngle == angle) return;
        _alignedAngle = angle;
        Vector2 position = Vector2.Up.Rotated(angle) * _offset;
        if (Position != position) Position = position;
        if (Rotation != angle) Rotation = angle;
    }

    // =========================================================
    // Logical meter state stays current in fog. A frozen owner realigns on reveal too.
    internal void SetFogConcealed(bool concealed)
    {
        if (_fogConcealed == concealed) return;
        _fogConcealed = concealed;
        if (concealed)
        {
            if (Visible) Visible = false;
            _redrawQueued = false;
            return;
        }
        RefreshVisibility(true);
    }

    private void RefreshVisibility(bool changed)
    {
        ShieldVisible = _hullAlive && ShieldFraction > 0f;
        HullVisible = _hullAlive && (HealthFraction < 1f || ShieldVisible);
        if (_fogConcealed) return;
        bool wasVisible = Visible;
        bool show = Style != null && (MasterConfig == null || MasterConfig.DefenseBarsEnabled)
            && (HullVisible || ShieldVisible);
        if (wasVisible != show) Visible = show;
        if (!show)
        {
            _redrawQueued = false;
            return;
        }
        if ((!wasVisible || _layoutDirty) && GetParent() is Node2D owner) AlignWithOwner(owner.Rotation);
        if ((changed || !wasVisible) && !_redrawQueued)
        {
            _redrawQueued = true;
            QueueRedraw();
        }
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
    // Layout changes with maximum HP, authored style or owner size, independently of fill.
    private void EnsureLayout()
    {
        if (!_layoutDirty) return;
        _layoutDirty = false;
        _alignedAngle = float.NaN;
        float width = WidthPixels;
        float hullHeight = Mathf.Max(2f, Style.HeightPixels);
        float shieldHeight = Mathf.Max(2f, Style.ShieldHeightPixels);
        float gap = Mathf.Max(0f, Style.ShieldGapPixels);
        _hullRect = new Rect2(-width * .5f, -hullHeight * .5f, width, hullHeight);
        _shieldRect = new Rect2(-width * .5f, -hullHeight * .5f - shieldHeight - gap, width, shieldHeight);
        _hullInner = _hullRect.Grow(-Mathf.Clamp(Style.BorderPixels, 0f, Mathf.Min(hullHeight, width) * .5f));
        _shieldInner = _shieldRect.Grow(-Mathf.Clamp(Style.BorderPixels, 0f, Mathf.Min(shieldHeight, width) * .5f));
        _offset = Mathf.Max(Style.VerticalOffsetPixels, _ownerHalfHeight
            + Mathf.Max(Style.HeightPixels, Style.ShieldHeightPixels) + Style.ShieldGapPixels + 2f) * _ownerVisualScale;
    }

    // =========================================================
    // One canvas draw draws the hull meter and (when present) a slimmer shield meter above.
    public override void _Draw()
    {
        _redrawQueued = false;
        if (_fogConcealed || Style == null || !Visible) return;
        EnsureLayout();
        if (_width <= 0f) return;
        if (HullVisible)
        {
            Color hull = HealthFraction <= Style.CriticalFraction ? Style.CriticalColor
                : HealthFraction <= Style.WarningFraction ? Style.WarningColor : Style.HealthyColor;
            DrawMeter(_hullRect, _hullInner, HealthFraction, hull);
        }
        if (ShieldVisible) DrawMeter(_shieldRect, _shieldInner, ShieldFraction, Style.ShieldColor);
    }

    private void DrawMeter(Rect2 rect, Rect2 inner, float fraction, Color fill)
    {
        DrawRect(rect, Style.BorderColor);
        DrawRect(inner, Style.BackgroundColor);
        if (fraction > 0f && inner.Size.X > 0f && inner.Size.Y > 0f)
            DrawRect(new Rect2(inner.Position, new Vector2(inner.Size.X * fraction, inner.Size.Y)), fill);
    }
    #endregion
}
