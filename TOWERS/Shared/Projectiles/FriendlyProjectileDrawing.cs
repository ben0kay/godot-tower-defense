// Draw the same tracer and fallback artwork for the shared buffer and standalone adapter.
using Godot;

internal sealed class FriendlyProjectileDrawing
{
    private readonly Vector2[] _streak = new Vector2[EnemyEnergyStreakGeometry.VertexCount];
    private readonly Color[] _colors = new Color[EnemyEnergyStreakGeometry.VertexCount];

    // =========================================================
    // Geometry is in the caller's current draw space. Only textured shots need a per-shot draw transform.
    internal void Draw(CanvasItem canvas, in FriendlyProjectileFlight shot, Vector2 head, Vector2 forward, Transform2D drawTransform)
    {
        if (!shot.Visual.HasValue)
        {
            canvas.DrawCircle(head, 3f, shot.Effect.HasValue ? new Color(.35f, .9f, 1f) : new Color(1f, .85f, .35f));
            return;
        }
        var style = shot.Visual.Value;
        if (!style.Enabled) return;
        Vector2 tail = head - forward * style.Length;
        if (GodotObject.IsInstanceValid(style.Texture))
        {
            canvas.DrawSetTransformMatrix(drawTransform * new Transform2D(forward.Angle(), head));
            canvas.DrawTextureRect(style.Texture, new Rect2(-style.Length, -style.Width * .5f, style.Length, style.Width), false, style.Body);
            canvas.DrawSetTransformMatrix(drawTransform);
        }
        else if (style.Tapered)
        {
            if (style.Glow.A > 0) DrawTapered(canvas, head, forward, style, 1, style.GlowWidth, style.Glow);
            DrawTapered(canvas, head, forward, style, 1, 1, style.Body);
            DrawTapered(canvas, head, forward, style, .85f, .55f, style.Core);
        }
        else
        {
            canvas.DrawLine(tail, head, style.Glow, style.Width * style.GlowWidth, true);
            canvas.DrawLine(tail, head, style.Body, style.Width * 1.6f, true);
            canvas.DrawLine(head - forward * style.Length * .75f, head, style.Core, style.Width, true);
        }
        if (style.BrightTip) canvas.DrawLine(head - forward * style.Width * 1.5f, head, style.Core, style.Width * 1.2f, true);
    }

    private void DrawTapered(CanvasItem canvas, Vector2 head, Vector2 forward, ProjectileVisualSnapshot style,
        float lengthScale, float widthScale, Color color)
    {
        EnemyEnergyStreakGeometry.Fill(head, forward, style, lengthScale, widthScale, color, _streak, _colors);
        canvas.DrawPolygon(_streak, _colors);
    }
}
