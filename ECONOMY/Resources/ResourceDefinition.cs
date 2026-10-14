// One editable resource type definition; balance and transactions live in the EconomyManager.
// Keep ResourceId stable once referenced by save files, purchases and upgrades.
using Godot;

[GlobalClass]
public partial class ResourceDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string ResourceId { get; set; } = "resource";
    [Export] public string DisplayName { get; set; } = "Resource";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Texture2D Icon { get; set; }
}
