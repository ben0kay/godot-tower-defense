// Backward-compatible tower scene/config adapter; shared weapon-neutral code alone owns recoil motion.
using Godot;

public partial class TowerRecoilController : WeaponRecoilController
{
    [Export] public NodePath TargetPath { get; set; } = new();
    private TowerRecoilStyle _sceneStyle;
    private TowerBase _owner;
    [Export] public TowerRecoilStyle Style
    {
        get => _sceneStyle;
        set
        {
            if (_sceneStyle == value) return; _sceneStyle = value;
            if (IsNodeReady() && !TargetPath.IsEmpty) Bind(GetNodeOrNull<Node2D>(TargetPath), value);
        }
    }

    // =========================================================
    // Retain the original tower lifecycle, including Battle's explicit config-owned binding.
    public override void _Ready()
    {
        base._Ready();
        if (TargetPath.IsEmpty) return;
        Bind(GetNodeOrNull<Node2D>(TargetPath), Style);
        _owner = GetParent() as TowerBase;
        if (_owner != null) { _owner.DisableStateChanged += OnOwnerDisabled; _owner.Destroyed += Cancel; }
    }
    private void OnOwnerDisabled() { if (_owner.IsDisabled) Cancel(); }
    public void Bind(Node2D target, TowerRecoilStyle style) => base.Bind(target, style);
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_owner)) { _owner.DisableStateChanged -= OnOwnerDisabled; _owner.Destroyed -= Cancel; }
        _owner = null; base._ExitTree();
    }
}
