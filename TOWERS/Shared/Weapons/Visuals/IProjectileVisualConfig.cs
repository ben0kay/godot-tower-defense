// Aimed projectile configs opt into presentation without adding weapon fields to passive buildings.
public interface IProjectileVisualConfig
{
    ProjectileVisualStyle ProjectileVisual { get; }
    MuzzleFlashStyle MuzzleFlash { get; }
}
