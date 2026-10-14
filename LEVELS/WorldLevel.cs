// Typed root for manually painted maps and their Inspector-editable level settings.
using Godot;

public partial class WorldLevel : Node2D
{
    [Export] public LevelConfig Settings { get; set; }
    public TileMapLayer Ground => GetNode<TileMapLayer>("Terrain/Ground");
    public TileMapLayer Walls => GetNode<TileMapLayer>("Terrain/Walls");
    public Node2D Citadel => Settings == null ? null : GetNodeOrNull<Node2D>(Settings.CitadelPath);
}
