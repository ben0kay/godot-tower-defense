// Optional authored pre-wave briefing; lives with the level, independent of wave-start narration.
using Godot;

[Tool, GlobalClass]
public partial class MissionBriefingDefinition : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public string Title { get; set; } = "MISSION BRIEFING";
    [Export] public string Speaker { get; set; } = "COMMAND INTELLIGENCE";
    [Export(PropertyHint.MultilineText)] public string Message { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Objectives { get; set; } = "";
}
