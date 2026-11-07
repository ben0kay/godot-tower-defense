// Admits a bounded number of cosmetic shot lights into spare slots of the world light pool.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private static readonly NodePath LightingPath = new("../../Lighting");
    private readonly List<Source> _projectileSources = new();

    internal sealed class ProjectileLight(WorldLightingController owner, bool hostile, Color tint)
    {
        private WorldLightingController _owner = owner;
        internal PointLight2D Light;
        internal readonly bool Hostile = hostile;
        internal readonly Color Tint = tint;

        // =========================================================
        // Called only by shots that obtained a lease, using the existing travel update.
        internal void Move(Vector2 position)
        {
            if (Light != null) Light.GlobalPosition = position;
        }

        internal void Release() => _owner?.ReleaseProjectile(this);
        internal void Detach() { Light = null; _owner = null; }
    }

    // =========================================================
    // Friendly projectiles live under World/Projectiles. Standalone fixtures have no lighting owner.
    internal static ProjectileLight TrackFriendly(Node2D shot, Color tint) =>
        shot.GetNodeOrNull<WorldLightingController>(LightingPath)?.TryTrackProjectile(shot.GlobalPosition, tint, false);

    // =========================================================
    // Admit at launch from spare capacity. Leave room for the opposite side when the category allows it.
    internal ProjectileLight TryTrackProjectile(Vector2 position, Color tint, bool hostile)
    {
        if (!_lightingEnabled || _ambient == null || !HasOutput(Config.Projectile)) return null;
        int limit = Mathf.Clamp(Config.MaxProjectileLights, 0, 8);
        if (_projectileSources.Count >= limit || !position.IsFinite()) return null;
        if (limit > 1)
        {
            int sideCount = 0;
            foreach (Source source in _projectileSources)
                if (source.Projectile.Hostile == hostile) sideCount++;
            if (sideCount >= limit - 1) return null;
        }
        int occupied = 0;
        foreach (Slot slot in _slots)
            if (slot.Source != null) occupied++;
        if (occupied >= Mathf.Clamp(Config.MaxActiveLights, 0, 64)) return null;

        float radius = Config.Projectile.RadiusPixels;
        if (Config.CullOffscreenLights &&
            !OverlapsLight(position, radius, GetCanvasTransform(), GetViewportRect().Grow(Mathf.Max(0, Config.ScreenMarginPixels))))
            return null;

        ProjectileLight lease = new(this, hostile, WeaponVisualMath.Color(tint));
        Source candidate = new(null, Config.Projectile, position, radius, null, Projectile: lease);
        _projectileSources.Add(candidate);
        Apply(candidate);
        if (Config.CullOffscreenLights && !IsProcessing()) SetProcess(true);
        return lease;
    }

    // =========================================================
    // Drop leases on impact, expiry, culling, pool pressure or F6 OFF.
    private void ReleaseProjectile(ProjectileLight lease)
    {
        for (int i = 0; i < _projectileSources.Count; i++)
            if (ReferenceEquals(_projectileSources[i].Projectile, lease))
            { RemoveProjectile(i); return; }
    }

    private void RemoveProjectile(int index)
    {
        Source source = _projectileSources[index];
        ReleaseSlot(source);
        source.Projectile.Detach();
        _projectileSources.RemoveAt(index);
        _selected.Remove(source);
    }
}
