// VE1: at most two GPU-shader quads; original baked background is preserved.
using Godot;
public partial class LevelAtmosphereController : Node2D
{
    #region Configuration
    [Export] public LevelAtmosphereConfig Config { get; set; }
    #endregion
    #region Lifecycle
    // =========================================================
    // Attach once; inactive visual instances do no C# processing or shader work.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        var c = Config;
        if (!Visible || c == null || !c.Enabled) return;
        Vector2 size = new(Mathf.Clamp(c.CoveragePixels.X, 512f, 32768f), Mathf.Clamp(c.CoveragePixels.Y, 512f, 32768f));
        if (c.NebulaEnabled && c.NebulaOpacity > 0f) AddLayer("NebulaDrift", size, false);
        if (c.StarsEnabled && c.StarDensity > 0f && c.StarOpacity > 0f) AddLayer("StarTwinkle", size, true);
    }
    // =========================================================
    // Shared shader file, independently configured per-level material.
    private void AddLayer(string name, Vector2 size, bool stars)
    {
        Shader shader = GD.Load<Shader>($"res://WORLD/VISUALS/Shaders/{name}.gdshader");
        if (shader == null) { GD.PushWarning($"VE1 missing shader {name}"); return; }
        ShaderMaterial material = new() { Shader = shader };
        if (stars)
        {
            material.SetShaderParameter("tint", Config.StarTint);
            material.SetShaderParameter("opacity", Mathf.Clamp(Config.StarOpacity, 0f, 1f));
            material.SetShaderParameter("density", Mathf.Clamp(Config.StarDensity, 0f, 1f));
            material.SetShaderParameter("twinkle_amount", Mathf.Clamp(Config.TwinkleAmount, 0f, 1f));
            material.SetShaderParameter("twinkle_speed", Mathf.Clamp(Config.TwinkleSpeed, 0f, 5f));
        }
        else
        {
            material.SetShaderParameter("tint", Config.NebulaTint);
            material.SetShaderParameter("opacity", Mathf.Clamp(Config.NebulaOpacity, 0f, 1f));
            material.SetShaderParameter("drift_speed", Mathf.Clamp(Config.DriftSpeed, 0f, .1f));
        }
        AddChild(new ColorRect { Name = name, Size = size, Material = material, Color = Colors.White, MouseFilter = Control.MouseFilterEnum.Ignore });
    }
    #endregion
}