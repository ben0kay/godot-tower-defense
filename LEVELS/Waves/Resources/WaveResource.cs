// One authorable wave: ordered batches and optional narrator/banner metadata.
using Godot;

[Tool, GlobalClass]
public partial class WaveResource : Resource
{
    [Export(PropertyHint.Range, "1,10000,1,or_greater")] public int Number { get; set; } = 1;
    [Export] public Godot.Collections.Array<WaveBatchResource> Batches { get; set; } = new();

    [ExportGroup("Presentation")]
    [Export] public WaveAlertKind Alert { get; set; } = WaveAlertKind.Standard;
    [Export] public string BannerTitle { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string BannerDescription { get; set; } = "";
    [Export] public string Speaker { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string NarrationText { get; set; } = "";
    [Export] public string VoiceCueId { get; set; } = "";

    // =========================================================
    // Copy both metadata and nested batches for independent wave duplication.
    public WaveResource Copy()
    {
        var copy = new WaveResource { Number = Number, Alert = Alert, BannerTitle = BannerTitle,
            BannerDescription = BannerDescription, Speaker = Speaker, NarrationText = NarrationText, VoiceCueId = VoiceCueId };
        foreach (WaveBatchResource batch in Batches) copy.Batches.Add(batch?.Copy());
        return copy;
    }
}
