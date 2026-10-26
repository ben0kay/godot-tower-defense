// Shared battlefield composition; only the mission resource changes between deployments.
using Godot;

public partial class BattlefieldLauncher : Node2D
{
    [Export] public MissionDefinition Mission { get; set; }
    internal CampaignData RestoringCampaign { get; set; }
    public CampaignSession Saves { get; private set; }
    // =========================================================
    // The save coordinator is dormant: it is called only by explicit UI/tool commands.
    public override void _Ready() => Saves = new CampaignSession(this);
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
