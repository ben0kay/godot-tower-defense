## Tapered energy streak — 2026-10-20

EnemyMinigunTracer.tres now uses LengthPixels=9, CoreThicknessPixels=1.5, Tapered=true, HeadWidthMultiplier=1.25, TailWidthFraction=0.08 and TailOpacity=0.08. Colors/glow remain muted coral red. The fuller rounded front stays at the projectile head; a narrow fading tail trails behind it. These settings are captured at launch and sanitized by the central system. Muzzle flash is unchanged. Defaults for other presets remain straight tracers. Headless geometry/launch checks pass; normal-zoom visual acceptance remains pending. Earlier7×1.3px rectangular settings below are historical.
---

# Enemy minigun visual presets

EnemyMinigunTracer.tres uses the shared ProjectileVisualStyle:7px length,1.3px core,2x faint glow, coral-red body #EF6661, soft peach core #FFB3AB, deep red glow #C83232 at0.18 alpha, no bright tip. EnemyMinigunMuzzleFlash.tres uses the shared MuzzleFlashStyle:5×3px cone,65ms, soft warm orange-red. Both are Inspector editable and referenced by DefaultEnemyMinigun.tres.

Per-weapon initialization/config assignment captures visual values. Central bullets retain their own snapshots; gun flashes use one bounded WeaponVisualRenderer, only after an accepted launch. No material/resource/node is created per shot. Review at gameplay zoom on dark/metal terrain before considering visuals approved; no desktop preview was possible here.
