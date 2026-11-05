# Stealth Beacon — split scanner artwork (2026-11-05)

The beacon now has three authored native **64×64** SVGs:

- `stealth_beacon_base.svg` — fixed octagonal teal base, support anchors and circular rotating-bearing socket.
- `stealth_beacon_head.svg` — independently rotatable, directional top scanner with cyan-green lens pointing **up / local -Y**. Pivot and texture centre coincide at local `(0,0)`.
- `stealth_beacon.svg` — updated composite of exactly the same aligned shapes. The tower catalog/icon and inactive `ConstructionSprite` use this composite; it is **not** a third visible layer at runtime when construction is finished.

## Scene wiring (already authored)

`StealthBeacon.tscn` keeps its root `Sprite` name for existing `StealthBeacon.ValidateScene()` and generic presentation dependencies. It is now the **fixed base**. `TurretPivot/BeaconHeadSprite` is the independently rotating head. `TurretPivot/ScanCone` retains its prepared Node2D placeholder path; SE2 creates/reuses a single `Sector` Polygon2D child there at runtime. `ConstructionSprite` is a separate full-composite fallback used only by shared construction presentation.

`TowerConstructionVisuals` uses `ConstructionSprite` as overlay and treats `Sprite` and `TurretPivot` as the finished visual layers. During construction the fixed base/head are hidden and the 0%, 35%, or 70% full-composite construction textures appear; after completion the two finished layers return. One build tier only — no invented purchased upgrades.

The 0%/35%/70% SVGs and `StealthConstructionArt.tres` are under [Construction/](Construction/README.md). The original finished artwork's style (dark sea green, pale teal scan lens, octagonal footprint) is retained; no additional animations, particles, shader or per-frame presentation code was implemented in this art pass.

**SE2 now source implements matching mechanics and visual cone.** Operational head rotation drives capped indexed cone membership (default 70° / 3s revolution / 0.1s refresh / 0.08 alpha); side/behind enemies are not radially revealed. No SVG or existing scene path changed. Owner build/play validation is pending. See [directional scan plan](../DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md).
