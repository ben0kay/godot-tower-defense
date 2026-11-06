// Selected-tower sale quotes and transactions; removal uses the established scene-tree cleanup.
using Godot;

public partial class TowerPlacementController
{
    private TowerBase _saleTower;
    private int _saleQuote;

    // =========================================================
    // Only real player placements in a live mission qualify, across every registered category.
    private bool CanSell(TowerBase tower) => IsInsideTree() && !IsQueuedForDeletion()
        && GetParent().IsInsideTree() && !GetParent().IsQueuedForDeletion()
        && GetParent().GetNodeOrNull<LevelSessionController>("LevelSession")?.State == LevelSessionState.Playing
        && TowerSelling.Eligible(tower, Settings) && _placementExits.ContainsKey(tower);
    private int SaleRefund(TowerBase tower) => TowerSelling.Refund(tower.CreditsInvested ?? 0,
        tower.Health, tower.MaxHealth, Settings.DefaultSellRefundFraction);

    // =========================================================
    // Runtime/health, work, selection and wallet events reuse the existing selected-only refresh.
    private void RefreshSelling()
    {
        bool valid = GodotObject.IsInstanceValid(_selected);
        bool eligible = valid && CanSell(_selected);
        int refund = valid ? SaleRefund(_selected) : 0;
        if (_saleTower != null && (_saleTower != _selected || !eligible || refund != _saleQuote)) InvalidateSale();
        _hud.SetSellDetails(valid, eligible, refund,
            "Credits refund: floor(actual Credits paid × resale rate (maximum 50%) × current hull HP / max HP)." +
            (valid && !_selected.CreditsInvested.HasValue ? " Legacy checkpoint: purchased cost unknown; selling unavailable." : ""));
    }
    private void InvalidateSale() { _saleTower = null; if (GodotObject.IsInstanceValid(_hud)) _hud.CloseSellConfirmation(); }

    // =========================================================
    // A modal quote never authorizes selling another selection or a different health-based amount.
    private void OnSellRequested()
    {
        InvalidateSale();
        if (!_hud.IsInspectionVisible || !CanSell(_selected)) return;
        _saleTower = _selected; _saleQuote = SaleRefund(_selected);
        _hud.ShowSellConfirmation(_selected.AuthoredConfig.DisplayName, _saleQuote);
    }
    private void OnSellConfirmed()
    {
        TowerBase tower = _saleTower; int quote = _saleQuote; InvalidateSale();
        if (!_hud.IsInspectionVisible || tower != _selected || !CanSell(tower)) return;
        if (SaleRefund(tower) != quote) { RefreshSelling(); _hud.SetUpgradeError("Resale value changed; request a new quote."); return; }
        tower.SaleCommitted = true; // Latch before Grant publishes any callbacks.
        if (!_economy.Grant(_economy.CreditsResource, quote))
        { tower.SaleCommitted = false; RefreshSelling(); _hud.SetUpgradeError("Wallet cannot accept the refund; tower retained."); return; }
        // TreeExiting releases footprint/selection/indexes; feature owners dispose power/work/logistics.
        _towers.RemoveChild(tower); tower.QueueFree();
    }
}
