// Optional event-driven Sprite2D presenter. No work state, combat, physics, VFX, polling or per-frame node creation.
using Godot;

public partial class TowerConstructionVisuals : Node
{
    [Export] public NodePath SpritePath { get; set; } = new();
    private TowerBase _tower;
    private Sprite2D _sprite;
    private TowerConstructionArt _art;
    private Texture2D _original;
    private bool _watchingProgress;
    public int TextureChanges { get; private set; }

    // =========================================================
    // Child Ready reads the construction state set before placement; directly instantiated scenes retain final art.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _tower = GetParent() as TowerBase;
        _sprite = SpritePath.IsEmpty ? null : GetNodeOrNull<Sprite2D>(SpritePath);
        _art = _tower?.AuthoredConfig?.ConstructionArt;
        if (_tower == null || _sprite == null || _art == null) return;
        _original = _sprite.Texture;
        _tower.WorkStarted += Refresh; _tower.WorkCompleted += Refresh;
        Refresh();
    }

    // =========================================================
    // Read existing progress only; assign the shared texture reference solely when the visible image changes.
    private void Refresh()
    {
        if (!GodotObject.IsInstanceValid(_tower) || !GodotObject.IsInstanceValid(_sprite)) return;
        bool busy = _tower.IsBusy;
        if (busy != _watchingProgress)
        {
            if (busy) _tower.WorkProgressChanged += Refresh; else _tower.WorkProgressChanged -= Refresh;
            _watchingProgress = busy;
        }
        Texture2D finished = _art.FinishedTexture(_tower.UpgradeLevel, _original);
        Texture2D texture = _tower.WorkPhase switch
        {
            TowerWorkPhase.Constructing => _art.BuildTexture(_tower.WorkProgress, finished),
            TowerWorkPhase.Upgrading => _art.UpgradeTexture(_tower.PendingUpgradeLevel, _tower.WorkProgress, finished),
            _ => finished
        };
        if (_sprite.Texture == texture) return;
        _sprite.Texture = texture; TextureChanges++;
    }

    // =========================================================
    // Safe for rollback, mid-work removal and whole-world unloading; no callbacks survive the presenter.
    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_tower)) return;
        _tower.WorkStarted -= Refresh; _tower.WorkCompleted -= Refresh;
        if (_watchingProgress) _tower.WorkProgressChanged -= Refresh;
        _watchingProgress = false;
        // Removing only this optional component must not leave a half-built image stuck on the live tower.
        if (GodotObject.IsInstanceValid(_sprite) && _art != null) _sprite.Texture = _art.FinishedTexture(_tower.UpgradeLevel, _original);
    }
}
