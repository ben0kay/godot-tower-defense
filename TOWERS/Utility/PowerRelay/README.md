## PW5-C — distribution visuals and inspection — 2026-11-05

**Source Implemented / Awaiting Owner Visual Validation.** Selecting a Power Relay now shows faint cyan filled/outlined **reachable conductive cells**, plus a thin dashed shortest route to a live source and the label **Nearest live source / Island supply shared**. No circular coverage spanning gaps. Inactive/source-less Relays show no coverage/route. Deselecting hides the diagnostic immediately. The route follows cached conductive distance and positive Transmitter hops when supply is remote; ties are deterministic and a source footprint terminates the path.

Powered Transmitter pairs now have a narrow cyan core, faint halo and two restrained travelling highlights. Unpowered retained links are dashed blue-grey; suspended links are dashed amber; unlink/removal hides the line. Only the lower-SaveId endpoint owns a renderer. Highlights are cosmetic, not an exclusive source or measured energy-flow direction. No power generation, allocation, booster, hop cost, consumer charge, combat, construction or save-format changes.

Selected distribution Inspector status remains bounded to three lines, with concise state, physical island / effective network / shared supply, plus peer identity/island or Relay boost coverage/ceiling. F4's existing throttled power row now distinguishes hovered physical-island output from joined-network supply and global active pair count. No new diagnostic polling loop or growing status rows.

**Tuning:** `FEEDBACK/Power/DefaultPowerDistributionVisuals.tres` exposes coverage fill/edge, source path/dash length, link core/halo/status colors and widths, pulse period (default 2.8s) and animation cadence (15 Hz default; clamped 5–30). Restart/redeploy after authoring style changes. Art and simulation settings stay with their existing owners.

**Performance/lifecycle:** Relay geometry uses retained buffers and caches by delivery revision/selected transform, and only builds while selected. Coverage extraction reuses the bounded conductive search scratch; route reconstruction descends the existing cost field without a second source search. Straight route sections collapse to one drawn segment; exceptional paths cap at 4,096 steps with an explicit preview-capped label. No per-frame map traversal/pathfinding. Link geometry updates on lifecycle/transform/state events. Only an onscreen powered pair processes its small local cosmetic clock; redraw is capped at 15 Hz by default, processing sleeps offscreen/hidden/unpowered/suspended, and inherited scene pause freezes animation. Static visuals rely on normal canvas culling. Removing a tower destroys its owned child visuals; no autoload or new power service.

**Validation:** minimal source/diff and native resource-reference checks only; .NET unavailable. No C# build, full Godot gameplay/visual acceptance or FPS benchmark is claimed. Owner: pull/rebuild, load Level011, select a powered Relay near a gap, then select/deselect and remove its source. Pair Transmitters, inspect active/unpowered/EMP-suspended styles, pan away/back, pause and try a fresh save/load. PW6/new generators and the optional priority slider remain deferred.


---

## PW5-A handoff — 2026-11-05

Implemented in Utility with provisional 150 Credits / 3s / 800 HP / 1×1, cyan live/dim inactive core and ordinary staged construction. `DefaultPowerRelayConfig.tres` exposes construction values, `RangeCells=10` and `BoostEfficiency=0.85`. Best-only coverage follows conducting cells on the same physical island and requires a source route; linked remote supply qualifies, but boost coverage never crosses the gap. Original source/hop scarcity distance and finite generation budget are unchanged. No pairing/upkeep/generation or Relay-to-generator line. Source/resource/SVG review only; .NET unavailable. [Implemented accounting, save and owner checks](../../Economy/Power/IMPLEMENTATION_PLAN.md). PW5-C selected coverage/source-route visuals remain planned.

---

# Power Relay — PW5-A LOCAL recharge booster

**Source Implemented / Awaiting Owner Validation — 2026-11-05.** Relay is a distinct small Utility building that improves the local possible recharge rate of tower capacitors on its **own** powered conducting island. **No Relay pairing, cross-island beam or power transport.** For that use the separate [Power Transmitter](../PowerTransmitter/README.md).

Only a completed, operational Relay on a valid conductive footprint with reachable live Citadel/generator output is eligible. It gives **zero generation** and has no island battery; the existing `PowerGridManager` still caps delivered energy by finite supply and prioritizes original source distance under scarcity. Configurable boosted coverage uses conductive-path cells, not geometric circles across space/Lava. A Relay improves the local PW4-B ceiling up to nominal; overlaps take the best eligible value, never stack or amplify endlessly. Removing/disabling a Relay restores original falloff but leaves stored tower charge intact.

**Essential PW5-A visuals:** cyan powered core, dim unpowered/disabled; selection/Inspector identifies BOOSTING, NO LIVE SOURCE, DISCONNECTED, or DISABLED. Reuse shared disabled presenter. **PW5-C adds:** selection-only cyan overlay following actual conducting cells and a thin dashed nearest-live-source diagnostic route; in islands with multiple generators the trail is not an exclusive ownership link. Avoid permanent generator spaghetti lines and per-frame full-map traversal.

Codex: reuse tower construction/placement/catalog, existing manager/cache/disable/save; do not add transmitters or pairing in PW5-A. Focused acceptance: same-island boosted far consumer, finite scarcity, invalid topology/diagonal gaps, overlapping Relays, source outage, EMP/work/destroy and save/load. Initial costs/distance/boost editable and explicitly provisional. [Authoritative detailed scope](../../Economy/Power/IMPLEMENTATION_PLAN.md).

Older November 4 Relay-as-paired-bridge text is superseded. Do not implement that historical behavior.
