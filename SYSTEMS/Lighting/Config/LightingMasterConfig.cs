// Inspector settings for one world's native lighting, source cap, and debug comparison.
using Godot;

[GlobalClass]
public partial class LightingMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool StartLit { get; set; } = true;
    [Export] public Color AmbientTint { get; set; } = new Color("#4a5562");
    [Export(PropertyHint.Range, "0,64,1")] public int MaxActiveLights { get; set; } = 24;
    [Export] public bool AllowDebugComparison { get; set; } = true;

    [ExportGroup("Source Profiles")]
    [Export] public LightSourceConfig Tower { get; set; }
    [Export] public LightSourceConfig Citadel { get; set; }
    [Export] public LightSourceConfig Drone { get; set; }
    [Export] public LightSourceConfig Lava { get; set; }
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float ConstructionRadiusFactor { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float ConstructionEnergyFactor { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,16,1")] public int MaxDroneLights { get; set; } = 8;

    [ExportGroup("Projectile Lighting")]
    [Export] public LightSourceConfig Projectile { get; set; }
    [Export(PropertyHint.Range, "0,8,1")] public int MaxProjectileLights { get; set; } = 4;

    [ExportGroup("Light-based Fog of War")]
    [Export] public bool RevealEnabled { get; set; } = true;
    [Export] public LightSourceConfig Portal { get; set; }

    [ExportGroup("Viewport Culling")]
    [Export] public bool CullOffscreenLights { get; set; } = true;
    [Export(PropertyHint.Range, "0.25,0.5,0.01")] public float RefreshSeconds { get; set; } = 0.25f;
    [Export(PropertyHint.Range, "0,256,1")] public float ScreenMarginPixels { get; set; } = 64;

    // Reserved settings for LGT0-B. LGT0-A never enables native shadows.
    [ExportGroup("Shadows (LGT0-B)")]
    [Export] public bool ShadowsEnabled { get; set; } = true;
    [Export] public Color ShadowTint { get; set; } = new Color(0.12f, 0.16f, 0.22f, 0.45f);
    [Export(PropertyHint.Enum, "Hard,PCF5")] public int ShadowQuality { get; set; }
    [Export(PropertyHint.Range, "0,16,1")] public int MaxShadowLights { get; set; } = 6;
}
