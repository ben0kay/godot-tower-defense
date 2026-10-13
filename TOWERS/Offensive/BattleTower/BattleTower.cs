// Minimal Battle Tower scene instance. Stores grid cell and selection tint.
// Nothing processes each frame; placement/selection is handled centrally.
using Godot;

public partial class BattleTower : Node2D
{
    #region Settings and State
    [Export] public BattleTowerConfig Config { get; set; }
    public Vector2I GridCell { get; private set; }
    private Sprite2D _sprite;
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache the single visual once; do not run idle physics or per-tower processing.
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        SetProcess(false);
        SetPhysicsProcess(false);
    }
    #endregion

    #region Placement and Selection
    // =========================================================
    // Initialize a placed tower's logical cell and world position.
    public void SetPlacement(Vector2I cell, Vector2 center)
    {
        GridCell = cell;
        GlobalPosition = center;
    }

    // =========================================================
    // Modulate the existing sprite instead of instantiating selection effects.
    public void SetSelected(bool selected) =>
        _sprite.Modulate = selected ? new Color(1f, 0.9f, 0.68f) : Colors.White;
    #endregion
}
