// Immutable per-request source policy; missing wave/batch/entry overrides retain the legacy sampler and RNG path.
using System;

public enum SpawnSourcePolicy { LegacyPortalPreferred, AnyEligible, PortalsOnly, EdgesOnly, RequiredPortal }
public sealed record SpawnSourceOptions(SpawnSourcePolicy Policy = SpawnSourcePolicy.LegacyPortalPreferred,
    string EdgeSector = "", double TopWeight = 1, double BottomWeight = 1, double LeftWeight = 1, double RightWeight = 1)
{
    public static readonly SpawnSourceOptions Legacy = new();
    public void Validate(string portal = "")
    {
        if (portal == null) throw new ArgumentException("Portal selector cannot be null.");
        if (!Enum.IsDefined(Policy) || EdgeSector == null || EdgeSector != EdgeSector.Trim() ||
            !Valid(TopWeight) || !Valid(BottomWeight) || !Valid(LeftWeight) || !Valid(RightWeight))
            throw new ArgumentException("Invalid source policy, sector ID or side weights (0–1000000).");
        if (Policy == SpawnSourcePolicy.RequiredPortal && string.IsNullOrEmpty(portal))
            throw new ArgumentException("RequiredPortal needs the enemy entry's exact Portal path.");
        if (portal.Length > 0 && (Policy == SpawnSourcePolicy.EdgesOnly || EdgeSector.Length > 0))
            throw new ArgumentException("An exact Portal cannot be combined with an edge-only request.");
        if (EdgeSector.Length > 0 && Policy != SpawnSourcePolicy.EdgesOnly)
            throw new ArgumentException("A required edge sector needs EdgesOnly policy.");
        if (Policy == SpawnSourcePolicy.EdgesOnly && TopWeight + BottomWeight + LeftWeight + RightWeight <= 0)
            throw new ArgumentException("EdgesOnly needs at least one positive side weight.");
    }
    private static bool Valid(double value) => double.IsFinite(value) && value >= 0 && value <= 1000000;
    public double Weight(LevelConfig.MapEdge edge) => edge switch
    {
        LevelConfig.MapEdge.Top => TopWeight, LevelConfig.MapEdge.Bottom => BottomWeight,
        LevelConfig.MapEdge.Left => LeftWeight, LevelConfig.MapEdge.Right => RightWeight, _ => 0
    };
}
