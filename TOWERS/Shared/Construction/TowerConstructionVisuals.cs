// Event-driven construction texture presenter; optional split finished layers preserve existing single-sprite behavior.
using Godot;
using System;
using System.Collections.Generic;

public partial class TowerConstructionVisuals : Node
{
    [Export] public NodePath SpritePath { get; set; } = new();
    [Export] public NodePath FinishedBaseSpritePath { get; set; } = new();
    [Export] public NodePath FinishedGunSpritePath { get; set; } = new();
    [Export] public Godot.Collections.Array<NodePath> FinishedVisualPaths { get; set; } = new();
    private readonly List<CanvasItem> _finished = new();
    private TowerBase _tower;
    private Sprite2D _sprite, _baseSprite, _gunSprite;
    private Texture2D _originalBase, _originalGun;
    private TowerConstructionArt _art;
    private Texture2D _original;
    private bool _watchingProgress;
    private bool? _lastSplit;
    public int TextureChanges { get; private set; }
    public event Action PresentationChanged;

    #region Presentation Lifecycle
    // =========================================================
    // Child Ready reads paid construction state before the tower's Ready; no poll or work-state owner is added.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _tower = GetParent() as TowerBase;
        _sprite = SpritePath.IsEmpty ? null : GetNodeOrNull<Sprite2D>(SpritePath);
        if (_tower == null || _sprite == null) return;
        _baseSprite = FinishedBaseSpritePath.IsEmpty ? null : GetNodeOrNull<Sprite2D>(FinishedBaseSpritePath);
        _gunSprite = FinishedGunSpritePath.IsEmpty ? null : GetNodeOrNull<Sprite2D>(FinishedGunSpritePath);
        _originalBase = _baseSprite?.Texture; _originalGun = _gunSprite?.Texture;
        if (FinishedVisualPaths != null) foreach (NodePath path in FinishedVisualPaths)
        {
            CanvasItem layer = path.IsEmpty ? null : GetNodeOrNull<CanvasItem>(path);
            if (layer != null && layer != _sprite && !_finished.Contains(layer)) _finished.Add(layer);
        }
        _art = _tower.AuthoredConfig?.ConstructionArt; _original = _sprite.Texture;
        _tower.WorkStarted += Refresh; _tower.WorkCompleted += Refresh; Refresh();
    }

    // =========================================================
    // Existing art resolver remains the sole threshold/tier authority; paid work keeps the old tier until completion.
    private void Refresh()
    {
        if (!GodotObject.IsInstanceValid(_tower) || !GodotObject.IsInstanceValid(_sprite)) return;
        bool busy = _tower.IsBusy;
        if (busy != _watchingProgress)
        {
            if (busy) _tower.WorkProgressChanged += Refresh; else _tower.WorkProgressChanged -= Refresh;
            _watchingProgress = busy;
        }
        // Apply tier-specific split art before resolving the optional monolithic construction overlay.
        if (_baseSprite != null) _baseSprite.Texture = _art?.FinishedBaseTexture(_tower.UpgradeLevel, _originalBase) ?? _originalBase;
        if (_gunSprite != null) _gunSprite.Texture = _art?.FinishedGunTexture(_tower.UpgradeLevel, _originalGun) ?? _originalGun;
        Texture2D finished = _art?.FinishedTexture(_tower.UpgradeLevel, _original) ?? _original;
        Texture2D texture = _tower.WorkPhase switch
        {
            TowerWorkPhase.Constructing => _art?.BuildTexture(_tower.WorkProgress, finished) ?? finished,
            TowerWorkPhase.Upgrading => _art?.UpgradeTexture(_tower.PendingUpgradeLevel, _tower.WorkProgress, finished) ?? finished,
            _ => finished
        };
        if (_sprite.Texture != texture) { _sprite.Texture = texture; TextureChanges++; }
        ShowLayers(texture);
    }

    // =========================================================
    // Default finished art uses split layers; authored monolithic stages/custom tier art remain a compatible overlay.
    private void ShowLayers(Texture2D texture, bool notify = true)
    {
        if (_finished.Count == 0) return; // Existing presenters never have their visibility changed.
        bool split = texture == _original;
        _sprite.Visible = !split;
        foreach (CanvasItem layer in _finished) if (GodotObject.IsInstanceValid(layer)) layer.Visible = split;
        if (_lastSplit == split) return;
        _lastSplit = split; if (notify) PresentationChanged?.Invoke();
    }

    // =========================================================
    // Removing only the presenter restores completed imagery; no work/progress callbacks survive teardown.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_tower))
        {
            _tower.WorkStarted -= Refresh; _tower.WorkCompleted -= Refresh;
            if (_watchingProgress) _tower.WorkProgressChanged -= Refresh;
            if (GodotObject.IsInstanceValid(_sprite))
            {
                _sprite.Texture = _art?.FinishedTexture(_tower.UpgradeLevel, _original) ?? _original;
                ShowLayers(_sprite.Texture, false);
                if (GodotObject.IsInstanceValid(_baseSprite)) _baseSprite.Texture = _art?.FinishedBaseTexture(_tower.UpgradeLevel, _originalBase) ?? _originalBase;
                if (GodotObject.IsInstanceValid(_gunSprite)) _gunSprite.Texture = _art?.FinishedGunTexture(_tower.UpgradeLevel, _originalGun) ?? _originalGun;
            }
        }
        _watchingProgress = false; PresentationChanged = null; _finished.Clear();
    }
    #endregion
}
