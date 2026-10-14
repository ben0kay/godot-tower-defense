# Terrain rules

| Example | can_build | blocks_enemies | Layer |
| --- | --- | --- | --- |
| Open metal/dirt floor | true | false | Passable |
| Future nonbuildable path | false | false | Passable |
| Raised solid platform | true | true | Impassable |
| Wall/rock | false | true | Impassable |
| Decal | ignored | ignored | Decorations |
| Empty space | false | false | Unpainted |

Metadata defines both rules independently; art/material names do not. Impassable overrides Passable if both cells are painted, including false build permission. Remove the old gameplay cell before replacing it in another layer. Decorations never contributes rules, even over empty space.

WorldGrid reads category layers only on load/explicit terrain rebuild, merges Portal/Citadel footprint reservations, and keeps runtime tower occupancy separate. A tower blocks an otherwise-passable cell; placing/removing one uses incremental navigation revision updates. A pre-blocked platform remains solid. Placement validates terrain, reservations, occupancy, active-enemy clearance, source-to-Citadel connectivity and economy payment.

Load/rebuild checks enforce TileSet cell size and Boolean gameplay metadata and report category mismatches, invalid tiles and overlaps. No runtime auto-conversion/deletion or per-frame terrain scan is performed.
