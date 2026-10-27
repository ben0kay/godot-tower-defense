// Shared immutable artwork references: any number of build stages and optional purchased-tier appearances.
using Godot;

[GlobalClass]
public partial class TowerConstructionArt : Resource
{
    [Export] public Godot.Collections.Array<TowerConstructionStage> BuildStages { get; set; } = new();
    [Export] public Godot.Collections.Array<TowerLevelArtwork> LevelArtwork { get; set; } = new();

    // =========================================================
    // Unsorted authoring is safe; ignore invalid/missing entries and choose the latest reached threshold.
    public Texture2D BuildTexture(double progress, Texture2D fallback)
    {
        float best = -1;
        if (BuildStages != null) foreach (TowerConstructionStage stage in BuildStages)
            if (stage?.Texture != null && ValidThreshold(stage.Progress) && stage.Progress <= progress && stage.Progress > best)
            { best = stage.Progress; fallback = stage.Texture; }
        return fallback;
    }

    // =========================================================
    // A tier without its own final image keeps the nearest earlier completed image, never an unfinished transition.
    public Texture2D FinishedTexture(int level, Texture2D original)
    {
        int best = 0;
        if (LevelArtwork != null) foreach (TowerLevelArtwork art in LevelArtwork)
            if (art?.FinishedTexture != null && art.Level > best && art.Level <= level)
            { best = art.Level; original = art.FinishedTexture; }
        return original;
    }

    // =========================================================
    // Independent tier layers retain the nearest completed sprite for levels with no authored replacement.
    public Texture2D FinishedBaseTexture(int level, Texture2D original)
    {
        int best = 0;
        if (LevelArtwork != null) foreach (TowerLevelArtwork art in LevelArtwork)
            if (art?.FinishedBaseTexture != null && art.Level > best && art.Level <= level)
            { best = art.Level; original = art.FinishedBaseTexture; }
        return original;
    }

    // =========================================================
    // Rotating/recoiling gun art follows the same fallback policy without changing muzzle node placement.
    public Texture2D FinishedGunTexture(int level, Texture2D original)
    {
        int best = 0;
        if (LevelArtwork != null) foreach (TowerLevelArtwork art in LevelArtwork)
            if (art?.FinishedGunTexture != null && art.Level > best && art.Level <= level)
            { best = art.Level; original = art.FinishedGunTexture; }
        return original;
    }

    // =========================================================
    // Only the exact purchased target may show its optional intermediate image; old-tier mechanics stay independent.
    public Texture2D UpgradeTexture(int level, double progress, Texture2D fallback)
    {
        if (LevelArtwork != null) foreach (TowerLevelArtwork art in LevelArtwork)
            if (art?.Level == level && art.UpgradeTexture != null && ValidThreshold(art.UpgradeProgress) && progress >= art.UpgradeProgress)
                return art.UpgradeTexture;
        return fallback;
    }

    // =========================================================
    // Broken cosmetic data is inert rather than a placement/payment failure.
    private static bool ValidThreshold(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
}
