# Instant blast visual migrated

The former BlastFlash class and its hardcoded orange ring have been removed. Instant impact presentation now uses [`../Shockwave/README.md`](../Shockwave/README.md): one shared ShockwaveEffect with a weapon-supplied ShockwaveStyle.

Blaster owns its orange style under TOWERS/Offensive/BlasterCannon/Effects. SplashProjectile still applies instantaneous AoE exactly once at contact, then spawns a cosmetic expanding ring. A true travelling damage front is a separate emitter under TOWERS/Shared/Weapons/Shockwaves; the visual never damages anything.
