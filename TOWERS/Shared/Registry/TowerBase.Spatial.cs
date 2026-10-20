// Optional spatial notifications for the world tower index; no processing tick or weapon coupling.
using Godot;
using System;

public abstract partial class TowerBase
{
    #region Spatial Membership
    internal event Action<TowerBase> SpatialChanged;
    // =========================================================
    // The registry subscribes only while this placed root belongs to its world.
    internal void ObserveSpatialChanges(bool enabled) { SetNotifyTransform(enabled); SetNotifyLocalTransform(enabled); }
    // =========================================================
    // Root movement/reparent transforms update bounds; rotating gun children never invalidate this index.
    public override void _Notification(int what)
    { if ((what == NotificationTransformChanged || what == NotificationLocalTransformChanged) && IsInsideTree()) SpatialChanged?.Invoke(this); }
    #endregion
}
