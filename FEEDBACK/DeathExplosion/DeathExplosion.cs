// Reusable, independent GPU smoke-and-flash puff for killed ships and future destroyed towers.
// Spawn into World/Effects; never parent effects to objects scheduled for QueueFree.
using Godot;

public partial class DeathExplosion : Node2D
{
    #region Shared Assets and Settings
    [Export] public DeathExplosionConfig Style { get; set; }
    public float AppliedScale { get; private set; }
    public static int ActiveCount { get; private set; }

    private static PackedScene _scene;
    private static FeedbackMasterConfig _master;
    private static DeathExplosionConfig _defaultStyle;
    private GpuParticles2D _smoke, _flash;
    private bool _counted;
    #endregion

    #region Lifecycle
    // =========================================================
    // One-shot GPU particles own the animation; the explosion has no per-frame C# loop.
    public override void _Ready()
    {
        _smoke = GetNode<GpuParticles2D>("Smoke");
        _flash = GetNode<GpuParticles2D>("Flash");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Release the concurrency budget even if the world is unloaded mid-explosion.
    public override void _ExitTree()
    {
        if (!_counted) return;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        _counted = false;
    }
    #endregion

    #region Shared Spawn API
    // =========================================================
    // Measure the actual displayed sprite dimensions in world pixels, including parent scale.
    // Rotating the ship does not change its explosion size; use local axes' lengths.
    public static float MeasureVisualSize(Sprite2D sprite, float fallbackPixels = 32f)
    {
        if (sprite == null || sprite.Texture == null) return Mathf.Max(1f, fallbackPixels);
        Vector2 rect = sprite.GetRect().Size;
        float width = rect.X * sprite.GlobalTransform.X.Length();
        float height = rect.Y * sprite.GlobalTransform.Y.Length();
        float measured = Mathf.Max(width, height);
        return float.IsFinite(measured) && measured > 0f ? measured : Mathf.Max(1f, fallbackPixels);
    }

    // =========================================================
    // Caller passes only an effects layer, world location, visual size and optional override.
    // The scene/config are lazy-loaded once and reused across enemies and future tower types.
    public static DeathExplosion Spawn(Node2D layer, Vector2 position, float visualSizePixels, float multiplier = 1f)
    {
        if (layer == null || !GodotObject.IsInstanceValid(layer) || !layer.IsInsideTree() ||
            !float.IsFinite(multiplier) || multiplier <= 0f) return null;

        _master ??= GD.Load<FeedbackMasterConfig>("res://FEEDBACK/DefaultFeedbackConfig.tres");
        if (_master != null && !_master.DeathExplosionsEnabled) return null;
        _defaultStyle ??= GD.Load<DeathExplosionConfig>("res://FEEDBACK/DeathExplosion/DefaultDeathExplosionConfig.tres");
        if (ActiveCount >= Mathf.Max(1, _defaultStyle?.MaxConcurrentExplosions ?? 64)) return null;

        _scene ??= GD.Load<PackedScene>("res://FEEDBACK/DeathExplosion/DeathExplosion.tscn");
        if (_scene == null) return null;
        DeathExplosion effect = _scene.Instantiate<DeathExplosion>();
        layer.AddChild(effect);
        effect.GlobalPosition = position;
        effect._counted = true; ActiveCount++;
        effect.Play(visualSizePixels, multiplier);
        return effect;
    }
    #endregion

    #region Appearance
    // =========================================================
    // Scale and particle count are automatic; only optional per-type multiplier changes them.
    // The particle materials and color ramps live in the scene for artist-friendly tuning.
    public void Play(float visualSizePixels, float multiplier = 1f)
    {
        DeathExplosionConfig settings = Style ?? _defaultStyle;
        float reference = Mathf.Max(1f, settings?.ReferenceVisualSizePixels ?? 32f);
        float min = Mathf.Max(0.01f, settings?.MinimumScale ?? 0.55f);
        float max = Mathf.Max(min, settings?.MaximumScale ?? 3f);
        float size = float.IsFinite(visualSizePixels) ? Mathf.Max(0f, visualSizePixels) : reference;
        AppliedScale = Mathf.Clamp(size / reference * Mathf.Max(0f, multiplier) *
            (settings?.GlobalSizeMultiplier ?? 1f), min, max);
        Scale = Vector2.One * AppliedScale;

        _smoke.Amount = Mathf.Max(1, settings?.SmokeParticleCount ?? 16);
        _flash.Amount = Mathf.Max(1, settings?.FlashParticleCount ?? 5);
        _smoke.Lifetime = Mathf.Max(0.05f, settings?.SmokeLifetimeSeconds ?? 0.7f);
        _flash.Lifetime = Mathf.Max(0.05f, settings?.FlashLifetimeSeconds ?? 0.18f);
        _smoke.Emitting = true; _flash.Emitting = true;

        // A tween timer also cleans up on renderers lacking a GPU-particles Finished signal.
        Tween cleanup = CreateTween();
        cleanup.TweenInterval(Mathf.Max(_smoke.Lifetime, _flash.Lifetime) + 0.15f);
        cleanup.Finished += QueueFree;
    }
    #endregion
}
