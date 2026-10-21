// Authored material identity and edge appearance; material identity is independent of walkability.
using Godot;

public enum TerrainEdgeLayer { Passable, Interactive, Impassable }
public enum TerrainEdgeShape { Soft, Jagged, Straight }

[GlobalClass]
public partial class TerrainEdgeProfile : Resource
{
    [ExportGroup("Material Identity")]
    [Export] public string MaterialId { get; set; } = "";
    [Export] public TerrainEdgeLayer Layer { get; set; } = TerrainEdgeLayer.Passable;
    [Export(PropertyHint.Range, "0,32,1")] public int SourceId { get; set; } = 2;
    [Export] public Godot.Collections.Array<Vector2I> AtlasCells { get; set; } = new();

    [ExportGroup("Visual Style")]
    [Export] public bool RenderEdges { get; set; } = true;
    [Export] public TerrainEdgeShape Shape { get; set; } = TerrainEdgeShape.Soft;
    [Export(PropertyHint.Range, "0,1,0.01")] public float CornerSoftness { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Irregularity { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Variation { get; set; } = 0.25f;
    [Export] public Color EdgeTint { get; set; } = new(0.9f, 0.62f, 0.41f, 1f);

    // =========================================================
    // Multiple atlas coordinates may share one material ID so visual variants do not create internal borders.
    public bool Matches(TerrainEdgeLayer layer, int sourceId, Vector2I atlas) =>
        !string.IsNullOrWhiteSpace(MaterialId) && layer == Layer && sourceId == SourceId
        && AtlasCells != null && AtlasCells.Contains(atlas);
}
