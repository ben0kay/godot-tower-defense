// Shared opt-in aiming for weapon towers; sprite artwork points up at rest.
using Godot;

public static class TurretAiming
{
    // =========================================================
    // Turn the visual pivot along the shortest arc; return whether its barrel is aligned.
    public static bool Track(Node2D pivot, Vector2 target, float speedDegrees,
        float toleranceDegrees, bool rotate, double delta)
    {
        Vector2 direction = target - pivot.GlobalPosition;
        if (direction.LengthSquared() < 0.0001f) return true;
        float desired = direction.Angle() + Mathf.Pi * 0.5f;
        float error = Mathf.AngleDifference(pivot.GlobalRotation, desired);
        if (rotate)
            pivot.GlobalRotation += Mathf.Clamp(error, -Mathf.DegToRad(Mathf.Max(0f, speedDegrees)) * (float)delta,
                Mathf.DegToRad(Mathf.Max(0f, speedDegrees)) * (float)delta);
        return Mathf.Abs(Mathf.AngleDifference(pivot.GlobalRotation, desired))
            <= Mathf.DegToRad(Mathf.Clamp(toleranceDegrees, 0f, 180f));
    }
}
