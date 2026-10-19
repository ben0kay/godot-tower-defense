// Inspector-assignable wave recipe contract; concrete level recipes remain concise C# code.
using Godot;

public abstract partial class WaveSetSource : Resource
{
    // =========================================================
    // Build fresh immutable definitions for a deployment, never hold scheduler/session state here.
    public abstract WaveDefinition[] Build();
}
