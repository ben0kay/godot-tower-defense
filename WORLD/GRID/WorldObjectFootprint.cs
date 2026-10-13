// Reserves grid cells for authored Portal/Citadel objects without physics per cell.
// The root position is the center of the upper-left footprint cell.
using Godot;

public partial class WorldObjectFootprint : Node2D
{
    [ExportGroup("Grid Footprint")]
    [Export] public Vector2I SizeCells { get; set; } = Vector2I.One;
    [Export] public bool BlocksEnemies { get; set; }
}
