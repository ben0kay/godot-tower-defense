// Inspector-editable starting curve; existing saves retain their original curve.
using Godot;

[GlobalClass]
public partial class ProgressionMasterConfig : Resource
{
    [ExportGroup("New Profile XP Curve")]
    [Export(PropertyHint.Range, "1,1000000,1")] public int FirstLevelXP { get; set; } = 500;
    [Export(PropertyHint.Range, "0,1000000,1")] public int LevelStepXP { get; set; } = 250;
}
