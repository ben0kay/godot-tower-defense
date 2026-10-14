# Laser Tower — sustained Energy beam (Phase 4C-2)

Ctrl → Laser Tower → place on route-safe terrain. LaserTowerDefinition registers `laser` in DefaultTowerCatalog; generic placement/HUD handle its 1×1 footprint, private config, prices, selection and upgrades without per-type branches. Initial price 250 Credits, range 256px and 20 base DPS are provisional playtest balance. Level2 costs 175 Credits and adds 10 DPS/32px range, producing 30 DPS/288px. Battle Tower remains200 Credits with its existing150/250 upgrade prices.

## Combat and retargeting

LaserTowerConfig owns RangePixels, genuine float DamagePerSecond, DamageType (default Energy), priority, search interval, tracking speed/tolerance and policy. Shared TowerTargeting and TurretAiming are reused. Searches start staggered and repeat every configured interval (default .15s), selecting the current best First/Closest candidate. A living valid lock persists between searches; there is no extra cooldown or charge-up. Invalid range/alignment/zero DPS/Enabled=false stops damage immediately. Valid aligned lock sends effective DPS × physics delta as base typed damage to Enemy.ApplyDamage. DamageResolver applies the global shield/hull coefficients; Laser contains no resistance math.

One tick damages only its captured lock. Target death/arrival/external unload clears subscriptions and hides the beam immediately; periodic search acquires the next candidate. The fixed placement root never rotates. A child up-facing TurretPivot smoothly tracks, with a Marker2D muzzle at its emitter. No wall line-of-sight, heat, audio, charged shots or critical/status effects are implemented.

## Visual ownership and Inspector tuning

BeamRenderer.tscn is one persistent child containing Outer/Core Line2Ds with two reusable points. Laser supplies world-space muzzle/target endpoints and active state. Renderer knows no Enemy, targeting service, damage type, DPS or wallet. Restyle `Beam/DefaultLaserBeamStyle.tres`: CoreColor, OuterColor, CoreWidth, OuterWidth, PulseAmount and PulseFrequency. The outer translucent line is a simple halo, not HDR bloom. Optional shaders/particles/impact presentation can be added inside the renderer later without altering attack/stat code.

Each renderer duplicates its mutable Style on Ready. During play, Remote → World/Towers/LaserTower/BeamRenderer → Style tunes only that visual instance. C#/Remote Inspector property edits refresh its lines, never gameplay caches. Replacing Style unsubscribes the old resource; unloading releases subscriptions. Inactive beams hide and clear endpoints, with cosmetic processing off. Edit the saved style for permanent/new-instance defaults. Config and upgrade tiers remain separately copied per tower.

## Stats and tests

Optional immutable BeamStats on TowerStatBlock/FinalStats carries float DPS through the existing eight stages. Stage 02 applies BeamDps tier operations, Stage 07 clamps invalid/negative DPS, Stage 08 publishes the same cached snapshot used by attack and preview. Original seven-field constructors and Battle adapter remain supported. LaserConfig exports no fictitious damage-per-shot/projectile/fire-rate fields; passive towers still need no combat snapshot.

LaserTowerChecks covers real build-bar placement/payment/selection, private config/tiers, repeated occupancy, real paid upgrade preview parity,30/60/120 Hz shield damage parity, hull efficiency/spillover, First/Closest acquisition, retarget/unload/arrival/death/disabled/alignment/range gates, stable node count, anchored world endpoints and visual/stat independence. Full enemy/navigation/economy/wave/shield/stat/upgrades/catalog/UI regression suites passed headlessly. Manual moving-target visual quality has not been verified in this environment; check muzzle anchoring and beam appearance in a live Godot window before considering visual polish complete.
