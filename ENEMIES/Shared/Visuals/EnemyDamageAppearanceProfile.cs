// Optional ordered Sprite2D textures for health-based enemy battle damage; index 0 is pristine.
using Godot;

[GlobalClass]
public partial class EnemyDamageAppearanceProfile : Resource
{
    // =========================================================
    // Equal health bands are derived from the number of supplied textures; no hardcoded thresholds.
    [Export] public Godot.Collections.Array<Texture2D> Stages { get; set; } = new();
}
