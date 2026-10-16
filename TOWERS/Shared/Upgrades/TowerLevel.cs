// Complete absolute level snapshot and the existing multi-resource purchase recipe.
using Godot;

public sealed class TowerLevel
{
    public double WorkSeconds { get; set; }
    public int Level { get; set; }
    public string DisplayName { get; set; }
    public TowerStatBlock Stats { get; set; }
    public Godot.Collections.Array<ResourceCost> Costs { get; set; }

    // =========================================================
    // Convert the visible C# credit price to the shared wallet's ordinary resource-cost format once.
    public TowerLevel(int level, int credits, TowerStatBlock stats, double workSeconds = 0)
    {
        TowerConfig.ValidateWorkSeconds(workSeconds); WorkSeconds = workSeconds;
        Level = level; DisplayName = $"Level {level}"; Stats = stats;
        Costs = new() { new ResourceCost { Resource = GD.Load<ResourceDefinition>("res://ECONOMY/Resources/Credits.tres"), Amount = credits } };
    }

    // =========================================================
    // Cloning needs no resource lookup or temporary cost entries.
    private TowerLevel() { }

    // =========================================================
    // Copies keep runtime row/recipe edits local; resource definitions retain their stable identities.
    public TowerLevel CreateInstanceCopy() => new() { Level = Level, WorkSeconds = WorkSeconds, Stats = Stats, DisplayName = DisplayName, Costs = CopyCosts(Costs) };

    // =========================================================
    // Reuse ordinary economy recipes, including multiple resources, without shared mutable entries.
    internal static Godot.Collections.Array<ResourceCost> CopyCosts(Godot.Collections.Array<ResourceCost> costs)
    {
        if (costs == null) return null;
        var copy = new Godot.Collections.Array<ResourceCost>();
        foreach (ResourceCost cost in costs)
            copy.Add(cost == null ? null : new ResourceCost { Resource = cost.Resource, Amount = cost.Amount });
        return copy;
    }
}
