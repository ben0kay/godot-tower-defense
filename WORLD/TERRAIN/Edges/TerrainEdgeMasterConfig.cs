// Central Inspector settings for future edge generation. Disabled by default; no game systems consume this yet.
using Godot;

[GlobalClass]
public partial class TerrainEdgeMasterConfig : Resource
{
    [ExportGroup("Feature")]
    [Export] public bool Enabled { get; set; } = false;
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
