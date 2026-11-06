// Per-level portal visual size and spawn-spread authoring; grid footprint and spawn timing stay separate.
using Godot;

[Tool]
public partial class EnemyPortal : WorldObjectFootprint
{
    /// <summary>0: visible at deployment. Positive: reveal when this authored wave Number starts.</summary>
    [ExportGroup("Wave Activation (0 = START)")]
    [Export(PropertyHint.Range, "0,10000,1,or_greater")] public int AppearAtWave { get; set; }
    public bool IsSpawnActive { get; private set; } = true;

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
    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (AppearAtWave < 0) warnings.Add("Appear At Wave must be 0 (START) or a positive authored wave Number.");
        if (RequiresCheckpoint && ResolveCheckpoint() == null)
            warnings.Add("Required Route Checkpoint path is missing or does not reference a RouteCheckpoint node.");
        return warnings.ToArray();
    }

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
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); UpdateVisualSize(); SetupPresentation();
        if (!Engine.IsEditorHint()) ResetActivation();
    }

    // =========================================================
    // Hide presentation only: footprint/exit/checkpoint nodes remain registered and protected.
    internal void ResetActivation()
    {
        IsSpawnActive = AppearAtWave == 0;
        GetNode<Node2D>("Visuals").Visible = IsSpawnActive;
        if (!IsSpawnActive) StopSpawnFeedback();
    }
    internal bool ActivateForWave(int number)
    {
        if (IsSpawnActive || AppearAtWave != number) return false;
        IsSpawnActive = true; GetNode<Node2D>("Visuals").Show(); return true;
    }

    // =========================================================
    // Scale only presentation; centered SpawnExit and logical grid footprint remain fixed.
    private void UpdateVisualSize()
    {
        Node2D visuals = GetNodeOrNull<Node2D>("Visuals");
        if (visuals != null) visuals.Scale = Vector2.One * SizeMultiplier;
    }
}
