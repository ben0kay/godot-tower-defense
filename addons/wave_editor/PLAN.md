# Wave Editor — quality-of-life improvements

**Branch:** `dev` · **Updated:** 2026-10-31

The main-screen `addons/wave_editor` Godot plugin edits `WaveSetResource` `.tres` files, preserving the existing `WaveManager`, enemy catalogue and exact weighted spawn scheduling. This is a living *editor UX* backlog; do not change wave gameplay, level resources or mission registration merely for editor polish. Prefer event-driven updates with no idle `_Process` or `_PhysicsProcess` polling.

## Completed — WP1 (2026-10-31)

| Item | Result |
| --- | --- |
| **Live batch composition** | Shows exact per-entry ship counts and percent bars using the **same** `WaveCatalog.CompositionCounts` largest-remainder algorithm as real spawns, including stable tie breaks. Updates on count, weight, identity/flags, selected batch and entry add/remove/reorder, with no random re-roll or scheduler effects. Distinct shield/stealth/portal entries remain distinguishable. |
| **Context tooltips** | Count, Interval, After, Weight, Wave Number, Portal, Shield, Stealth, Alert, and presentation controls explain values, examples and timing semantics. |
| **Deletion confirmation** | Wave, batch and enemy entry deletions require a modal confirmation. A pending action captures exact resource identity, is cancellable, and cannot silently delete another selected row. The `.tres` isn't saved until Save. |

The resource schema and Level003 wave data remain unchanged; this is mostly editor-side UI behavior. A focused source regression check compares preview quotas with actual composition and stable ties. Godot.NET build and manual editor check pending.

## Backlog — remaining earlier suggestions

| Priority | Idea | Relative effort | Proposed outcome / acceptance |
| --- | --- | --- | --- |
| WP2 | **Enemy sprite & quick stats preview** | Small–medium | Display the selected registered enemy's **existing** visual sprite, display name, HP, max shield, move speed, and possibly Citadel damage; use real EnemyCatalog definitions/art without duplicating gameplay assets or per-frame spinning. Gracefully handle unregistered IDs. |
| WP3 | **Move Add / Duplicate / Delete / Move controls to top** | Small | Place wave/batch action buttons above each list, with clear labels and disabled state where invalid; gain usable screen real estate and improve selection context. Keep existing confirmation dialogs. |
| WP4 | **Wave difficulty summary** | Small | Show each wave's enemy total, expected shielded/stealth entry counts based on exact composition, batch count, and estimated **spawn scheduling** duration (`sum((count-1)*interval + nonfinal after)`). Explain that blocked portals, frame caps and combat time may increase actual duration. No invented numeric 'difficulty score' without balance rules. |
| WP5 | **Optional automatic wave renumbering** | Small | Offer a toggle / one-click renumber after move/insert/delete; default preserve authored numbers and document that list order—not number—governs wave sequence. Never silently renumber an existing level. |
| WP6 | **Save As / Duplicate Wave Set** | Small | Clone the open `.tres` (including nested wave/batch/entry resources) to a new user-selected project path, validate it, avoid overwriting without confirmation, and leave original source untouched. New level still needs `LevelConfig.WaveSource` assigned manually. |
| WP7 | **Enemy search/filter in chooser** | Small–medium | Search by display name or stable ID above/within the catalogue dropdown. Preserve the exact stored ID, show deprecated/missing IDs explicitly, and refresh choices only on demand or input changes. |

## Later, separate data-model proposal

**Exact Counts / Weighted Mix per batch** (medium): Let authors choose exact composition by directly entering e.g. 10 Standard + 5 Scout + 2 Bulwark, or existing weighted proportions. Decide how to reconcile total batch Count, mixed fixed/weighted entries, shuffle/ordered choice, save schema/versioning, and existing Level003 resource compatibility **before** modifying the runtime. Do not conflate this with the implemented exact-*quota* preview (which still edits *weights*).

**Optional timeline visualization** (medium): If scheduling grows complex, show the expected batch spawn/after timing line (not pathfinding or combat ETA), with no continuous editor updates.

## Implementation guardrails

- Keep `WaveSetResource -> WaveDefinition` conversion and `WaveCatalog.Compose` authoritative.
- Never change or silently rewrite existing Level003 waves when adding editor controls.
- No new runtime managers, per-frame editor redraw/poll or enemy scene instantiation on every preview update.
- Preserve the existing one-commit-per-task `dev` branch workflow and keep short, focused validation.
