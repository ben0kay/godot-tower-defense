// Named normalized boundary range; map data, not a scene full of spawn markers.
using Godot;
using System;
[Tool, GlobalClass]
public partial class SpawnEdgeSector : Resource
{
    [Export] public string SectorId { get; set; } = "";
    [Export] public LevelConfig.MapEdge Side { get; set; } = LevelConfig.MapEdge.Top;
    [Export(PropertyHint.Range, "0,1,.01")] public float StartFraction { get; set; }
    [Export(PropertyHint.Range, "0,1,.01")] public float EndFraction { get; set; } = 1;
    [Export] public bool ExcludePortalFootprints { get; set; } = true;
    [Export(PropertyHint.Range, "0,32,.5")] public float MinimumPortalDistanceCells { get; set; }
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SectorId) || SectorId != SectorId.Trim() ||
            Side is not (LevelConfig.MapEdge.Top or LevelConfig.MapEdge.Bottom or LevelConfig.MapEdge.Left or LevelConfig.MapEdge.Right) ||
            !float.IsFinite(StartFraction) || !float.IsFinite(EndFraction) || StartFraction < 0 || EndFraction > 1 || StartFraction >= EndFraction ||
            !float.IsFinite(MinimumPortalDistanceCells) || MinimumPortalDistanceCells < 0 || MinimumPortalDistanceCells > 32)
            throw new ArgumentException("Edge sector needs a unique ID, one side, a nonempty 0–1 range and valid portal spacing.");
    }
}
