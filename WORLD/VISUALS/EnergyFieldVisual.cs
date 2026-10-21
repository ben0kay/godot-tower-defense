// Reusable VE2 overlay shared by portals and Citadels. Animation is GPU-only at idle.
// Spawn pulse is optional feedback and never touches spawning, navigation, health or Credits.
using Godot;

public partial class EnergyFieldVisual : ColorRect
{
    #region Configuration
    [Export] public EnergyFieldStyle Style { get; set; }
    private ShaderMaterial _ownMaterial;
    private Tween _pulseTween;
    #endregion

    #region Lifecycle
    // =========================================================
    // Clone the material once so per-instance tweaks cannot bleed across multiple Citadels/portals.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        MouseFilter = MouseFilterEnum.Ignore;
        if (Style == null || !Style.Enabled) { Visible = false; return; }
        if (Material is not ShaderMaterial source) { Visible = false; return; }
        _ownMaterial = (ShaderMaterial)source.Duplicate();
        Material = _ownMaterial;
        _ownMaterial.SetShaderParameter("field_mode", (int)Style.Kind);
        _ownMaterial.SetShaderParameter("energy_tint", Style.Tint);
        _ownMaterial.SetShaderParameter("idle_opacity", Mathf.Clamp(Style.Opacity, 0f, 1f));
        _ownMaterial.SetShaderParameter("animation_speed", Mathf.Clamp(Style.AnimationSpeed, 0f, 3f));
        _ownMaterial.SetShaderParameter("glow_width", Mathf.Clamp(Style.GlowWidth, .01f, .5f));
        Modulate = Colors.White;
    }
    #endregion

    #region Spawn Response
    // =========================================================
    // Briefly brighten only this portal after successful spawn; no background tick.
    public void PlaySpawnPulse()
    {
        if (!Visible || Style == null || !Style.Enabled || _ownMaterial == null) return;
        if (_pulseTween != null && _pulseTween.IsValid()) _pulseTween.Kill();
        float start = Mathf.Clamp(1f + Style.PulseExtraOpacity, 1f, 2f);
        Modulate = new Color(start, start, start, 1f);
        _pulseTween = CreateTween();
        _pulseTween.TweenProperty(this, "modulate", Colors.White,
            Mathf.Clamp(Style.PulseDuration, .05f, 2f)).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    // =========================================================
    // Clear the only active event-driven animation on scene removal.
    public override void _ExitTree()
    {
        if (_pulseTween != null && _pulseTween.IsValid()) _pulseTween.Kill();
    }
    #endregion
}
