# Enemy minigun visual presets

EnemyMinigunTracer.tres uses the shared ProjectileVisualStyle:7px length,1.3px core,2x faint glow, coral-red body #EF6661, soft peach core #FFB3AB, deep red glow #C83232 at0.18 alpha, no bright tip. EnemyMinigunMuzzleFlash.tres uses the shared MuzzleFlashStyle:5×3px cone,65ms, soft warm orange-red. Both are Inspector editable and referenced by DefaultEnemyMinigun.tres.

Per-weapon initialization/config assignment captures visual values. Central bullets retain their own snapshots; gun flashes use one bounded WeaponVisualRenderer, only after an accepted launch. No material/resource/node is created per shot. Review at gameplay zoom on dark/metal terrain before considering visuals approved; no desktop preview was possible here.
