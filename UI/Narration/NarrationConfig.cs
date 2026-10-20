// Shared authored presentation settings; not a wave scheduler or audio engine.
using Godot;

[GlobalClass]
public partial class NarrationConfig : Resource
{
    #region Behaviour and Timing
    [ExportGroup("Behaviour")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "5,150,1,suffix:char/s")]
    public float CharactersPerSecond { get; set; } = 34f;
    [Export(PropertyHint.Range, "0,30,0.5,suffix:s")]
    public float HoldSeconds { get; set; } = 5f;
    [Export(PropertyHint.Range, "0.01,2,0.01,suffix:s")]
    public float FadeInSeconds { get; set; } = .18f;
    [Export(PropertyHint.Range, "0.01,2,0.01,suffix:s")]
    public float FadeOutSeconds { get; set; } = .30f;
    #endregion

    #region Top-Right Layout
    [ExportGroup("Layout - Top Right")]
    [Export(PropertyHint.Range, "220,700,10,suffix:px")]
    public float PanelWidth { get; set; } = 390f;
    [Export(PropertyHint.Range, "80,260,2,suffix:px")]
    public float PanelHeight { get; set; } = 136f;
    [Export(PropertyHint.Range, "0,80,1,suffix:px")]
    public float RightMargin { get; set; } = 18f;
    [Export(PropertyHint.Range, "48,240,1,suffix:px")]
    public float TopMargin { get; set; } = 64f;
    [Export(PropertyHint.Range, "0,60,1,suffix:px")]
    public float AlertClearance { get; set; } = 14f;
    #endregion

    #region Appearance
    [ExportGroup("Appearance")]
    [Export] public Color Background { get; set; } = new(.032f, .080f, .115f, .93f);
    [Export] public Color Accent { get; set; } = new(.28f, .79f, .91f, 1f);
    [Export] public Color TextColor { get; set; } = new(.88f, .94f, .96f, 1f);
    [Export(PropertyHint.Range, "10,30,1,suffix:px")]
    public int BodyFontSize { get; set; } = 15;
    [Export(PropertyHint.Range, "9,26,1,suffix:px")]
    public int SpeakerFontSize { get; set; } = 12;
    #endregion
}
