# Island power inspection
**Source Implemented / Awaiting Owner Validation.**

Faint world-space outlines follow occupied conducting islands, including holes. Player power consumers, generators, Relays/Transmitters and registered objectives make an island relevant; unfinished/disabled buildings retain their outline. Empty islands stay quiet.

Hover within 7 screen pixels of a boundary for 0.25s to reveal a screen-space cursor tooltip and temporary tower footprint highlights. Leaving has a 0.15s grace. Camera motion, dragging, zoom, UI hover and pause dismiss inspection without consuming input. Red means depleted/recovering with no recharge; amber means a sustained smoothed consumption/recharge deficit. Normally charging new towers are not flagged. Tower materials, selection and disable tints are never modified.

Tooltip separates local generation, available shared network supply on transmitter-linked islands, observed consumption, delivered tower recharge and aggregate tower buffers. These are not battery storage or measured island import/export flows. Rates are sampled every 0.25s only while inspected and smoothed over roughly 0.75s; amber needs a 1s deficit. No power is settled or allocated by UI reads.

All presentation lives here. `DefaultIslandPowerInspectionStyle.tres` controls enable, delays, hit/line widths, sample cadence, warning hold and colours. `PowerGridManager.IslandInspection.cs` supplies read-only snapshots from existing simulation membership/accounting. Shared World composition includes the controller; levels require no edits.

Boundary geometry is merged/cached per power terrain revision. Membership events coalesce outline visibility changes; cursor tests inspect only nearby cell edges. No map/tower scans per rendered frame, no per-tower UI process, no new gameplay clock or saved state. Processing/input sleeps when no island is relevant; teardown removes subscriptions and tooltip children.

Lightweight source/resource review only: .NET is unavailable. Owner: inspect an occupied island, build/remove power infrastructure on another island, hover near holes, link/disconnect transmitters, provoke a shortage, pan/zoom, open UI/pause and restore a checkpoint.
