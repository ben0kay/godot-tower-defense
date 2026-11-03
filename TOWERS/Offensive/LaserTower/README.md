## Laser fixed base and independently rotating emitter — 2026-11-03

Corrected the single-sprite Laser implementation to match the intended split-tower architecture. The original silver chassis is now a **stationary `BaseSprite`** while its energy emitter remains on the existing rotating `TurretPivot/Sprite`. Level 1, 2 and 3 each have distinct split base/gun textures derived from their authored composite SVGs. Existing 0/35/70% initial construction and paid L2/L3 35/70% upgrade artwork now uses a temporary `ConstructionSprite` overlay while the presenter hides both completed layers; completed construction restores the correct split tier automatically.

The physical beam muzzle marker stays under the same `TurretPivot` at (0,-29); targeting, rotation timing, shield damage, DPS, ammo, heat, credit costs, 4/5/6-second work durations and beam rendering were left unchanged. Selection highlight now tints the fixed base, gun and temporary construction image consistently. [Construction artwork](Artwork/Construction/README.md) · [Purchased upgrades](Artwork/Upgrades/README.md). Source-only verification done; visual Godot review pending.

---

## Purchased Level 3 — 2026-11-03

Laser now has **three levels total** (initial Level 1 build followed by paid upgrades to Levels 2 and 3). Level 3 is **250 Credits / 6 seconds**, raising the authored beam output to **16 DPS**, range to **345.4 px**, and maximum health to **1102.5**. The Level 3 silver/yellow high-output emitter art includes a 35% scaffold and 70% calibration image; the latest split-art pass keeps its base stationary. Level 1/2 art, prices and stats are unchanged; no new beam/controller path or per-frame work was introduced. See [upgrade artwork](Artwork/Upgrades/README.md).

---

## Laser Tower Level 1 construction and Level 2 artwork — 2026-11-03

Laser originally used the shared construction presenter on a single rotating sprite; this has since been corrected to **fixed base + rotating emitter** without changing gameplay code. Initial build now shows a bare silver mounting foundation (0%), exposed emitter frame (35%) and partially fitted low-power yellow emitter (70%), restoring the existing Level 1 laser art at 100%. Its paid Level 2 and Level 3 upgrades have distinct 35%/70% intermediate art and unique silver/yellow finished appearances. See [construction](Artwork/Construction/README.md) and [upgrades](Artwork/Upgrades/README.md).

The existing `Muzzle` location (0,-29), 4-second build, Level 2 purchase price/work time (175 Credits, 5s), beam behaviour, shield preference, DPS, heat, ammo and targeting are untouched. Art changes are triggered by paid work progress; no new process loops. Source implemented; Godot visual acceptance pending.

---

## HD4 continuous beam heat (2026-10-26)

The shipped Laser config now enables **3.5-minute** sustained damaging beam heat and **10-second** overheat recovery (35% restart). Calibrated to funded beam seconds, not the zero `ShotsPerSecond` placeholder; idle/lost-target intervals cool normally. Inspector Heat fields can change minutes/recovery or opt out. Existing DPS/energy response, ammo and attack rules are unchanged. Source-implemented; live runtime acceptance pending.

---

## Laser shield preference — implemented 2026-10-18

Laser configs now expose **Targeting → TargetPreference**, default **Shielded** (None opts out). This preference is separate from First/Last/Closest/Farthest/Strongest/Weakest: choose eligible in-range enemies with **CurrentShield > 0** first, apply the chosen mode within that group, and fall back to ordinary eligible targets when no preferred candidate qualifies. First/Last retain full remaining-journey distance across portals; Strongest/Weakest retain current hull + shield scoring and stable registration-order ties.

TowerTargeting.Find accepts an optional preference after its existing required-effect argument. One existing spatial traversal ranks preference tiers before ordinary scores; no second scan/list allocation, new per-frame loop or tower-local pathfinding. Other towers default to None. Eligibility/range/finite-route checks still run before preferences; Cryo's required-effect filter is unchanged. Laser reacquires on its existing staggered interval, including after shield depletion. Damage/beam/ammo/heat/rank values are unchanged.

Future stealth preference can add a real matching rule here once stealth/detection exists; it must not bypass detection/eligibility. No speculative stealth flag, detector or UI button is implemented now. The latest HP/ammo Inspector bars and compact-rank visual commit is preserved.

Validation: .NET SDK build (zero warnings/errors); ShieldTargetingChecks passed, including real Laser acquisition/opt-out/config isolation, six-mode preference/fallback, actual two-portal First/Last, shared route-query budget/RNG invariants and 500-enemy route sharing. PlacementHudChecks and CompactInspectorChecks also passed against the latest HP/ammo-bar Inspector revision. See TESTS/Towers/ShieldTargeting.md.

---

undefined
## Yellow visual redesign — 2026-10-17

The original cyan SVG is replaced with a 64px silver/gunmetal turret accented by yellow rails, glowing reactor and emitter. Its muzzle geometry remains aligned with the existing Marker2D. `Beam/DefaultLaserBeamStyle.tres` now uses a warm yellow/ivory core and yellow-gold halo, with smaller and less uniform, smoothly seeded (allocation-free) wobble and contact wander. Shared `BeamRenderer` remains weapon-agnostic. No combat, target priority, ammo, price, shield multipliers or construction changes; verify appearance at normal game zoom after pulling.
