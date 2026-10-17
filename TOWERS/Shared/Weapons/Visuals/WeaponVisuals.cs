// Common firing presentation adapter: one renderer, one launch position, no gameplay ownership.
using Godot;
public static class WeaponVisuals
{
    // =========================================================
    // Empty styles remain compatible; renderers have no idle processing and are owned by the rotating pivot.
    public static WeaponVisualRenderer Attach(Node2D pivot)
    { var renderer = new WeaponVisualRenderer { Name = "WeaponVisuals" }; pivot.AddChild(renderer); return renderer; }
    // =========================================================
    // Called only after ammunition funds this projectile. The bullet remains independent of its source tower.
    public static void Present(BasicProjectile bullet, Marker2D muzzle, IProjectileVisualConfig config, WeaponVisualRenderer renderer)
    { bullet.LaunchFrom(muzzle.GlobalPosition); renderer.FlashAt(muzzle, config?.MuzzleFlash); }
}
