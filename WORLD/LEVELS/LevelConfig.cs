// Per-level spawn permissions and authored object references; no wave or spawn logic.
using Godot;

[GlobalClass]
public partial class LevelConfig : Resource
{
    public enum SpawnSourceMode { Portals, MapEdges, Both }
    [System.Flags] public enum MapEdge { None = 0, Top = 1, Bottom = 2, Left = 4, Right = 8 }

    [ExportGroup("Enemy Entry")]
    [Export] public SpawnSourceMode SpawnMode { get; set; } = SpawnSourceMode.Portals;
    [Export(PropertyHint.Flags, "Top,Bottom,Left,Right")] public MapEdge AllowedEdges { get; set; } = MapEdge.Left;
    [Export] public Godot.Collections.Array<NodePath> ActivePortals { get; set; } = new();

    [ExportGroup("Destination")]
    [Export] public NodePath CitadelPath { get; set; } = new("Objectives/Citadel");
}
