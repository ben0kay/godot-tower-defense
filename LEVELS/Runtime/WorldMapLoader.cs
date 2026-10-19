// Install the authored map before any grid/navigation/service Ready callbacks execute.
using Godot;
using System;

public partial class WorldMapLoader : Node2D
{
    [Export] public PackedScene MapScene { get; set; }
    // =========================================================
    // First child preserves World/Level paths and initialization order for every existing world service.
    public override void _EnterTree() => PrepareMap();
    // =========================================================
    // Fixtures may mount off-tree to edit their private map before services initialize.
    public void PrepareMap()
    {
        if (GetNodeOrNull<Node>("Level") != null) return;
        if (MapScene == null) throw new InvalidOperationException("World has no map scene.");
        Node level = MapScene.Instantiate();
        if (level is not WorldLevel) { level.Free(); throw new InvalidOperationException("World map must use WorldLevel."); }
        level.Name = "Level"; AddChild(level); MoveChild(level, 0);
    }
}
