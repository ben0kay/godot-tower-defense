// Event-driven miniature rank emblem attached only to a placed combat tower's fixed root.
using Godot;

public sealed partial class TowerRankEmblem : Sprite2D
{
    private const string Artwork = "res://TOWERS/Shared/Ranks/Artwork/";
    private static readonly string[] Files =
    {
        "standard.svg", "veteran.svg", "enhanced.svg", "superior.svg", "legendary.svg"
    };
    private static readonly Texture2D[] Icons = new Texture2D[5];

    public TowerRankEmblem()
    {
        Name = "RankEmblem";
        ZIndex = 6; // Fixed root overlay above rotating weapon/unfinished construction artwork.
    }

    // =========================================================
    // Inside the top-right footprint, sized from one tile rather than the whole multi-cell building.
    public void Configure(Vector2I footprint, float cellSize, TowerRank rank)
    {
        float size = Mathf.Clamp(cellSize * 0.30f, 12f, 20f);
        Scale = Vector2.One * (size / 24f);
        Position = new Vector2(footprint.X * cellSize * .5f - size * .5f - 3f,
            -footprint.Y * cellSize * .5f + size * .5f + 3f);
        SetRank(rank);
    }

    // =========================================================
    // Each rank shares one imported SVG texture; promotion replaces it, with no polling or tween.
    public void SetRank(TowerRank rank)
    {
        int index = (int)rank;
        if (index < 0 || index >= Icons.Length) return;
        Texture = Icons[index] ??= GD.Load<Texture2D>(Artwork + Files[index]);
    }
}
