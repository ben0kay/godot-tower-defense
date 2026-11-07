// Shared retained drone presentation. Existing active role updates drive it; no timers, particles or own tick.
using Godot;
using System;
public partial class DroneBody : Node2D
{
    [Export] public DroneVisualConfig Config { get; set; }
    [Export] public Vector2 EngineOffset { get; set; } = new(9, 6);
    [Export(PropertyHint.Range, "0,64,1")] public float TrailStartPixels { get; set; } = 8;
    // LGT0-A.1 observes flight transitions from the existing presentation path.
    public bool IsFlightPresented { get; private set; }
    public event Action FlightPresentationChanged;
    private Sprite2D _sprite;
    private readonly Vector2[] _history = new Vector2[32];
    private int _count, _leg = -1;
    private float _heading, _ratio, _ignition, _glow, _lastSpeed, _accelerationGlow;
    private bool? _cargo;
    public override void _Ready() { _sprite = GetNode<Sprite2D>("Sprite"); SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // A leg transition ignites both rear strips. History follows actual world positions, never the target vector.
    public void Present(float heading, Vector2 velocity, float maximumSpeed, bool flying, int leg, bool? cargo, float dt, bool ignite = true)
    {
        if (!IsVisibleInTree()) { Reset(); return; }
        SetFlightPresented(flying);
        _heading = heading; _sprite.Rotation = heading + Mathf.Pi / 2; _cargo = cargo;
        if (Config == null) return;
        if (flying && _leg != leg)
        { _ignition = ignite ? Safe(Config.IgnitionSeconds, .28f, .2f, .35f) : 0; _count = 0; }
        _leg = flying ? leg : -1;
        if (!flying) _ignition = 0;
        _ignition = Mathf.Max(0, _ignition - dt);
        _ratio = flying && maximumSpeed > 0 ? Mathf.Clamp(velocity.Length() / maximumSpeed, 0, 1) : 0;
        float speed = flying ? velocity.Length() : 0;
        float accelerating = dt > 0 && speed > _lastSpeed ? Mathf.Clamp((speed - _lastSpeed) / dt / 1000, 0, 1) : 0;
        _accelerationGlow = Mathf.Lerp(_accelerationGlow, accelerating, 1 - Mathf.Exp(-10 * Mathf.Max(0, dt)));
        _lastSpeed = speed;
        _glow = flying ? Mathf.Lerp(.12f, 1, _ratio) : .08f;
        if (!flying || !Config.TrailEnabled || Config.TrailAlpha <= 0 || Config.MaximumTrailLength <= 0 || _ratio < .02f) _count = 0;
        else
        {
            Vector2 point = GlobalPosition;
            if (_count == 0 || point.DistanceSquaredTo(_history[0]) > .36f)
            {
                int last = Mathf.Min(_count, _history.Length - 1);
                for (int i = last; i > 0; i--) _history[i] = _history[i - 1];
                _history[0] = point; _count = Mathf.Min(_count + 1, _history.Length);
            }
        }
        QueueRedraw();
    }
    public void Reset() { SetFlightPresented(false); if (_count > 0 || _glow > 0 || _cargo.HasValue) QueueRedraw(); _count = 0; _leg = -1; _ignition = _glow = _lastSpeed = _accelerationGlow = 0; _cargo = null; }
    // =========================================================
    // Publish only state transitions, including pool reset and fighter docking.
    private void SetFlightPresented(bool flying)
    {
        if (IsFlightPresented == flying) return;
        IsFlightPresented = flying;
        FlightPresentationChanged?.Invoke();
    }
    // =========================================================
    // Bounded segments fade outward. Engines/cargo rotate with the sprite, never the construction beam.
    public override void _Draw()
    {
        var c = Config; if (c == null) return;
        float response = Mathf.Pow(_ratio, Safe(c.SpeedResponse, 1, .25f, 3));
        float min = Safe(c.MinimumTrailLength, 15, 0, 64), max = Mathf.Max(min, Safe(c.MaximumTrailLength, 35, 0, 64));
        float start = Safe(TrailStartPixels, 8, 0, 64);
        float tail = response * Mathf.Lerp(min, max, response), length = start + tail, covered = 0;
        Transform2D local = GlobalTransform.AffineInverse();
        if (c.TrailEnabled && c.TrailAlpha > 0 && tail > .1f)
            for (int i = 0; i + 1 < _count && covered < length; i++)
            {
                Vector2 a = _history[i], b = _history[i + 1]; float segment = a.DistanceTo(b);
                if (segment < .001f) continue;
                float used = Mathf.Min(segment, length - covered); b = a.Lerp(b, used / segment);
                if (covered + used <= start) { covered += used; continue; }
                if (covered < start) a = a.Lerp(b, (start - covered) / used);
                Color color = c.TrailColor;
                color.A *= Safe(c.TrailAlpha, .32f, 0, 1) * Mathf.Clamp(1 - (Mathf.Max(start, covered) + used * .5f - start) / tail, 0, 1);
                DrawLine(local * a, local * b, color, Safe(c.TrailWidth, 1.25f, .5f, 2), true); covered += used;
            }
        DrawSetTransform(Vector2.Zero, _heading + Mathf.Pi / 2);
        if (c.ThrustersEnabled && c.Intensity > 0 && _glow > 0)
        {
            float duration = Safe(c.IgnitionSeconds, .28f, .2f, .35f);
            float flare = _ignition > 0 ? Mathf.Sin(Mathf.Pi * (1 - _ignition / duration)) : 0;
            float scale = Safe(c.EngineScale, 1, .2f, 3) * (1 + flare * (Safe(c.IgnitionScale, 2.2f, 1, 4) - 1));
            float intensity = Safe(c.Intensity, 1, 0, 3) * Mathf.Max(_glow, flare);
            Color halo = c.HaloTint, core = c.CoreTint; halo.A *= intensity; core.A *= intensity;
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = EngineOffset.IsFinite() ? EngineOffset : new(9, 6);
                Vector2 engine = new(i == 0 ? -offset.X : offset.X, offset.Y);
                float extension = Safe(c.EngineLengthMultiplier, 1, 1, 5) *
                    (1 + _accelerationGlow * Safe(c.AccelerationFlare, 0, 0, 2));
                float plume = (1.5f + 3 * _ratio + 3 * flare) * scale * extension;
                DrawCircle(engine + Vector2.Down * scale, 2.4f * scale, halo);
                if (extension > 1.01f)
                    DrawLine(engine, engine + Vector2.Down * plume, halo, 3.5f * scale, true);
                DrawLine(engine, engine + Vector2.Down * plume, core, 1.5f * scale, true);
                DrawCircle(engine, .8f * scale, core);
            }
        }
        if (_cargo.HasValue) DrawCircle(new(0, 2), 1.8f, _cargo.Value ? new Color("#ffc06b") : new Color("#438d9b"));
        DrawSetTransform(Vector2.Zero);
    }
    private static float Safe(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
