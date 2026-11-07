// Shared drone-scene adapter. Godot follows the body transform only while a light slot is assigned.
using Godot;

public partial class DroneLightAttachment : RemoteTransform2D
{
    private WorldLightingController _lighting;

    // =========================================================
    // Wait for the containing world and shared presentation body to finish entering the tree.
    public override void _Ready() => Callable.From(Register).CallDeferred();

    private void Register()
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || GetParent() is not DroneBody body) return;
        for (Node ancestor = body.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not WorldMapLoader) continue;
            _lighting = ancestor.GetNodeOrNull<WorldLightingController>("Lighting");
            _lighting?.TrackDrone(this, body);
            return;
        }
    }

    // =========================================================
    // A pooled light follows world position only. Radius and tint stay with the lighting controller.
    internal void FollowWith(PointLight2D light) => RemotePath = GetPathTo(light);
    internal void StopFollowing() => RemotePath = new NodePath("");

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_lighting)) _lighting.Untrack(this);
        _lighting = null;
    }
}
