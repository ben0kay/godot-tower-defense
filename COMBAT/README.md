# Shared combat

Damage/ owns typed base hit payloads, the single shield/hull resolver and the canonical Inspector-editable response profile. Delivery, targeting, enemy lifecycle, rewards and presentation remain with their existing owners. No armor, effects or enemy-specific resistance overrides are implemented.

Phase 4C-3 is implemented: immutable per-hit absolute shield/hull overrides and shield penetration are resolved centrally. Null inherits type defaults, explicit zero overrides, and legacy1:1 shield-first callers remain supported. See Damage/README.md for numerical fixtures and ownership.

**Phase 4D implemented:** [`AreaDamage/README.md`](AreaDamage/README.md) documents stateless instantaneous disc selection, weapon-input linear falloff, mutation-safe recipient snapshots and defense-payload forwarding. Blaster Cannon supplies its own cached radius/edge retention; projectile timing and cosmetic flash remain separate. Phase4E travelling damage is implemented separately by TOWERS/Shared/Weapons/Shockwaves/ShockwaveEmitter, reusing AtDistance at encounter rather than repeating instant AoE. FEEDBACK/Shockwave is visual-only in both cases.
