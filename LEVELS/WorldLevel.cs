// Typed root for manually painted maps and their Inspector-editable level settings.
using Godot;

public partial class WorldLevel : Node2D
{
    [Export] public LevelConfig Settings { get; set; }
    public TileMapLayer Passable => GetNode<TileMapLayer>("Terrain/Passable");
    public TileMapLayer Impassable => GetNode<TileMapLayer>("Terrain/Impassable");
    public TileMapLayer Decorations => GetNode<TileMapLayer>("Terrain/Decorations");
    public Node2D Citadel => Settings == null ? null : GetNodeOrNull<Node2D>(Settings.CitadelPath);
}
