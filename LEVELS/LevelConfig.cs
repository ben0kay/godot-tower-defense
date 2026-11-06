// Per-level spawn permissions, optional load-time briefing and authored references; no wave logic.
using Godot;

[GlobalClass]
public partial class LevelConfig : Resource
{
    public enum SpawnSourceMode { Portals, MapEdges, Both }
    [System.Flags] public enum MapEdge { None = 0, Top = 1, Bottom = 2, Left = 4, Right = 8 }

    [ExportGroup("Player XP Rewards (provisional)")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")] public int VictoryBaseXP { get; set; } = 500;
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")] public int DefeatBaseXP { get; set; }

    [ExportGroup("Map Size (optional)")]
    [Export] public WorldMasterConfig WorldConfig { get; set; }

    [ExportGroup("Starting Resources")]
    [Export(PropertyHint.Range, "0,2000000000,100")] public int StartingCredits { get; set; } = 0;

    [ExportGroup("Enemy Entry")]
    [Export] public SpawnSourceMode SpawnMode { get; set; } = SpawnSourceMode.Portals;
    [Export(PropertyHint.Flags, "Top,Bottom,Left,Right")] public MapEdge AllowedEdges { get; set; } = MapEdge.Left;
    [Export] public Godot.Collections.Array<NodePath> ActivePortals { get; set; } = new();

    [ExportGroup("Mission Intro (optional)")]
    [Export] public MissionBriefingDefinition MissionBriefing { get; set; }

    [ExportGroup("Waves")]
    [Export(PropertyHint.Range, "0,86400,1")] public double DefaultNextWaveDelaySeconds { get; set; } = 30;
    [Export] public WaveSetSource WaveSource { get; set; }
    [Export] public string WaveSetId { get; set; } = "";

    [ExportGroup("Destination")]
    [Export] public Godot.Collections.Array<NodePath> CitadelPaths { get; set; } = new();
    [Export(PropertyHint.Range, "1,1000000,1")] public int CitadelMaxHealth { get; set; } = 1000;
    [Export] public NodePath CitadelPath { get; set; } = new("Objectives/Citadel");
}
