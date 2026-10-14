// Registered tower type: scene/art/category and a reference to canonical authored config.
using Godot;
using System;

[GlobalClass]
public partial class TowerDefinition : Resource
{
    public enum TowerCategory { Offensive, Support, Economy, Utility }
    [ExportGroup("Type")]
    [Export] public string TowerId { get; set; } = "";
    [Export] public TowerCategory Category { get; set; }
    [Export] public bool Available { get; set; } = true;
    [ExportGroup("Assets")]
    [Export] public PackedScene Scene { get; set; }
    [Export] public TowerConfig DefaultConfig { get; set; }
    [Export] public Texture2D Icon { get; set; }
    [ExportGroup("Placement")]
    [Export] public Vector2I Footprint { get; set; } = Vector2I.One;
    public string DisplayName => DefaultConfig?.DisplayName ?? TowerId;
    public string Description => DefaultConfig?.Description ?? "";

    // =========================================================
    // Validate registration once; temporary scene inspection never enters the tree or runs combat.
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TowerId) || TowerId != TowerId.Trim()) throw new InvalidOperationException("Tower ID must be nonblank and trimmed.");
        if (!Enum.IsDefined(Category) || Footprint != Vector2I.One) throw new InvalidOperationException("Only valid categories and 1 x 1 towers are supported.");
        if (Scene == null || DefaultConfig == null) throw new InvalidOperationException("Tower scene/config is missing: " + TowerId);
        if (DefaultConfig.ConstructionCosts == null) throw new InvalidOperationException("Construction recipe is missing: " + TowerId);
        foreach (ResourceCost cost in DefaultConfig.ConstructionCosts)
            if (cost?.Resource == null || string.IsNullOrWhiteSpace(cost.Resource.ResourceId) || cost.Amount < 0)
                throw new InvalidOperationException("Invalid construction cost: " + TowerId);
        Node root = Scene.Instantiate();
        try
        {
            if (root is not TowerBase tower || !tower.AcceptsConfig(DefaultConfig) || !tower.ValidateScene())
                throw new InvalidOperationException("Scene does not support its declared tower config: " + TowerId);
        }
        finally { root.Free(); }
    }
}
