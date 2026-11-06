// Tiny retained world-space rearm meter; only actual stock transfers change progress, no private clock.
using Godot;
public partial class FighterServiceVisual : Node2D
{
    private float _progress;
    private bool _waiting;
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); Position = new(25, -12); Hide(); }
    internal void Present(bool visible, bool waiting, float progress)
    {
        progress = Mathf.Clamp(progress, 0, 1);
        bool changed = Visible != visible || _waiting != waiting || Mathf.Abs(_progress - progress) >= .005f;
        Visible = visible; _waiting = waiting; _progress = progress;
        if (changed) QueueRedraw();
    }
    public override void _Draw()
    {
        Color edge = _waiting ? new Color(1, .65f, .2f, .45f) : new Color(.4f, .95f, 1, .5f);
        DrawRect(new Rect2(-1, -1, 6, 26), new Color(.02f, .06f, .08f, .3f));
        DrawRect(new Rect2(0, 0, 4, 24), edge, false, .7f);
        if (!_waiting && _progress > 0) DrawRect(new Rect2(1, 23 - 22 * _progress, 2, 22 * _progress), new Color(.35f, .95f, 1, .8f));
    }
}
