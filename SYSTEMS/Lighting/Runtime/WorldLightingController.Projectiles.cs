// Admits a bounded number of cosmetic shot lights into spare slots of the world light pool.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private static readonly NodePath LightingPath = new("../../Lighting");
    private readonly List<Source> _projectileSources = new();
    private Transform2D _projectileCanvas;
    private Rect2 _projectileViewport;
    private uint _projectileRevealRevision;
    private bool _projectileRevealVisible;

    internal sealed class ProjectileLight(WorldLightingController owner, bool hostile, Color tint, float radius)
    {
        private WorldLightingController _owner = owner;
        internal PointLight2D Light;
        internal Vector2 Position;
        internal readonly float Radius = radius;
        internal readonly bool Hostile = hostile;
        internal readonly Color Tint = tint;

        // =========================================================
        // Called only by shots that obtained a lease, using the existing travel update.
        internal void Move(Vector2 position)
        {
            Position = position;
            _owner?.UpdateProjectileLight(this);
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
            !OverlapsLight(position, radius, GetCanvasTransform(), GetViewportRect()))
            return null;

        ProjectileLight lease = new(this, hostile, WeaponVisualMath.Color(tint), radius) { Position = position };
        Source candidate = new(null, Config.Projectile, position, radius, null, Projectile: lease);
        _projectileSources.Add(candidate);
        Apply(candidate);
        if (!IsProcessing()) SetProcess(true);
        return lease;
    }

    // =========================================================
    // Movement already visits each leased shot. Whole-light culling releases the same pooled slot.
    private void UpdateProjectileLight(ProjectileLight lease) =>
        UpdateProjectileLight(lease, GetCanvasTransform(), GetViewportRect());

    private void UpdateProjectileLight(ProjectileLight lease, Transform2D canvas, Rect2 viewport)
    {
        if (lease.Light == null) return;
        if (Config.CullOffscreenLights && !OverlapsLight(lease.Position, lease.Radius, canvas, viewport))
        {
            ReleaseProjectile(lease);
            return;
        }
        if (lease.Light.GlobalPosition != lease.Position) lease.Light.GlobalPosition = lease.Position;
        bool enabled = ProjectileLightOverlapsReveal(lease.Position, lease.Radius);
        if (lease.Light.Enabled != enabled) lease.Light.Enabled = enabled;
    }

    private bool ProjectileLightOverlapsReveal(Vector2 position, float radius)
    {
        if (!RevealVisible) return true;
        EnsureRevealField();
        // Keep light spill wherever bilinear mask filtering can leave visible projectile pixels.
        Vector2 texel = _maskBounds.Size / new Vector2(Mathf.Max(1, _maskWidth), Mathf.Max(1, _maskHeight));
        return _reveal.OverlapsDisk(position, radius + texel.Length());
    }

    // =========================================================
    // Camera and reveal changes share the existing world loop, bounded by at most eight leases.
    private void RefreshProjectileLightVisibility()
    {
        if (_projectileSources.Count == 0) return;
        if (RevealVisible) EnsureRevealField();
        Transform2D canvas = GetCanvasTransform();
        Rect2 viewport = GetViewportRect();
        if (_projectileCanvas == canvas && _projectileViewport == viewport &&
            _projectileRevealRevision == _revealRevision && _projectileRevealVisible == RevealVisible) return;
        _projectileCanvas = canvas;
        _projectileViewport = viewport;
        _projectileRevealRevision = _revealRevision;
        _projectileRevealVisible = RevealVisible;
        for (int i = _projectileSources.Count - 1; i >= 0; i--)
            UpdateProjectileLight(_projectileSources[i].Projectile, canvas, viewport);
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
