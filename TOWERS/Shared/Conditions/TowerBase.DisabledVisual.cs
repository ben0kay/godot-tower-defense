// Shared lazy disabled presentation; all placed tower types use the same condition state and visual helper.
using Godot;

public abstract partial class TowerBase
{
    private TowerDisabledVisual _disabledVisual;
    private bool _disabledVisualReady;

    // =========================================================
    // Attach only after concrete Ready has finished creating the tower's native artwork.
    private void InitializeDisabledVisual() { _disabledVisualReady = true; RefreshDisabledVisual(); }
    private void RefreshDisabledVisual()
    {
        if (!_disabledVisualReady || !IsInsideTree() || IsQueuedForDeletion() || IsDestroyed) return;
        if (_disabledVisual == null && IsDisabled)
        {
            _disabledVisual = new TowerDisabledVisual { Name = "DisabledVisual" };
            AddChild(_disabledVisual); _disabledVisual.Bind(this);
        }
        _disabledVisual?.Refresh();
    }

    // =========================================================
    // Unsubscribe before disposing power/conditions can publish final teardown changes.
    private void DetachDisabledVisual()
    {
        _disabledVisualReady = false;
        if (GodotObject.IsInstanceValid(_disabledVisual)) _disabledVisual.Detach();
        _disabledVisual = null;
    }
}
