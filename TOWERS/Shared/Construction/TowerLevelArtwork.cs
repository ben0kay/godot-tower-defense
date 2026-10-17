// Optional per-tier finished image and one quick intermediate upgrade image; absent images retain completed artwork.
using Godot;

[GlobalClass]
public partial class TowerLevelArtwork : Resource
{
    [Export(PropertyHint.Range, "1,100,1,or_greater")] public int Level { get; set; } = 2;
    [Export] public Texture2D FinishedTexture { get; set; }
    [Export] public Texture2D UpgradeTexture { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float UpgradeProgress { get; set; } = .75f;
}
