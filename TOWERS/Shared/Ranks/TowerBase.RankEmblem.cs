// Fixed battlefield rank emblem for ranked placed towers; purely visual, no XP or save state.
using Godot;

public abstract partial class TowerBase
{
    private TowerRankEmblem _rankEmblem;

    // =========================================================
    // SetPlacement runs after Ready for paid placements and after checkpoint rank restore.
    // Standard-only passive buildings have no combat-rank badge.
    private void AttachRankEmblem()
    {
        if (_rankEmblem != null || AuthoredConfig?.Ranks?.Count != 5 || !IsInsideTree()) return;
        float cellSize = GetNodeOrNull<WorldGrid>("../../Grid")?.Config.CellSize ?? 64f;
        _rankEmblem = new TowerRankEmblem();
        AddChild(_rankEmblem);
        _rankEmblem.Configure(Footprint, cellSize, CurrentRank.Rank);
    }

    // =========================================================
    // A genuine promotion changes one small texture; ordinary XP awards touch no emblem.
    private void RefreshRankEmblem() => _rankEmblem?.SetRank(CurrentRank.Rank);
}
