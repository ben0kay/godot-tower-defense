// Central opt-in mask-generation settings; disabled by default for explicit visual/performance rollout.
using Godot;

[GlobalClass]
public partial class TerrainEdgeMasterConfig : Resource
{
    [ExportGroup("Feature")]
    [Export] public bool Enabled { get; set; } = false;
    [Export(PropertyHint.Range, "8,32,8")] public int MaskPixelsPerCell { get; set; } = 16;
    [Export(PropertyHint.Range, "1,64,1")] public int MaximumMaskMegapixels { get; set; } = 16;
    [Export] public int VariationSeed { get; set; } = 1337;

    [ExportGroup("Material Profiles")]
    [Export] public Godot.Collections.Array<TerrainEdgeProfile> Profiles { get; set; } = new();

    // =========================================================
    // Lookups are an explicit terrain-rebuild operation, not a per-frame tile scan.
    public TerrainEdgeProfile Find(TerrainEdgeLayer layer, int sourceId, Vector2I atlas)
    {
        if (Profiles == null) return null;
        foreach (TerrainEdgeProfile profile in Profiles)
            if (profile != null && profile.Matches(layer, sourceId, atlas)) return profile;
        return null;
    }
}
