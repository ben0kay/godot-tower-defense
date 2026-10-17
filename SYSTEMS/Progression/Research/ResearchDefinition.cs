// Inspector-authored research data; IDs and axial coordinates are independent of display text.
using Godot;

public enum ResearchEffectKind { None, CitadelMaxHealthPercent }

[GlobalClass]
public partial class ResearchDefinition : Resource
{
    [ExportGroup("Identity and Graph")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public int CostRP { get; set; } = 1;
    [Export] public string[] PrerequisiteIds { get; set; } = System.Array.Empty<string>();
    [Export] public Vector2I AxialPosition { get; set; }
    [ExportGroup("Gameplay Effect")]
    [Export] public ResearchEffectKind Effect { get; set; }
    [Export] public double EffectPercent { get; set; }
}
