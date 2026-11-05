# Power Relay — PW5-A LOCAL recharge booster

**Design approved / not implemented — 2026-11-05.** Relay is a distinct small Utility building that improves the local possible recharge rate of tower capacitors on its **own** powered conducting island. **No Relay pairing, cross-island beam or power transport.** For that use the separate [Power Transmitter](../PowerTransmitter/README.md).

Only a completed, operational Relay on a valid conductive footprint with reachable live Citadel/generator output is eligible. It gives **zero generation** and has no island battery; the existing `PowerGridManager` still caps delivered energy by finite supply and prioritizes original source distance under scarcity. Configurable boosted coverage uses conductive-path cells, not geometric circles across space/Lava. A Relay improves the local PW4-B ceiling up to nominal; overlaps take the best eligible value, never stack or amplify endlessly. Removing/disabling a Relay restores original falloff but leaves stored tower charge intact.

**Essential PW5-A visuals:** cyan powered core, dim unpowered/disabled; selection/Inspector identifies BOOSTING, NO LIVE SOURCE, DISCONNECTED, or DISABLED. Reuse shared disabled presenter. **PW5-C adds:** selection-only cyan overlay following actual conducting cells and a thin dashed nearest-live-source diagnostic route; in islands with multiple generators the trail is not an exclusive ownership link. Avoid permanent generator spaghetti lines and per-frame full-map traversal.

Codex: reuse tower construction/placement/catalog, existing manager/cache/disable/save; do not add transmitters or pairing in PW5-A. Focused acceptance: same-island boosted far consumer, finite scarcity, invalid topology/diagonal gaps, overlapping Relays, source outage, EMP/work/destroy and save/load. Initial costs/distance/boost editable and explicitly provisional. [Authoritative detailed scope](../../Economy/Power/IMPLEMENTATION_PLAN.md).

Older November 4 Relay-as-paired-bridge text is superseded. Do not implement that historical behavior.
