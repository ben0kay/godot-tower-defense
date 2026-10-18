// Reserves authored Portal/Citadel cells; the root anchors the upper-left footprint cell center.
// Editor-only transform notifications align instances with the painted map, without a gameplay tick.
using Godot;

[Tool]
public partial class WorldObjectFootprint : Node2D
{
    #region Authoring
    [ExportGroup("Grid Footprint")]
    [Export] public Vector2I SizeCells { get; set; } = Vector2I.One;
    [Export] public bool BlocksEnemies { get; set; }
    private bool _snapInEditor = true;
    [ExportGroup("Editor Placement")]
    [Export] public bool SnapToGridInEditor
    {
        get => _snapInEditor;
        set { _snapInEditor = value; QueueEditorSnap(); }
    }
    private bool _snapQueued;
    #endregion

    #region Editor Snapping
    // =========================================================
    // Inherited by Portal and Citadel; standalone assets have no map and retain their local origin.
    public override void _EnterTree()
    {
        SetNotifyTransform(Engine.IsEditorHint());
        QueueEditorSnap();
    }

    // =========================================================
    // Moving an instance or its parent schedules one deferred correction, never a processing loop.
    public override void _Notification(int what)
    { if (what == NotificationTransformChanged) QueueEditorSnap(); }

    // =========================================================
    // Defer until parenting/transforms settle and coalesce repeated editor drag notifications.
    private void QueueEditorSnap()
    {
        if (!Engine.IsEditorHint() || !_snapInEditor || !IsInsideTree() || _snapQueued) return;
        _snapQueued = true;
        Callable.From(ApplyEditorSnap).CallDeferred();
    }

    // =========================================================
    // Resolve the nearest enclosing map's native tile layer; no tool-mode WorldLevel/Grid cast needed.
    private void ApplyEditorSnap()
    {
        _snapQueued = false;
        if (!Engine.IsEditorHint() || !_snapInEditor || !IsInsideTree()) return;
        for (Node ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
        {
            TileMapLayer layer = ancestor.GetNodeOrNull<TileMapLayer>("Terrain/Passable");
            if (layer?.TileSet == null) continue;
            Vector2 snapped = CellCenterFor(layer, GlobalPosition);
            if (!GlobalPosition.IsEqualApprox(snapped)) GlobalPosition = snapped;
            return;
        }
    }

    // =========================================================
    // Native tile conversion honors tile size, grid origin and transformed map/object parents.
    public static Vector2 CellCenterFor(TileMapLayer layer, Vector2 worldPosition)
        => layer.ToGlobal(layer.MapToLocal(layer.LocalToMap(layer.ToLocal(worldPosition))));
    #endregion
}
