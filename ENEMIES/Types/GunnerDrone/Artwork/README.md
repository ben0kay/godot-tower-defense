## Fleet art palette upgrade — 2026-11-06

The 48×30 `gunner_drone.svg` and 12×44 `gunner_minigun.svg` were repolished as true source SVGs to match the same **flat near-black / charcoal / restrained red** family as Standard Drone, with small red wing markers, a darkened independent turret and compact scarlet barrel/motor markings. There are no gradients, filters, textures or added animated art layers. The existing authored right-facing hull shape, turret socket `(-2,0)` and muzzle `(0,-18)` are preserved. Source checks only; review in Godot at native zoom.

---

# Gunner Drone artwork

## Starter gunner — second silhouette refinement (2026-11-05)

The entry-level Gunner has been redrawn as a visibly **smaller, slimmer 48×30 hull** (previously 52×38). A narrow main fuselage and short swept stabilizers make it look like a lightweight starter ship rather than an armoured medium gunship. It retains the +X nose, centered SVG, dark steel colour scheme, orange engine accent and local gun socket at **(-2, 0)**.

The separate minigun has been **redrawn from scratch**, not merely scaled narrower: **12×44 px** instead of 20×40. A single long exposed barrel (from SVG y=4 to y=26) and compact breech/motor housing distinguish it from larger turret weapons. The gun remains up-facing in its own SVG and initially faces +X using the existing pivot rotation.

**Critical muzzle registration:** SVG centre is `(6,22)`, barrel tip is `(6,4)`, and the existing scene Marker2D remains `(0,-18)`. Firing still originates at the physical barrel tip. No scene transform, weapon script, bullet/tracer sprite, muzzle flash, targeting, recoil, movement, damage or other combat settings were changed.

Original filenames, import metadata UIDs and scene hierarchy are retained, so Godot reloads the art without asset migration. The hull's smaller native size also influences the existing shared auto-size thruster visual. Existing **135 HP**, **18px movement radius**, **75px/s speed** and Standard/Common/Fighter classification are intentionally unchanged. Source/reference checks only; normal zoom acceptance pending.

## Earlier compact pass (2026-11-04)

The first revision reduced the original 64×48 / 24×40 visuals to 52×38 / 20×40 and reduced health from 180 to 135 (25%) with the radius from 22 to 18. The present refinement changes **artwork only**, keeping that earlier balance.
