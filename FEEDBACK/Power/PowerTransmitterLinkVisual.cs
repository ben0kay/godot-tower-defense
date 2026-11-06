// One visual per reciprocal pair: cached line geometry, subtle pulses at bounded cadence, asleep offscreen/inactive.
using Godot;
using System;

public partial class PowerTransmitterLinkVisual : Node2D
{
    private Vector2 _end;
    private bool _owned, _inView;
    private PowerLinkVisualState _state;
    private VisibleOnScreenNotifier2D _notifier;
    private PowerDistributionVisualConfig _style;
    private double _phase, _elapsed;
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); ZIndex = 2;
        _style = PowerDistributionVisualConfig.Shared;
        _notifier = new VisibleOnScreenNotifier2D(); AddChild(_notifier);
        _notifier.ScreenEntered += Entered; _notifier.ScreenExited += Exited;
    }
    public void Present(Vector2 end, bool owned, PowerLinkVisualState state)
    {
        if (_owned == owned && _end == end && _state == state) return;
        _owned = owned; _end = end; _state = state; Visible = owned;
        if (_notifier != null) _notifier.Rect = new Rect2(Vector2.Zero.Min(end), end.Abs()).Grow(12);
        if (!owned || state != PowerLinkVisualState.Powered) { _phase = 0; _elapsed = 0; }
        RefreshProcessing(); QueueRedraw();
    }
    private void Entered() { _inView = true; RefreshProcessing(); }
    private void Exited() { _inView = false; RefreshProcessing(); }
    private void RefreshProcessing() => SetProcess(_owned && _inView && IsVisibleInTree() && _state == PowerLinkVisualState.Powered);
    public override void _Notification(int what)
    { if (what == NotificationVisibilityChanged && IsInsideTree()) RefreshProcessing(); }
    public override void _Process(double delta)
    {
        // Scene pause freezes this local cosmetic clock. No queries, allocations, energy or route rebuilding.
        _phase = (_phase + delta / Math.Max(1, _style.PulseSeconds)) % 1;
        _elapsed += delta; if (_elapsed < 1 / Math.Clamp(_style.AnimationHz, 5, 30)) return;
        _elapsed = 0; QueueRedraw();
    }
    public override void _Draw()
    {
        if (!_owned || _style == null) return;
        float width = Mathf.Max(.5f, _style.CoreWidth);
        if (_state != PowerLinkVisualState.Powered)
        { DrawDashedLine(Vector2.Zero, _end, _state == PowerLinkVisualState.Suspended ? _style.Suspended : _style.Unpowered, width, 10); return; }
        DrawLine(Vector2.Zero, _end, _style.PoweredHalo, Mathf.Max(width, _style.HaloWidth), true);
        DrawLine(Vector2.Zero, _end, _style.PoweredCore, width, true);
        float length = _end.Length(); if (length < 1) return;
        Vector2 direction = _end / length;
        for (int i = 0; i < 2; i++)
        {
            float t = (float)((_phase + i * .5) % 1);
            float alpha = Mathf.Sin(t * Mathf.Pi) * .65f;
            Color pulse = new(.65f, .95f, 1f, alpha);
            DrawLine(direction * Mathf.Max(0, length * t - 14), direction * (length * t), pulse, width + 1, true);
        }
    }
}
