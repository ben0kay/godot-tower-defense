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
