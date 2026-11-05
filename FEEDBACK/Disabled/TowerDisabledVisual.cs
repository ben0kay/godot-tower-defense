// Static warning badge and composable sprite tint for one affected tower; no processing/animation loop.
using Godot;
using System.Collections.Generic;

public partial class TowerDisabledVisual : Node2D
{
    private TowerBase _tower;
    private TowerDisabledStyle _style;
    private readonly List<(Sprite2D Sprite, Color Original)> _art = new();
    private bool _tinted;
    private float _cellSize = 64;
    private readonly Vector2[] _triangle = new Vector2[3];

    // =========================================================
    // Cache native artwork once. Scripted feedback/weapon-effect branches and GUI are not artwork.
    public void Bind(TowerBase tower)
    {
        _tower = tower;
        _style = GD.Load<TowerDisabledStyle>("res://FEEDBACK/Disabled/DefaultTowerDisabledStyle.tres");
        CaptureArt(tower);
        _cellSize = tower.GetNodeOrNull<WorldGrid>("../../Grid")?.Config.CellSize ?? 64;
        tower.WorkStarted += Refresh; tower.WorkCompleted += Refresh; tower.Destroyed += Refresh;
        SetProcess(false); SetPhysicsProcess(false); ZIndex = 20;
        Refresh();
    }
    private void CaptureArt(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child == this || child is Control || child is CanvasLayer || child.GetScript().VariantType != Variant.Type.Nil) continue;
            if (child is Sprite2D sprite) _art.Add((sprite, sprite.SelfModulate));
            CaptureArt(child);
        }
    }

    // =========================================================
    // Hide initial construction sites; operational disabled buildings and paid upgrades show the warning.
    // SelfModulate preserves selection Modulate and alpha, including split/monolithic construction textures.
    public void Refresh()
    {
        if (!GodotObject.IsInstanceValid(_tower) || _style == null) return;
        bool disabled = _tower.IsDisabled && !_tower.IsDestroyed && !_tower.IsQueuedForDeletion()
            && _tower.WorkPhase != TowerWorkPhase.Constructing;
        if (_tinted != disabled)
        {
            foreach (var item in _art)
                if (GodotObject.IsInstanceValid(item.Sprite)) item.Sprite.SelfModulate = disabled ? item.Original * _style.ArtTint : item.Original;
            _tinted = disabled;
        }
        Visible = disabled;
        if (!disabled) return;
        Position = new Vector2(_tower.Footprint.X * _cellSize * .5f, -_tower.Footprint.Y * _cellSize * .5f) + _style.CornerOffset;
        float half = Mathf.Clamp(_style.WarningSizePixels, 10, 28) * .5f;
        _triangle[0] = new(0, -half); _triangle[1] = new(half, half * .8f); _triangle[2] = new(-half, half * .8f);
        QueueRedraw();
    }

    // =========================================================
    // Vector triangle and exclamation remain clear at normal zoom without fonts/textures or timers.
    public override void _Draw()
    {
        if (!_tinted || _style == null) return;
        DrawColoredPolygon(_triangle, new Color(0.16f, 0.055f, 0.025f, .95f));
        DrawLine(_triangle[0], _triangle[1], _style.WarningColor, 1.6f, true);
        DrawLine(_triangle[1], _triangle[2], _style.WarningColor, 1.6f, true);
        DrawLine(_triangle[2], _triangle[0], _style.WarningColor, 1.6f, true);
        float half = Mathf.Clamp(_style.WarningSizePixels, 10, 28) * .5f;
        DrawLine(new(0, -half * .38f), new(0, half * .18f), _style.WarningColor, 2, true);
        DrawCircle(new(0, half * .48f), 1, _style.WarningColor);
    }

    // =========================================================
    // Restore captured artwork exactly; repeated disable sources never multiply tint cumulatively.
    public void Detach()
    {
        if (GodotObject.IsInstanceValid(_tower)) { _tower.WorkStarted -= Refresh; _tower.WorkCompleted -= Refresh; _tower.Destroyed -= Refresh; }
        foreach (var item in _art) if (GodotObject.IsInstanceValid(item.Sprite)) item.Sprite.SelfModulate = item.Original;
        _art.Clear(); _tower = null; _tinted = false; Hide();
    }
    public override void _ExitTree() => Detach();
}
