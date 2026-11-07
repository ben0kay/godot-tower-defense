// One-shot mission loading scene: threaded resource status and an image-ready sci-fi UI.
using Godot;
using System;

public partial class LevelLoadingScreen : Control
{
    // =========================================================
    // Externally supplied by MenuNavigation before this scene enters the tree.
    public string MissionPath { get; set; }
    public string ReturnScenePath { get; set; } = MenuNavigation.LevelsScene;

    [ExportGroup("Presentation")]
    [Export(PropertyHint.File, "*.png")]
    public string BackdropPath { get; set; } = "res://UI/Loading/Artwork/LoadingBackdrop.png";
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float ArtworkDarken { get; set; } = 0.48f;
    [Export(PropertyHint.Range, "0,2,0.05")]
    public float MinimumDisplaySeconds { get; set; } = 0.6f;

    private const string BattlefieldScene = "res://LEVELS/Runtime/Battlefield.tscn";
    private static readonly Color Accent = new("#5cdae4");
    private static readonly Color Muted = new("#90aebd");
    private static readonly Color White = new("#e9f5fa");
    private static readonly Color Grid = new("#234656");

    private Texture2D _backdrop;
    private Font _font;
    private Button _back;
    private float _progress;
    private float _shown;
    private float _elapsed;
    private float _scan;
    private bool _ready;
    private bool _switching;
    private bool _failed;
    private string _stage = "PREPARING DEPLOYMENT";
    private string _details = "QUEUING MISSION RESOURCES";

    // =========================================================
    // The artwork file is optional: a dark vector background works until added later.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _font = ThemeDB.FallbackFont;
        _back = GetNode<Button>("BackButton");
        _back.Hide();
        _back.Pressed += () => MenuNavigation.Open(this, ReturnScenePath);
        Resized += QueueRedraw;

        if (!string.IsNullOrWhiteSpace(BackdropPath) && ResourceLoader.Exists(BackdropPath, "Texture2D"))
            _backdrop = GD.Load<Texture2D>(BackdropPath);

        if (string.IsNullOrWhiteSpace(MissionPath) || !MissionPath.EndsWith(".tres", StringComparison.Ordinal))
        {
            Fail("Invalid mission destination.");
            return;
        }

        if (!ResourceLoader.Exists(MissionPath) || !ResourceLoader.Exists(BattlefieldScene))
        {
            Fail("Mission or battlefield resources were not found.");
            return;
        }

        Error missionRequest = ResourceLoader.LoadThreadedRequest(MissionPath);
        Error sceneRequest = ResourceLoader.LoadThreadedRequest(BattlefieldScene);
        if (missionRequest != Error.Ok || sceneRequest != Error.Ok)
        {
            Fail($"Could not queue assets: {missionRequest} / {sceneRequest}");
            return;
        }

        SetProcess(true);
    }

    // =========================================================
    // Use Godot's actual threaded resource-loading progress; no invented progress timer.
    public override void _Process(double delta)
    {
        float dt = (float)Math.Min(delta, 0.1);
        _elapsed += dt;
        _scan = Mathf.PosMod(_scan + dt * 0.65f, 1f);

        if (!_failed && !_ready)
        {
            float mission = Sample(MissionPath, out ResourceLoader.ThreadLoadStatus missionStatus);
            float battlefield = Sample(BattlefieldScene, out ResourceLoader.ThreadLoadStatus battlefieldStatus);
            if (missionStatus == ResourceLoader.ThreadLoadStatus.Failed ||
                battlefieldStatus == ResourceLoader.ThreadLoadStatus.Failed ||
                missionStatus == ResourceLoader.ThreadLoadStatus.InvalidResource ||
                battlefieldStatus == ResourceLoader.ThreadLoadStatus.InvalidResource)
            {
                Fail("Godot could not load the selected mission resources.");
            }
            else
            {
                _progress = (mission + battlefield) * 0.5f;
                _ready = missionStatus == ResourceLoader.ThreadLoadStatus.Loaded &&
                         battlefieldStatus == ResourceLoader.ThreadLoadStatus.Loaded;
                _stage = _ready ? "DEPLOYMENT ASSETS READY" : "LOADING MISSION ASSETS";
                _details = _ready ? "INITIALIZING BATTLEFIELD" : "STREAMING MISSION DATA";
                if (_ready) _progress = 1f;
            }
        }

        _shown = Mathf.MoveToward(_shown, _progress, dt * 2.6f);
        QueueRedraw();

        // One brief frame-safe presentation window, then replace this entire screen.
        if (!_failed && _ready && !_switching &&
            _elapsed >= MinimumDisplaySeconds && _shown >= 0.999f)
        {
            _switching = true;
            Callable.From(EnterMission).CallDeferred();
        }
    }

    private static float Sample(string path, out ResourceLoader.ThreadLoadStatus status)
    {
        Godot.Collections.Array progress = new();
        status = ResourceLoader.LoadThreadedGetStatus(path, progress);
        if (status == ResourceLoader.ThreadLoadStatus.Loaded) return 1f;
        if (status != ResourceLoader.ThreadLoadStatus.InProgress || progress.Count == 0) return 0f;
        return Mathf.Clamp((float)progress[0], 0f, 1f);
    }

    // =========================================================
    // Instantiate on the main thread after both resource loads finish.
    private void EnterMission()
    {
        if (_failed || !IsInsideTree()) return;
        BattlefieldLauncher instance = null;
        try
        {
            MissionDefinition mission = ResourceLoader.LoadThreadedGet(MissionPath) as MissionDefinition;
            PackedScene scene = ResourceLoader.LoadThreadedGet(BattlefieldScene) as PackedScene;
            if (mission == null || scene == null || !mission.Enabled)
                throw new InvalidOperationException("Mission is unavailable.");
            mission.Validate();

            instance = scene.Instantiate<BattlefieldLauncher>();
            instance.Mission = mission;
            Error result = GetTree().ChangeSceneToNode(instance);
            if (result != Error.Ok) throw new InvalidOperationException("Battlefield transition failed: " + result);
        }
        catch (Exception error)
        {
            if (GodotObject.IsInstanceValid(instance) && !instance.IsInsideTree()) instance.Free();
            Fail(error.Message);
        }
    }

    // =========================================================
    // Leave failures readable with an explicit return, never an endless spinner.
    private void Fail(string reason)
    {
        _failed = true;
        _switching = false;
        _stage = "DEPLOYMENT FAILED";
        _details = reason;
        _back.Show();
        SetProcess(true);
        QueueRedraw();
        GD.PushWarning("Level loading: " + reason);
    }

    // =========================================================
    // One CanvasItem draws the entire overlay, percentage gauge and bottom loading rail.
    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        if (w < 40 || h < 40) return;
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), 0.55f, 1.4f);
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#07141e"));
        if (_backdrop != null)
        {
            Vector2 img = _backdrop.GetSize();
            if (img.X > 0 && img.Y > 0)
            {
                float zoom = Mathf.Max(w / img.X, h / img.Y);
                Vector2 crop = new(w / zoom, h / zoom);
                DrawTextureRectRegion(_backdrop, new Rect2(0, 0, w, h), new Rect2((img - crop) * 0.5f, crop));
            }
            DrawRect(new Rect2(0, 0, w, h), new Color(0.01f, 0.025f, 0.04f, ArtworkDarken));
        }
        else
        {
            // A deliberately quiet fallback leaves the large art field clear for a future PNG.
            for (int n = 0; n < 18; n++)
                DrawRect(new Rect2(0f, h * n / 18f, w, h / 18f + 1f),
                    new Color(0.04f, 0.10f, 0.14f, 0.12f * (1f - n / 18f)));
        }

        float margin = Mathf.Clamp(w * 0.045f, 26f, 94f);
        float barY = h - 92f * scale;
        float barW = w - margin * 2f;
        int small = Mathf.RoundToInt(14f * scale);
        int large = Mathf.RoundToInt(23f * scale);
        Line(new Vector2(margin, 42f * scale), new Vector2(margin + 104f * scale, 42f * scale), Accent, 2f);
        Write("SECTOR COMMAND  //  MISSION DEPLOYMENT", new Vector2(margin, 70f * scale), small, White);
        Write(_stage, new Vector2(margin, barY - 42f * scale), large, White);
        Write(_details, new Vector2(margin, barY + 45f * scale), small, Muted);

        // Thin continuous cyan progress rail, backed by discrete telemetry ticks.
        DrawRect(new Rect2(margin, barY, barW, 10f * scale), new Color("#142f3c"));
        float fill = barW * Mathf.Clamp(_shown, 0f, 1f);
        if (fill > 0.5f) DrawRect(new Rect2(margin, barY, fill, 10f * scale), Accent);
        for (int n = 1; n < 44; n++)
        {
            float x = margin + barW * n / 44f;
            Line(new Vector2(x, barY), new Vector2(x, barY + 10f * scale),
                 new Color("#09202d"), 1f);
        }
        float scanX = margin + Mathf.PosMod(_scan, 1f) * barW;
        DrawRect(new Rect2(scanX - 1f, barY - 3f * scale, 2f, 16f * scale),
            new Color(0.36f, 0.86f, 0.89f, 0.5f));
        Line(new Vector2(margin, barY - 6f * scale), new Vector2(margin, barY + 17f * scale), Accent, 2f);
        Line(new Vector2(margin + barW, barY - 6f * scale),
             new Vector2(margin + barW, barY + 17f * scale), Accent, 2f);

        // Three counter-rotating partial arcs, peripheral dots and a numerical center.
        Vector2 center = new(w - margin - 72f * scale, barY - 112f * scale);
        float radius = 46f * scale;
        DrawArc(center, radius, 0f, Mathf.Tau, 84, new Color("#264954"), 1.2f);
        float spin = _scan * Mathf.Tau;
        DrawArc(center, radius, -spin, -spin + 2f, 34, Accent, 2.8f);
        DrawArc(center, radius * 0.82f, spin, spin + 1.75f, 30, new Color("#68b6ca"), 2.2f);
        DrawArc(center, radius * 0.64f, spin + 2f, spin + 3.3f, 22, Accent, 2f);
        for (int dot = 0; dot < 8; dot++)
        {
            float a = -spin + dot * Mathf.Tau / 8f;
            DrawCircle(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius + 8f * scale),
                1.3f * scale, Accent);
        }
        string percent = $"{Mathf.RoundToInt(_shown * 100f):00}%";
        DrawString(_font, new Vector2(center.X - radius, center.Y + 9f * scale),
                   percent, HorizontalAlignment.Center, radius * 2f, Mathf.RoundToInt(27f * scale), White);
        Write("TRANSFER", new Vector2(center.X - 32f * scale, center.Y + 28f * scale), Mathf.RoundToInt(11f * scale), Muted);
        DrawRect(new Rect2(margin, h - 27f * scale, barW, 1f), new Color("#234656"));
    }

    private void Write(string text, Vector2 at, int fontSize, Color color) =>
        DrawString(_font, at, text, HorizontalAlignment.Left, -1f, fontSize, color);
    private void Line(Vector2 a, Vector2 b, Color color, float width) => DrawLine(a, b, color, width);
}
