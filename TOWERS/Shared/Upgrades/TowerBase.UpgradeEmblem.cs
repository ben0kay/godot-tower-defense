// Fixed top-left purchased-level badge, separate from the earned top-right rank emblem.
using Godot;

public abstract partial class TowerBase
{
    private TowerUpgradeEmblem _upgradeEmblem;

    // =========================================================
    // Only buildings with multiple purchase levels need a level marker.
    // Placement also runs after saved tier restoration; never attach on ghost previews.
    private void AttachUpgradeEmblem()
    {
        if (_upgradeEmblem != null || Levels == null || Levels.Count < 2 || !IsInsideTree()) return;
        float cellSize = GetNodeOrNull<WorldGrid>("../../Grid")?.Config.CellSize ?? 64f;
        _upgradeEmblem = new TowerUpgradeEmblem();
        AddChild(_upgradeEmblem);
        _upgradeEmblem.Configure(Footprint, cellSize, UpgradeLevel);
    }

    // =========================================================
    // Refresh only when gameplay republishes calculated stats, never from a frame callback.
    private void RefreshUpgradeEmblem() => _upgradeEmblem?.SetLevel(UpgradeLevel);
}
