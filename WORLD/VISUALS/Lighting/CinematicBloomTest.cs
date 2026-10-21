// Isolated VE5C visual sandbox. Uses the engine glow processor and draws static showcase art.
// No modifications to maps, towers, enemy logic or project.godot.
using Godot;

public partial class CinematicBloomTest : Node2D
{
    #region Inspector Settings
    [Export] public BloomTestConfig Config { get; set; }
    [Export] public Key ToggleKey { get; set; } = Key.B;
    private WorldEnvironment _worldEnvironment;
    private Environment _environment;
    private bool _enabled = true;
    #endregion

    #region Lifecycle
    // =========================================================
    // Own a private Environment; toggling bloom does not affect any other world.
    public override void _Ready()
    {
        Config ??= new BloomTestConfig();
        _worldEnvironment = GetNode<WorldEnvironment>("WorldEnvironment");
        _environment = new Environment { BackgroundMode = Environment.BGMode.Canvas };
        _environment.GlowIntensity = Mathf.Clamp(Config.Intensity, 0f, 2f);
        _environment.GlowBloom = Mathf.Clamp(Config.Bloom, 0f, 1f);
        _environment.GlowHdrThreshold = Mathf.Clamp(Config.Threshold, 0f, 2f);
        _enabled = Config.Enabled;
        _environment.GlowEnabled = _enabled;
        _worldEnvironment.Environment = _environment;
        SetProcess(false);
        QueueRedraw();
    }

    // =========================================================
    // B toggles built-in bloom for an immediate same-scene comparison.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != ToggleKey) return;
        _enabled = !_enabled;
        _environment.GlowEnabled = _enabled;
        QueueRedraw();
        GetViewport().SetInputAsHandled();
    }
    #endregion

    #region Test Artwork
    // =========================================================
    // No asset dependency. Contrast cool glows with sharp neutral geometry at normal 2D zoom.
    public override void _Draw()
    {
        DrawRect(new Rect2(-640, -360, 1280, 720), Config.BackgroundTint);
        DrawCircle(new Vector2(0, 40), 255f, new Color(Config.HaloTint.R, Config.HaloTint.G, Config.HaloTint.B, Config.HaloOpacity));
        for (int i = -5; i <= 5; i++)
        {
            float x = i * 110f;
            DrawLine(new Vector2(x, -310), new Vector2(x, 310), new Color(.16f, .24f, .35f, .30f), 1f);
        }
        for (int i = -3; i <= 3; i++)
        {
            float y = i * 100f;
            DrawLine(new Vector2(-620, y), new Vector2(620, y), new Color(.16f, .24f, .35f, .30f), 1f);
        }
        DrawCircle(new Vector2(-295, 0), 83f, new Color("#192a42"));
        DrawArc(new Vector2(-295, 0), 65f, 0, Mathf.Tau, 72, new Color("#c348e8"), 4f, true);
        DrawArc(new Vector2(-295, 0), 48f, 0, Mathf.Tau, 72, new Color("#a658d0"), 2f, true);
        DrawCircle(new Vector2(-295, 0), 25f, new Color("#a559d9"));
        DrawRect(new Rect2(15, -77, 165, 155), new Color("#172c40"));
        DrawRect(new Rect2(28, -64, 139, 129), new Color("#4c7685"), false, 4);
        DrawCircle(new Vector2(96, 0), 44f, new Color("#1c778d"));
        DrawCircle(new Vector2(96, 0), 26f, new Color("#a1f5ff"));
        DrawCircle(new Vector2(96, 0), 13f, new Color("#e6ffff"));
        DrawLine(new Vector2(225, -95), new Vector2(395, -95), new Color("#f5c180"), 3f, true);
        DrawLine(new Vector2(225, -91), new Vector2(395, -91), new Color("#f6b26a"), 1f, true);
        DrawCircle(new Vector2(460, 0), 42f, new Color("#731b2c"));
        DrawArc(new Vector2(460, 0), 44f, 0, Mathf.Tau, 64, new Color("#fe676e"), 4f, true);
        DrawLine(new Vector2(390, 100), new Vector2(480, 100), new Color("#e2e6ed"), 2f, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(-560, -280), "VE5C  |  CINEMATIC BLUE  |  B: bloom on/off", HorizontalAlignment.Left, -1, 19, Colors.LightGray);
        DrawString(ThemeDB.FallbackFont, new Vector2(-560, 280), _enabled ? "BLOOM ON" : "BLOOM OFF", HorizontalAlignment.Left, -1, 20, new Color("#a1cce2"));
    }
    #endregion
}
