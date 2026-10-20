// Reusable local +Y kick/return: one active-only callback, no tweens, timers or per-shot allocations.
using Godot;

public partial class TowerRecoilController : Node
{
    #region Instance State
    private Node2D _target;
    private TowerRecoilStyle _style;
    private Vector2 _rest;
    private TowerRecoilSnapshot _motion;
    private double _elapsed;
    private float _offset, _start, _peak;
    public bool Active { get; private set; }
    public float OffsetPixels => _offset;
    public Vector2 RestPosition => _rest;
    public long Impulses { get; private set; }
    #endregion

    #region Binding and Lifetime
    // =========================================================
    // Scene child is present once; worlds without an enabled preset pay no idle animation processing.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // Replacement restores the previous target before capturing a new baseline, preventing accumulated drift.
    public void Bind(Node2D target, TowerRecoilStyle style)
    {
        Unbind();
        if (!GodotObject.IsInstanceValid(target)) return;
        _target = target; _rest = target.Position; _style = style;
        _target.TreeExiting += OnTargetExiting;
        if (GodotObject.IsInstanceValid(_style)) _style.Changed += Cancel;
    }
    // =========================================================
    // Returning to the exact captured pose also sleeps immediately when a preset is edited or disabled.
    public void Cancel()
    {
        Active = false; _elapsed = 0; _offset = _start = _peak = 0;
        if (GodotObject.IsInstanceValid(_target)) _target.Position = _rest;
        SetProcess(false);
    }
    // =========================================================
    // Explicit teardown supports source removal, target replacement and parent scene unload in either order.
    public void Unbind()
    {
        Cancel();
        if (GodotObject.IsInstanceValid(_target)) _target.TreeExiting -= OnTargetExiting;
        if (GodotObject.IsInstanceValid(_style)) _style.Changed -= Cancel;
        _target = null; _style = null;
    }
    // =========================================================
    // The target can disappear before its controller; release its connection while it is still valid.
    private void OnTargetExiting() => Unbind();
    public override void _ExitTree() => Unbind();
    #endregion

    #region Funded Impulses and Motion
    // =========================================================
    // Caller invokes only after projectile and flash snapshot their launch position; Kick itself moves nothing.
    public void Kick()
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused || !GodotObject.IsInstanceValid(_target)
            || _target.IsQueuedForDeletion() || !_target.IsInsideTree() || !_target.IsVisibleInTree() || !GodotObject.IsInstanceValid(_style)) { Cancel(); return; }
        TowerRecoilSnapshot next = _style.Capture();
        if (!next.Enabled) { Cancel(); return; }
        float accumulated = Active && _elapsed < _motion.KickSeconds ? Mathf.Max(_peak, _offset) : _offset;
        _motion = next; _start = _offset; _peak = Mathf.Min(next.Maximum, accumulated + next.Distance);
        _elapsed = 0; Active = true; Impulses++; SetProcess(true);
    }
    // =========================================================
    // Rendering owns the animation clock; physics/aim/cooldowns and projectile motion remain independent.
    public override void _Process(double delta) => Advance(delta);
    // =========================================================
    // Deterministic smoothstep kick then return, preserving pause and finishing large deltas without residual drift.
    public void Advance(double delta)
    {
        if (!Active || !double.IsFinite(delta) || delta <= 0 || !IsInsideTree() || GetTree().Paused) return;
        if (!GodotObject.IsInstanceValid(_target) || _target.IsQueuedForDeletion() || !_target.IsInsideTree() || !_target.IsVisibleInTree()
            || !GodotObject.IsInstanceValid(_style) || !_style.Enabled) { Cancel(); return; }
        _elapsed += delta;
        if (_elapsed >= _motion.KickSeconds + _motion.ReturnSeconds) { Cancel(); return; }
        if (_elapsed < _motion.KickSeconds) _offset = Mathf.Lerp(_start, _peak, Smooth((float)(_elapsed / _motion.KickSeconds)));
        else _offset = _peak * (1 - Smooth((float)((_elapsed - _motion.KickSeconds) / _motion.ReturnSeconds)));
        _offset = Mathf.Clamp(_offset, 0, _motion.Maximum);
        _target.Position = _rest + Vector2.Down * _offset;
    }
    // =========================================================
    // Zero slope at phase endpoints gives a restrained mechanical settle without oscillation or a spring solver.
    private static float Smooth(float value) { value = Mathf.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
    #endregion
}
