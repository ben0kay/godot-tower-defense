## Charcoal/scarlet visual pass — 2026-11-06

`heavy_gunner_drone.svg` (62×42) and `heavy_gunner_cannon.svg` (18×50) are now original flat-colour near-black/charcoal vector sprites with restrained scarlet armour identification and small orange-red mechanical detail. They share fleet colours with the starter Gunner and existing newer hulls, while preserving the heavier silhouette and independent barrel. The head muzzle still lies at native `(9,4)`, i.e. Godot `Marker2D(0,-21)`, with gun centred at (9,25) and hull mount `(-2,0)`. Source SVG-only; previous blue-grey gradient art notes are superseded.

---

# Heavy Gunner Drone vector artwork

The native hull SVG `heavy_gunner_drone.svg` is **62×42** with a +X-facing armoured prow, twin aft engines, broad reinforced stabilizers, layered steel-blue armour and reddish engine accents. Its socket is centered at pixel **(29,21)** = `(-2,0)` from hull centre (31,21).

The independent up-facing `heavy_gunner_cannon.svg` is **18×50**, designed as a visibly heavier cousin of the starter gun's 12×44 barrel. Long single central autocannon tube with cooling conduits, reinforced breech and orange-red rotating motor. Barrel tip at pixel **(9,4)** relative to texture centre (9,25): **`Muzzle=(0,-21)`**.

The original starter's `gunner_drone.svg` and `gunner_minigun.svg` are not modified. All art is SVG, uses no embedded fonts/images/runtime shader, and preserves the usual +X ship / -Y gun orientation. Godot imports new SVGs normally on editor refresh; manual at-scale acceptance pending.
