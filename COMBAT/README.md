# Shared combat

Damage/ owns typed base hit payloads, the single shield/hull resolver and the canonical Inspector-editable response profile. Delivery, targeting, enemy lifecycle, rewards and presentation remain with their existing owners. No armor, effects or enemy-specific resistance overrides are implemented.

Phase 4C-3 is implemented: immutable per-hit absolute shield/hull overrides and shield penetration are resolved centrally. Null inherits type defaults, explicit zero overrides, and legacy1:1 shield-first callers remain supported. See Damage/README.md for numerical fixtures and ownership.

**PLANNED — Phase 4D:** [`AreaDamage/README.md`](AreaDamage/README.md) now specifies a stateless reusable *instant radial* target/falloff helper. The Blaster Cannon will supply the radius and falloff from its own cached weapon stats. This is not implemented C# yet; timed expanding wavefront delivery is separately reserved for future Phase 4E.
