// Per-level portal visual size and spawn-spread authoring; grid footprint and spawn timing stay separate.
using Godot;

[Tool]
public partial class EnemyPortal : WorldObjectFootprint
{
    [ExportGroup("Route Checkpoint")]
    private NodePath _checkpointPath = new("");
    [Export] public NodePath CheckpointPath
    { get => _checkpointPath; set { _checkpointPath = value ?? new NodePath(""); if (IsInsideTree()) UpdateConfigurationWarnings(); } }
    public bool RequiresCheckpoint => !CheckpointPath.IsEmpty;
    // =========================================================
    // A configured broken link remains required; it must never silently become a direct Citadel route.
    public RouteCheckpoint ResolveCheckpoint() => RequiresCheckpoint ? GetNodeOrNull<RouteCheckpoint>(CheckpointPath) : null;

    // =========================================================
    // Broken authored links are visible in the editor instead of silently dropping a hard requirement.
    public override string[] _GetConfigurationWarnings() => RequiresCheckpoint && ResolveCheckpoint() == null
        ? new[] { "Required Route Checkpoint path is missing or does not reference a RouteCheckpoint node." } : System.Array.Empty<string>();

    private float _sizeMultiplier = 1f;
    [ExportGroup("Portal Size")]
    [Export(PropertyHint.Range, "0.1,16,0.1")] public float SizeMultiplier
    {
        get => _sizeMultiplier;
        set
        {
            _sizeMultiplier = float.IsFinite(value) ? Mathf.Clamp(value, 0.1f, 16f) : 1f;
            UpdateVisualSize();
        }
    }

    // =========================================================
    // Apply authored overrides in the editor and game without any processing or navigation rebuild.
    public override void _Ready() => UpdateVisualSize();

    // =========================================================
    // Scale only presentation; centered SpawnExit and logical grid footprint remain fixed.
    private void UpdateVisualSize()
    {
        Node2D visuals = GetNodeOrNull<Node2D>("Visuals");
        if (visuals != null) visuals.Scale = Vector2.One * SizeMultiplier;
    }
}
