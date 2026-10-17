// Inspector-editable visual and layout defaults shared by the gameplay HUD.
// Configure the interface from DefaultUIConfig.tres, not hardcoded gameplay values.
using Godot;

[GlobalClass]
public partial class UIMasterConfig : Resource
{
    [ExportGroup("HUD Layout")]
    [Export(PropertyHint.Range, "32,96,1")] public int TopBarHeight { get; set; } = 46;
    [Export(PropertyHint.Range, "110,270,1")] public int BottomBarHeight { get; set; } = 176;
    [Export(PropertyHint.Range, "260,900,1")] public int InspectorWidth { get; set; } = 560;
    [Export(PropertyHint.Range, "180,600,1")] public int InspectorHeight { get; set; } = 330;
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float BottomPanelOpacity { get; set; } = 0.90f;
    [Export(PropertyHint.Range, "0,36,1")] public int EdgeMargin { get; set; } = 12;
    [Export(PropertyHint.Range, "80,180,1")] public int TowerButtonWidth { get; set; } = 112;

    [ExportGroup("Top Status")]
    [Export] public int StatusFontSize { get; set; } = 16;
    [Export] public int StatusGapPixels { get; set; } = 16;
    [Export] public int CitadelBlockWidth { get; set; } = 280;
    [Export] public int CitadelBarWidth { get; set; } = 220;
    [Export] public int CitadelBarHeight { get; set; } = 5;
    [Export] public Color HealthHealthyColor { get; set; } = new Color("#69cddc");
    [Export] public Color HealthWarningColor { get; set; } = new Color("#efbc62");
    [Export] public Color HealthCriticalColor { get; set; } = new Color("#ed6666");
    [Export(PropertyHint.Range, "0,1,0.05")] public float HealthWarningThreshold { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float HealthCriticalThreshold { get; set; } = 0.25f;

    [ExportGroup("Credit Balance Animation")]
    [Export] public bool AnimateCreditBalance { get; set; } = true;
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float CreditCountSeconds { get; set; } = 0.3f;

    [ExportGroup("Credit Reward Feedback")]
    [Export] public bool ShowCreditRewards { get; set; } = true;
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float CreditRewardStackSeconds { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float CreditRewardFadeSeconds { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,12,1")] public float CreditRewardRisePixels { get; set; } = 4f;
    [Export(PropertyHint.Range, "1,1.5,0.05")] public float CreditRewardPulseScale { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0.05,0.5,0.05")] public float CreditRewardPulseSeconds { get; set; } = 0.22f;
    [Export] public Color CreditRewardColor { get; set; } = new Color("#69e4a3");
    [Export] public bool ShowCreditExpenses { get; set; } = true;
    [Export] public Color CreditSpendColor { get; set; } = new Color("#efbc62");

    [ExportGroup("Top Bar Reactions")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float CitadelBarTweenSeconds { get; set; } = 0.25f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float CitadelDamageFlashSeconds { get; set; } = 0.3f;
    [Export] public Color CitadelDamageFlashColor { get; set; } = new Color("#ed6666");
    [Export(PropertyHint.Range, "0,1,0.05")] public float WavePulseSeconds { get; set; } = 0.28f;
    [Export(PropertyHint.Range, "1,1.25,0.01")] public float WavePulseScale { get; set; } = 1.10f;

    [ExportGroup("Interaction")]
    [Export] public bool StartWithTowerBarOpen { get; set; } = false;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float TowerBarTransitionSeconds { get; set; } = 0.17f;

    [ExportGroup("Palette")]
    [Export] public Color TopBarColor { get; set; } = new Color("#132633");
    [Export] public Color PanelColor { get; set; } = new Color("#122330");
    [Export] public Color ButtonColor { get; set; } = new Color("#1b3645");
    [Export] public Color AccentColor { get; set; } = new Color("#69cddc");
    [Export] public Color TextColor { get; set; } = new Color("#e0f2f5");
    [Export] public Color MutedTextColor { get; set; } = new Color("#97b4bf");
}
