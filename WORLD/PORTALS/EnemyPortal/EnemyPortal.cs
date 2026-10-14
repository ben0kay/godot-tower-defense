// Per-level portal visual size and spawn-spread authoring; grid footprint and spawn timing stay separate.
using Godot;

[Tool]
public partial class EnemyPortal : WorldObjectFootprint
{
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
