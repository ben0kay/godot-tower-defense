// Isolated projectile-only random stream and deterministic heading helper; one angular sample at launch, none during travel.
using Godot;

public static class ProjectileLaunch
{
    #region Launch Sampling
    private static readonly RandomNumberGenerator Random = CreateRandom();
    // =========================================================
    // Never consume wave/spawn randomness, or create one RNG/node for every projectile.
    private static RandomNumberGenerator CreateRandom() { var random = new RandomNumberGenerator(); random.Randomize(); return random; }
    // =========================================================
    // Tests can supply a normalized sample in [-1,1]; zero spread consumes no RNG and stays exactly precise.
    public static Vector2 Direction(Vector2 forward, float spreadDegrees, float? sample = null)
    {
        if (!forward.IsFinite() || forward.LengthSquared() <= .000001f) forward = Vector2.Up;
        forward = forward.Normalized();
        float spread = float.IsFinite(spreadDegrees) ? Mathf.Clamp(spreadDegrees, 0, 180) : 0;
        if (spread == 0) return forward;
        float unit = sample ?? Random.RandfRange(-1, 1);
        unit = float.IsFinite(unit) ? Mathf.Clamp(unit, -1, 1) : 0;
        return forward.Rotated(Mathf.DegToRad(spread * unit));
    }
    #endregion
}
