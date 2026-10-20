// One drawing surface for real hostile shots; immutable launch styles and travel direction, with no per-bullet nodes.
using Godot;

public partial class EnemyBulletRenderer : Node2D
{
    private EnemyBulletSystem _system;
    private readonly Vector2[] _streak = new Vector2[EnemyEnergyStreakGeometry.VertexCount];
    private readonly Color[] _colors = new Color[EnemyEnergyStreakGeometry.VertexCount];
    #region Drawing Lifetime
    // =========================================================
    // The world simulation explicitly binds this surface; drawing never owns damage, expiry or offscreen culling.
    public void Bind(EnemyBulletSystem system) { _system = system; QueueRedraw(); }
    // =========================================================
    // Render work is requested only by state changes; there is no idle animation callback.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // One world-space draw pass supports line tracers and optional texture presets using the existing visual snapshot.
    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_system)) return;
        Transform2D transform = GlobalTransform;
        if (!transform.X.IsFinite() || !transform.Y.IsFinite() || !transform.Origin.IsFinite() || transform.Determinant() == 0) return;
        Transform2D worldToLocal = transform.AffineInverse();
        DrawSetTransformMatrix(worldToLocal);
        for (int i = 0; i < _system.ActiveCount; i++)
        {
            EnemyBulletData bullet = _system.Snapshot(i);
            var visual = bullet.Visual; if (!visual.Enabled) continue;
            Vector2 head = bullet.Position, tail = head - bullet.Direction * visual.Length;
            if (GodotObject.IsInstanceValid(visual.Texture))
            {
                DrawSetTransformMatrix(worldToLocal * new Transform2D(bullet.Direction.Angle(), head));
                DrawTextureRect(visual.Texture, new Rect2(-visual.Length, -visual.Width * .5f, visual.Length, visual.Width), false, visual.Body);
                DrawSetTransformMatrix(worldToLocal);
            }
            else if (visual.Tapered)
            {
                if (visual.Glow.A > 0) DrawEnergy(bullet, 1, visual.GlowWidth, visual.Glow);
                DrawEnergy(bullet, 1, 1, visual.Body);
                DrawEnergy(bullet, .85f, .55f, visual.Core);
            }
            else
            {
                if (visual.Glow.A > 0) DrawLine(tail, head, visual.Glow, visual.Width * visual.GlowWidth, true);
                DrawLine(tail, head, visual.Body, visual.Width * 1.6f, true);
                DrawLine(head - bullet.Direction * visual.Length * .75f, head, visual.Core, visual.Width, true);
            }
            if (visual.BrightTip) DrawLine(head - bullet.Direction * visual.Width * 1.5f, head, visual.Core, visual.Width * 1.2f, true);
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }
    // =========================================================
    // Reuse one pair of polygon/color buffers for every glow/body/core pass; shape never changes hit geometry.
    private void DrawEnergy(EnemyBulletData bullet, float lengthScale, float widthScale, Color color)
    {
        EnemyEnergyStreakGeometry.Fill(bullet.Position, bullet.Direction, bullet.Visual, lengthScale, widthScale, color, _streak, _colors);
        DrawPolygon(_streak, _colors);
    }
    // =========================================================
    // Surface teardown releases the world simulation wrapper regardless of sibling exit order.
    public override void _ExitTree() => _system = null;
    #endregion
}
