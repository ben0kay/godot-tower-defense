// One reusable draw node per turret. Fixed flash slots and one optional aiming line; no per-shot nodes/timers.
using Godot;

public partial class WeaponVisualRenderer : Node2D
{
    public const int FlashCapacity = 32;
    private struct Flash { public Vector2 Position, Forward; public float Age; public bool Fresh; public MuzzleFlashSnapshot Style; }
    private readonly Flash[] _flashes = new Flash[FlashCapacity];
    private readonly Vector2[] _shape = new Vector2[8];
    private int _count, _overflow;
    private Vector2 _aimStart, _aimEnd;
    private AimLaserSnapshot _aim;
    public bool AimVisible { get; private set; }
    public Vector2 AimStart => _aimStart;
    public Vector2 AimEnd => _aimEnd;
    public int ActiveFlashes => _count;
    public long TotalFlashes { get; private set; }
    public long OverwrittenFlashes { get; private set; }
    public Vector2 LastFlashPosition { get; private set; }
    public MuzzleFlashSnapshot LastFlashStyle { get; private set; }

    // =========================================================
    // Existing tower physics supplies aim changes. Only live flash animation needs this node's processing.
    public override void _Ready() { ZIndex = 3; SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // Funded launches call once per selected barrel; local coordinates track parent rotation exactly once.
    public void FlashAt(Marker2D muzzle, MuzzleFlashStyle style)
    {
        if (!GodotObject.IsInstanceValid(muzzle) || !GodotObject.IsInstanceValid(style)) return;
        MuzzleFlashSnapshot snapshot = style.Capture(); if (!snapshot.Enabled || snapshot.Opacity <= 0) return;
        Vector2 position = ToLocal(muzzle.GlobalPosition);
        Vector2 forward = (ToLocal(muzzle.GlobalPosition - muzzle.GlobalTransform.Y) - position).Normalized();
        int slot;
        if (_count < FlashCapacity) slot = _count++;
        else { slot = _overflow; _overflow = (_overflow + 1) % FlashCapacity; OverwrittenFlashes++; }
        _flashes[slot] = new() { Position = position, Forward = forward, Fresh = true, Style = snapshot };
        LastFlashPosition = position; LastFlashStyle = snapshot; TotalFlashes++;
        SetProcess(true); QueueRedraw();
    }
    // =========================================================
    // Aiming follows barrel forward toward target distance, not a fake hitscan or a damaging beam.
    public void SetAim(Marker2D muzzle, Vector2 targetWorld, float range, AimLaserStyle style, bool active)
    {
        bool show = active && GodotObject.IsInstanceValid(style) && GodotObject.IsInstanceValid(muzzle)
            && targetWorld.IsFinite() && float.IsFinite(range) && range > 0;
        AimLaserSnapshot snapshot = show ? style.Capture() : default; show &= snapshot.Enabled;
        if (!show) { if (AimVisible) { AimVisible = false; QueueRedraw(); } return; }
        Vector2 from = muzzle.GlobalPosition;
        Vector2 to = from - muzzle.GlobalTransform.Y.Normalized() * Mathf.Min(range, from.DistanceTo(targetWorld));
        Vector2 start = ToLocal(from), end = ToLocal(to);
        if (!AimVisible || start != _aimStart || end != _aimEnd || snapshot != _aim)
        { _aimStart = start; _aimEnd = end; _aim = snapshot; AimVisible = true; QueueRedraw(); }
    }
    // =========================================================
    // Reuse compact slots and stop processing immediately after the final flash expires.
    public override void _Process(double delta)
    {
        if (_count == 0 || !double.IsFinite(delta) || delta <= 0) return;
        for (int i = _count - 1; i >= 0; i--)
        {
            // A shot emitted during physics must survive its first process pass, even after a frame hitch.
            if (_flashes[i].Fresh) { _flashes[i].Fresh = false; continue; }
            _flashes[i].Age += (float)delta;
            if (_flashes[i].Age >= _flashes[i].Style.Duration)
            { _flashes[i] = _flashes[--_count]; _flashes[_count] = default; }
        }
        QueueRedraw(); if (_count == 0) SetProcess(false);
    }
    // =========================================================
    // Brief bright hold followed by linear fade stays readable at gameplay zoom without adding light nodes.
    public static float FlashOpacity(float age, float duration) => Mathf.Clamp((1 - age / duration) / .7f, 0, 1);

    // =========================================================
    // All draw data is reused. Outer translucent geometry is faux glow, independent of environment bloom.
    public override void _Draw()
    {
        if (AimVisible)
        { DrawLine(_aimStart, _aimEnd, _aim.Color, _aim.Width, true); if (_aim.TipRadius > 0) DrawCircle(_aimEnd, _aim.TipRadius, _aim.Color); }
        for (int i = 0; i < _count; i++)
        {
            Flash flash = _flashes[i]; float alpha = FlashOpacity(flash.Age, flash.Style.Duration) * flash.Style.Opacity;
            DrawFlash(flash, 1, flash.Style.Outer * new Color(1, 1, 1, alpha));
            DrawFlash(flash, .55f, flash.Style.Core * new Color(1, 1, 1, alpha));
        }
    }
    // =========================================================
    // Fundamental shape primitives are shared across arbitrary tower-owned presets.
    private void DrawFlash(Flash flash, float scale, Color color)
    {
        var style = flash.Style; float length = style.Length * scale, width = style.Width * scale;
        Vector2 side = flash.Forward.Orthogonal();
        if (style.Shape == MuzzleFlashShape.Ring)
        { DrawArc(flash.Position + flash.Forward * length * .25f, width * .5f, 0, Mathf.Tau, 16, color, scale, true); return; }
        if (style.Shape == MuzzleFlashShape.Texture && GodotObject.IsInstanceValid(style.Texture))
        {
            DrawSetTransform(flash.Position, flash.Forward.Angle());
            DrawTextureRect(style.Texture, new Rect2(0, -width * .5f, length, width), false, color);
            DrawSetTransform(Vector2.Zero); return;
        }
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Tau / 8;
            float radius = (i & 1) == 0 ? 1 : .3f;
            float x = Mathf.Cos(angle) * length * radius, y = Mathf.Sin(angle) * width * .5f * radius;
            if (style.Shape == MuzzleFlashShape.Cone)
            {
                Vector2 corner = i switch { 0 => new(0, -.18f), 1 => new(.8f, -.5f), 2 => new(.6f, -.12f),
                    3 => new(1, 0), 4 => new(.6f, .12f), 5 => new(.8f, .5f), 6 => new(0, .18f), _ => new(.15f, 0) };
                x = corner.X * length; y = corner.Y * width;
            }
            _shape[i] = flash.Position + flash.Forward * x + side * y;
        }
        DrawColoredPolygon(_shape, color);
    }
}
