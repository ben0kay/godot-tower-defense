// Inactive visual hook for Codex's future organic mask/transition renderer; deliberately draws nothing today.
using Godot;

public partial class TerrainEdgeRenderer : Node2D
{
    private TerrainEdgeController _controller;

    // =========================================================
    // Keep rendering event-driven; Codex will implement masking and _Draw() in a later pass.
    public void Bind(TerrainEdgeController controller)
    {
        if (_controller == controller) return;
        Unbind(); _controller = controller;
        if (_controller != null) _controller.MaterialsRebuilt += QueueRedraw;
        QueueRedraw();
    }

    // =========================================================
    // Release subscriptions when a level unloads; this foundation has no visuals or per-frame work.
    public void Unbind()
    {
        if (GodotObject.IsInstanceValid(_controller)) _controller.MaterialsRebuilt -= QueueRedraw;
        _controller = null;
    }

    public override void _ExitTree() => Unbind();
}
