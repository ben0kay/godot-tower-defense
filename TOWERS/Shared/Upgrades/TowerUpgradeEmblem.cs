// Fixed Roman-numeral badge for the tower's completed purchased level; imported SVGs are shared globally.
using Godot;

public sealed partial class TowerUpgradeEmblem : Sprite2D
{
    private const string Artwork = "res://TOWERS/Shared/Upgrades/Artwork/";
    private static readonly string[] Files =
    {
        "level_01.svg", "level_02.svg", "level_03.svg", "level_04.svg",
        "level_05.svg", "level_06.svg", "level_07.svg", "level_08.svg",
        "level_09.svg", "level_10.svg", "level_11.svg", "level_12.svg"
    };
    private static readonly Texture2D[] Icons = new Texture2D[Files.Length];
    private int _currentLevel;

    public TowerUpgradeEmblem()
    {
        Name = "UpgradeEmblem";
        ZIndex = 6; // On the stationary root, above rotating/under-construction sprites.
    }

    // =========================================================
    // Match the rank badge's size and top margin; mirror it into the top-left footprint corner.
    public void Configure(Vector2I footprint, float cellSize, int level)
    {
        float size = Mathf.Clamp(cellSize * 0.30f, 12f, 20f);
        Scale = Vector2.One * (size / 24f);
        Position = new Vector2(-footprint.X * cellSize * .5f + size * .5f + 3f,
            -footprint.Y * cellSize * .5f + size * .5f + 3f);
        SetLevel(level);
    }

    // =========================================================
    // Each completed tier reuses one imported SVG; upgrades replace only this sprite's texture.
    // Do not mislabel a future tier beyond the currently authored Roman-numeral range.
    public void SetLevel(int level)
    {
        if (level == _currentLevel && Texture != null) return;
        _currentLevel = level;
        int index = level - 1;
        if (index < 0 || index >= Icons.Length) { Texture = null; Visible = false; return; }
        Texture = Icons[index] ??= GD.Load<Texture2D>(Artwork + Files[index]);
        Visible = Texture != null;
    }
}
