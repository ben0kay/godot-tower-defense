// Shared Inspector-editable price entry for construction and future upgrades.
using Godot;

[GlobalClass]
public partial class ResourceCost : Resource
{
    [Export] public ResourceDefinition Resource { get; set; }
    [Export(PropertyHint.Range, "0,2000000000,1")] public int Amount { get; set; }
}
