// Shared recipe display; no wallet, placement or stat calculations.
using System.Text;

public static class TowerPresentation
{
    // =========================================================
    // The same display format is used for shared build and upgrade recipes.
    public static string DescribeResourceCosts(Godot.Collections.Array<ResourceCost> costs)
    {
        if (costs == null) return "Invalid cost configuration";
        StringBuilder text = new();
        foreach (ResourceCost cost in costs)
        {
            if (cost?.Resource == null || string.IsNullOrWhiteSpace(cost.Resource.ResourceId) || cost.Amount < 0) return "Invalid cost configuration";
            if (cost.Amount == 0) continue;
            if (text.Length > 0) text.Append(", ");
            text.Append($"{cost.Amount:N0} {cost.Resource.DisplayName}");
        }
        return text.Length == 0 ? "Free" : text.ToString();
    }

}
