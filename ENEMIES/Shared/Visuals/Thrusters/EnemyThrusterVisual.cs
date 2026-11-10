// ET1: one rear-facing ignition for all +X enemy hull sprites, with an optional capped GPU exhaust.
// Flame width follows hull height; flame length combines hull mass impression and authored speed.
// Status/stealth changes are event-driven, so there's no extra per-enemy tick.
using Godot;

public partial class EnemyThrusterVisual : Node2D
{
    #region Shared Configuration and State
    [Export] public EnemyThrusterConfig Config { get; set; }
    private Sprite2D _flame;
    private GpuParticles2D _exhaust;
    private VisibleOnScreenNotifier2D _screen;
    private float _nominalSpeed, _baseFlameLength, _flameWidth, _rearX, _rearY;
    private Transform2D _authoredTransform;
    private bool _authoredLocalExhaust;
    private float _lastSpeedFraction = -1f;
    private bool _configured, _onScreen, _hasGpuSlot, _stealthed, _fogConcealed;
    private bool _restartExhaust;
    private static int _activeGpuEmitters;

    public static int ActiveGpuEmitters => _activeGpuEmitters;
    public bool HasGpuSlot => _hasGpuSlot;
    public Vector2 FlameDimensions => new Vector2(_baseFlameLength, _flameWidth) * Scale.Abs();
    #endregion

    #region Setup
    // =========================================================
    // Rendered hull height sets width; both overall hull size and authored speed add to length.
    // This makes slow capital ships burn broadly AND substantially, while fast scouts stay thin.
    internal static Vector2 ComputeFlameDimensions(Vector2 hull, float moveSpeed, EnemyThrusterConfig settings)
    {
        float shipSize = Mathf.Sqrt(Mathf.Max(0f, hull.X * hull.Y));
        float width = Mathf.Clamp(hull.Y * (settings?.FlameWidthToHullHeight ?? .17f), 2.4f, 24f);
        float length = Mathf.Clamp((settings?.BaseFlameLengthPixels ?? 4f) +
            shipSize * (settings?.LengthPerHullSize ?? .12f) +
            Mathf.Max(0f, moveSpeed) * (settings?.LengthPerSpeed ?? .10f),
            5f, Mathf.Max(5f, settings?.MaxFlameLengthPixels ?? 55f));
        return new Vector2(length, width);
    }

    // =========================================================
    // Scene node setup remains dormant until the authoritative Enemy.Initialize assigns a hull.
    public override void _Ready()
    {
        _flame = GetNode<Sprite2D>("Flame");
        _exhaust = GetNode<GpuParticles2D>("Exhaust");
        _screen = GetNode<VisibleOnScreenNotifier2D>("OnScreen");
        _authoredTransform = Transform;
        _authoredLocalExhaust = _exhaust.LocalCoords;
        _flame.Visible = false;
        _exhaust.Emitting = false;
        _screen.ScreenEntered += OnScreenEntered;
        _screen.ScreenExited += OnScreenExited;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Called once per enemy spawn; no per-frame measure or generated per-ship shader/material.
    public void Configure(Sprite2D hull, EnemyDefinition definition, float visualScale = 1f)
    {
        Stop();
        _configured = false;
        _flame.Visible = false;
        if (Config?.Enabled != true || definition?.ThrusterEnabled != true ||
            hull?.Texture == null) return;

        // All authored engine geometry lives in 1x hull space. Scale the existing branch once.
        Transform = _authoredTransform.Scaled(Vector2.One * visualScale);
        bool localExhaust = _authoredLocalExhaust || visualScale != 1f;
        // Scaled exhaust uses local coordinates so Godot scales both the particle quads and their travel.
        // Keep the authored world-space trail for ships using the original size.
        if (_exhaust.LocalCoords != localExhaust) _exhaust.LocalCoords = localExhaust;
        Vector2 hullScale = hull.Scale / visualScale;
        Vector2 hullPosition = hull.Position / visualScale;
        Vector2 bounds = hull.GetRect().Size * hullScale.Abs();
        if (bounds.X <= 0 || bounds.Y <= 0 || !float.IsFinite(definition.MoveSpeed)) return;
        Vector2 dimensions = ComputeFlameDimensions(bounds, definition.MoveSpeed, Config);
        _nominalSpeed = Mathf.Max(1f, definition.MoveSpeed);
        _baseFlameLength = dimensions.X;
        _flameWidth = dimensions.Y;

        // Right-facing (+X) sprites burn to the left (-X). A per-definition local mount offset
        // is available for artwork whose nozzle is not exactly rear-center.
        _rearX = hullPosition.X + hull.GetRect().Position.X * hullScale.X +
            Config.RearNozzleInsetPixels + definition.ThrusterMountOffset.X;
        _rearY = hullPosition.Y + definition.ThrusterMountOffset.Y;
        _exhaust.Position = new Vector2(_rearX - 1f, _rearY);
        _exhaust.Amount = Mathf.Clamp(Config.ParticlesPerShip, 1, 8);

        // The notification rectangle includes both the hull and its tail.
        float left = _rearX - _baseFlameLength - 12f;
        float right = hullPosition.X + bounds.X * .5f + 8f;
        float halfHeight = Mathf.Max(bounds.Y, _flameWidth * 2f) * .5f + 12f;
        _screen.Rect = new Rect2(left, _rearY - halfHeight, Mathf.Max(1f, right - left), halfHeight * 2f);
        _onScreen = _screen.IsOnScreen();
        _configured = true;
        _fogConcealed = false;
        _exhaust.Visible = true;
        _lastSpeedFraction = -1f;
        _stealthed = false;
        SetEffectiveSpeed(_nominalSpeed);
    }
    #endregion

    #region Active Presentation
    // =========================================================
    // Caller only notifies this on spawn or actual Slow/Freeze change; no extra enemy update loop.
    // Quantize ratio changes to avoid redundant CanvasItem/GPU property writes.
    public void SetEffectiveSpeed(float speed)
    {
        if (!_configured) return;
        float fraction = float.IsFinite(speed) ? Mathf.Clamp(speed / _nominalSpeed, 0f, 1.2f) : 0f;
        if (Mathf.Abs(_lastSpeedFraction - fraction) < .04f) return;
        _lastSpeedFraction = fraction;
        if (!_fogConcealed) ApplySpeedVisuals(fraction);
    }

    private void ApplySpeedVisuals(float fraction)
    {
        bool active = fraction > Mathf.Clamp(Config.MinimumSpeedFraction, 0f, .5f);
        _flame.Visible = active;

        float fade = Mathf.Clamp(.45f + fraction * .55f, 0f, 1f);
        _flame.Modulate = new Color(1f, 1f, 1f, fade * (_stealthed ? Config.StealthIntensity : 1f));
        float length = _baseFlameLength * (.55f + .45f * Mathf.Clamp(fraction, 0f, 1f));
        // EngineFlame.svg is 96×24, hot nozzle at the +X edge.
        _flame.Scale = new Vector2(length / 96f, _flameWidth / 24f);
        _flame.Position = new Vector2(_rearX - length * .5f, _rearY);
        _exhaust.Modulate = new Color(1f, 1f, 1f, _stealthed ? Config.StealthIntensity : 1f);
        // Exhaust particle travel should match the displayed engine, not punish slow heavy hulls.
        // The status fraction still reduces/halts it when Slow or Freeze affects the ship.
        _exhaust.SpeedScale = Mathf.Clamp(Config.ParticleSpeedMultiplier *
            (_baseFlameLength / 16f) * fraction, .1f, 2.8f);

        if (!active) StopGpu();
        else TryStartGpu();
    }

    // =========================================================
    // Preserve the same 30% visibility as a stealthed ship, so engines don't expose hidden hulls.
    public void SetStealthDimmed(bool stealthed)
    {
        if (_stealthed == stealthed) return;
        _stealthed = stealthed;
        if (!_configured || _fogConcealed) return;
        float speed = Mathf.Max(0, _lastSpeedFraction);
        float alpha = Mathf.Clamp(.45f + speed * .55f, 0f, 1f) *
            (stealthed ? Config.StealthIntensity : 1f);
        _flame.Modulate = new Color(1f, 1f, 1f, alpha);
        _exhaust.Modulate = new Color(1f, 1f, 1f, stealthed ? Config.StealthIntensity : 1f);
    }

    // =========================================================
    // Concealment is separate from viewport admission and stealth dimming.
    internal void SetFogConcealed(bool concealed)
    {
        if (!_configured || _fogConcealed == concealed) return;
        _fogConcealed = concealed;
        if (concealed)
        {
            StopGpu();
            _flame.Visible = false;
            _exhaust.Visible = false;
            _exhaust.SpeedScale = 0;
            _restartExhaust = true;
            return;
        }
        _exhaust.Visible = true;
        ApplySpeedVisuals(_lastSpeedFraction);
    }

    // =========================================================
    // Rendering visibility signals wake/sleep *only* the GPU emitter.
    private void OnScreenEntered() { _onScreen = true; TryStartGpu(); }
    private void OnScreenExited() { _onScreen = false; StopGpu(); }

    // =========================================================
    // Cap active GPU exhaust systems globally. At cap, the inexpensive static flame remains.
    private void TryStartGpu()
    {
        if (_hasGpuSlot || !_configured || _fogConcealed || !_onScreen || !_flame.Visible ||
            Config?.GpuParticlesEnabled != true ||
            _activeGpuEmitters >= Mathf.Clamp(Config.MaxActiveGpuEmitters, 1, 128)) return;
        _hasGpuSlot = true;
        _activeGpuEmitters++;
        if (_restartExhaust)
        {
            _restartExhaust = false;
            _exhaust.Restart();
        }
        _exhaust.Emitting = true;
    }

    // =========================================================
    // Release the shared budget immediately on exit, Freeze, death or arrival.
    private void StopGpu()
    {
        if (!_hasGpuSlot) return;
        _exhaust.Emitting = false;
        _hasGpuSlot = false;
        _activeGpuEmitters = Mathf.Max(0, _activeGpuEmitters - 1);
    }

    public void Stop()
    {
        StopGpu();
        if (GodotObject.IsInstanceValid(_flame)) _flame.Visible = false;
    }

    // =========================================================
    // No global manager, orphan emitter or per-frame process survives enemy destruction.
    public override void _ExitTree()
    {
        StopGpu();
        if (GodotObject.IsInstanceValid(_screen))
        {
            _screen.ScreenEntered -= OnScreenEntered;
            _screen.ScreenExited -= OnScreenExited;
        }
        _configured = false;
    }
    #endregion
}
