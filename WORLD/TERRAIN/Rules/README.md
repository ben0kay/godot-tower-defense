# Terrain rules

| Terrain | can_build | blocks_enemies | Layer |
| --- | --- | --- | --- |
| Open metal/dirt floor | true | false | Passable |
| Nonbuildable path | false | false | Passable |
| Raised Foundation | true | true | Interactive |
| Lava | false | false | Interactive |
| Wall/rock | false | true | Impassable |
| Decal | ignored | ignored | Decorations |
| Empty space | false | false | Unpainted |

Metadata defines both rules independently; material/color names do not. WorldGrid applies Passable → Interactive → Impassable, then object reservations. Interactive overlays deliberately replace floor rules, including false build permission; structural blockers remain authoritative. Decorations never contribute rules. A tower's separate occupancy blocks an otherwise-passable cell; preblocked Foundation remains solid before and after construction. Placement still validates terrain, reservations, occupancy, enemy clearance, connectivity and payment.

Interactive interaction metadata identifies foundation/lava. The world manager caches only effective non-structurally-blocked Lava cells and owns environmental exposure. It never substitutes a second blocking authority or alters route weights. Explicit rebuilds reconcile hazard edits through spatial queries; only changed static blocking or Citadel approach cells require navigation invalidation. Spawn-source policy gets a separate cache invalidation notification. Cell size/Boolean metadata validation and diagnostics remain on load/rebuild, with no per-frame tile scan. See ../Interactive/README.md.
