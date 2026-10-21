// Pure affine ellipse intersection: presentation geometry never changes gameplay contact.
using Godot;
public static class RicochetGeometry
{
    // =========================================================
    // Reconcile gameplay contact with the earlier entry on the visible shield rim along the incoming line.
    public static bool Reflect(Transform2D ellipse, Vector2 from, Vector2 contact, Vector2 direction,
        out Vector2 point, out Vector2 normal, out Vector2 reflected)
    {
        point = normal = reflected = default;
        float determinant = ellipse.Determinant();
        if (!from.IsFinite() || !contact.IsFinite() || !direction.IsFinite() || direction.LengthSquared() < .0001f ||
            !ellipse.Origin.IsFinite() || !ellipse.X.IsFinite() || !ellipse.Y.IsFinite() || !float.IsFinite(determinant) || Mathf.Abs(determinant) < .0001f) return false;
        var inverse = ellipse.AffineInverse();
        Vector2 start = inverse * from;
        direction = direction.Normalized();
        Vector2 localDirection = inverse.BasisXform(direction);
        double a = localDirection.LengthSquared(), b = start.Dot(localDirection), c = start.LengthSquared() - 1;
        double discriminant = b * b - a * c;
        if (c <= 0 || a <= 0 || discriminant <= 0) return false;
        double t = (-b - System.Math.Sqrt(discriminant)) / a;
        double reach = (contact - from).Dot(direction);
        if (!double.IsFinite(t) || t < 0 || t > reach + .01) return false;
        Vector2 local = start + localDirection * (float)t;
        point = ellipse * local;
        normal = new Vector2(inverse.X.X * local.X + inverse.X.Y * local.Y,
            inverse.Y.X * local.X + inverse.Y.Y * local.Y).Normalized();
        if (!point.IsFinite() || !normal.IsFinite() || direction.Dot(normal) >= -.0001f) return false;
        reflected = (direction - 2 * direction.Dot(normal) * normal).Normalized();
        return reflected.IsFinite();
    }
}
