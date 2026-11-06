# Drone Station — 2×3 buildable hangar (FD1-A)

**Source implemented on `dev` (2026-11-06), awaiting owner gameplay/art acceptance.** The station foundation is a normal **placeable/selectable/constructible/sellable/damageable/saveable** utility building, not yet a fighter production facility. **Footprint is exactly `Vector2i(2,3)`**: **2 grid cells across X** (128px) and **3 grid cells down Y** (192px) for a 64px/cell level. The centered top-down sprite's overall native canvas matches that footprint.

## Files and ownership

- `DroneStation.cs`: minimal, passive `TowerBase` specialization; construction/durability/disabled/sale/save/Inspector are inherited. No fighter AI, ammo tank, station inventory, launch timer, target scanning or active tick.
- `DroneStationConfig.cs` and `DefaultDroneStationConfig.tres`: Inspector-editable provisional **600 Credits**, **8s** construction and **1,800 HP**. No purchased upgrades/levels yet; passive `Levels` are intentionally empty. No power draw or ammo regeneration claimed.
- `DroneStation.tscn`: one centered `Sprite` and shared `TowerConstructionVisuals`; no independently ticking visual components. `DroneStationDefinition.tres`: stable `drone_station` Tower ID, `Utility` category, `Footprint = Vector2i(2,3)`; registered in `TOWERS/DefaultTowerCatalog.tres`.
- `Artwork/drone_station.svg` (128×192) is a new detailed, dark steel-blue/graphite **twin docking-bay** facility with illuminated cyan runways/lanes, maintenance bays, amber safety stripes, structural edge plating and a central launch/guide trench. **The small fighter-like markings inside the bays are flat artwork**, not real fighter instances; Codex should eventually anchor physical fighters to matching spots.
- `Artwork/stage0_foundation.svg`, `stage1_frame.svg`, `stage2_assembly.svg` each use the **same full 128×192 canvas** to avoid footprint jumps. Their 0%/35%/70% thresholds and original finished art at 100% are owned by `Artwork/DroneStationConstructionArt.tres`. Standard construction drones, site telemetry and holograms are inherited, not copied here.

## Current behaviour and limitations

Place via existing **Build → Utility → Drone Station** (same category grouping as other utility towers). On build, ordinary `ConstructionWorkManager` and construction drones create the site; at completion, station remains an inert but selectable structural building, paying no ammo/energy and doing no autonomous processing. Supports generic tower selection/health/repair/disable/sale/persistence conventions automatically. Description intentionally says **“Fighter systems planned (FD1-B)”**, rather than implying fighters can already dock/launch.

**No fighter spawning, slot reservation, patrol UI, ammunition consumption, resupply or repair-service code exists yet.** The station itself takes damage/repair using normal TowerBase mechanics; it does not yet service fighter HP. If desired, Codex can add stations' fighter-owner slot/config fields in FD1-B using the existing placed-station instance and saved `SaveId`.

The station has no built-in flight controller. Suggested future docking/launch positions at native image x=39 and x=90, y=97, mapped to local **(-25,+1)** and **(+26,+1)**. These are **visual candidate anchors only**, not logical Marker2Ds or functional docking locations yet. Codex should author actual `Marker2D` slots only when fighter movement/capacity exists.

## Codex handoff — next phase is FD1-B

**Do not re-create the station or move its scripts to `TOWERS/`.** Use the existing `DRONES/Combat/Station/` assets, build ID `drone_station`, centered 2×3 footprint and standard `TowerBase` ownership. Implement one actual fighter, launch/return/slot logic, visual fighter model and station safe-cleanup on sale/destroy next. Amend station config with genuine capacity/stock only when needed; retain per-instance config copies, saving, and construction/busy gating. Later FD2–FD6 cover Auto-pilot, Auto-patrol, weapons, finite logistics and full fighter persistence. [Full plan](../FIGHTER_DRONES_IMPLEMENTATION_PLAN.md).

**Minimal checks:** updated `TESTS/Towers/TowerCatalogChecks.cs` source expectations for catalog, dimensions, config, stages and passive scene. Test not executed on GitHub; owner to build/import in Godot and test placement on free 2×3 area, proper centre/ghost/collision, builder stages, selection and selling.
