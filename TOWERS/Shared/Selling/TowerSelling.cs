// Shared hull-based resale math; payment history belongs to the instance, never catalog prices.
using System;
using Godot;

public static class TowerSelling
{
    // =========================================================
    // Decimal intermediates keep large histories safe; payouts remain within the existing int wallet.
    public static int Refund(long invested, float health, float maximum, float rate)
    {
        if (invested <= 0 || !float.IsFinite(health) || !float.IsFinite(maximum) || maximum <= 0 || !float.IsFinite(rate)) return 0;
        decimal ratio = (decimal)Math.Clamp((double)health / maximum, 0d, 1d);
        decimal value = decimal.Floor(invested * (decimal)Math.Clamp(rate, 0f, .5f) * ratio);
        return (int)Math.Min(int.MaxValue, value);
    }

    // =========================================================
    // Placement membership is checked by its owner; mission objectives never enter that index.
    public static bool Eligible(TowerBase tower, TowerMasterConfig rules) =>
        rules?.AllowSelling == true && GodotObject.IsInstanceValid(tower) && tower.IsInsideTree()
        && !tower.IsQueuedForDeletion() && !tower.GetTree().Paused && !tower.IsDestroyed
        && !tower.IsBusy && !tower.SaleCommitted && tower.CreditsInvested.HasValue && tower.Health > 0;
}
