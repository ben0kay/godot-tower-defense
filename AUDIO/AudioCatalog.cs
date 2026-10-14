// Inspector registry of sound definitions; sound files and .tres entries stay in feature-owned folders.
using Godot;

[Tool, GlobalClass]
public partial class AudioCatalog : Resource
{
    [Export] public Godot.Collections.Array<AudioDefinition> Sounds { get; set; } = new();
}
