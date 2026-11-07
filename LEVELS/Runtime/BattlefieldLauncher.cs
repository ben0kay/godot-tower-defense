// Shared battlefield composition; only the mission resource changes between deployments.
using Godot;

public partial class BattlefieldLauncher : Node2D
{
    private const string AmbienceId = "ambience_atmospheric_space_1";

    [Export] public MissionDefinition Mission { get; set; }
    internal CampaignData RestoringCampaign { get; set; }
    public CampaignSession Saves { get; private set; }
    // =========================================================
    // The battlefield owns the ambient emitter and the on-demand save coordinator.
    public override void _Ready()
    {
        Saves = new CampaignSession(this);
        AudioManager.Find(this)?.StartEmitter(AmbienceId, this);
    }

    // =========================================================
    // Stop the scene's emitter on unload or retry.
    public override void _ExitTree() => AudioManager.Find(this)?.StopEmitter(AmbienceId, this);
    // =========================================================
    // The parent enters first, allowing the world to mount the correct map before services initialize.
    public override void _EnterTree() => PrepareMap();
    // =========================================================
    // Explicit preparation also supports off-tree tooling without running gameplay callbacks.
    public void PrepareMap()
    {
        Mission?.Validate();
        WorldMapLoader world = GetNode<WorldMapLoader>("World");
        if (Mission != null) world.MapScene = Mission.MapScene;
        world.PrepareMap();
    }
}
