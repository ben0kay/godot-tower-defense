## 2026-10-30 — full-height Wave Editor and basic authoring walkthrough

**Layout repair:** Godot's `EditorInterface.GetEditorMainScreen()` is a `VBoxContainer`, which sizes child controls using their size flags. The plugin previously set `FullRect` anchors but **not** the required `SizeFlagsVertical = ExpandFill`, so the editor collapsed into a strip about 200 px tall and hid its wave/batch lists. The panel now explicitly sets both horizontal and vertical `ExpandFill` and keeps `FullRect` anchors. The details pane now places primary wave/batch/enemy controls **above** the optional narrator/banner fields, so most authoring requires less scrolling. The right scrollbar belongs only to those details, not to the whole workspace. No waves, resources, serializers, save paths or gameplay behavior changed.

**Quick usage:** The top bar identifies the active `.tres` file (initially `Level003Waves.tres`). Click a **Wave** on the left, then a **Batch** in the center. On the right, edit Wave number and Batch Count, Interval (seconds between successful spawns), and After (seconds after its final spawn). Choose an **enemy entry** within the batch and edit EnemyId (e.g. `basic_mk1`, `scout_mk1`, `medium_mk1`), Weight (relative proportion within the batch, not number of ships), optional Portal, Shield, and Stealth. Add/Duplicate/Delete/Move buttons affect the currently selected row. Additional presentation fields are near the bottom of the right scroll area. **Save** persists the validated resource; **Reload** discards unsaved changes after confirmation. Next level load reads updated waves; the editor does not reconfigure a level already running.

Example: **Wave 1 → Batch 1 → Count 12 to 20 → Save** gives Level003 eight more Standard Drones in that batch. If you change a batch to Count 20 and have two enemy entries with weights 75 and 25, that batch spawns exactly 15 of the first and 5 of the second, shuffled by the existing wave scheduler.

This was corrected in source; the resized Godot editor workspace and the changed field order still need a local editor UI check. No automated .NET/Godot build was available during this pass.

---

## 2026-10-29 — first C# compile fix

Renamed the Wave Editor panel's `ItemList(...)` factory to `CreateItemList(...)`, avoiding a C# member-name collision with `Godot.ItemList.SelectModeEnum.Single` (CS0119 at line 128). The three wave, batch and enemy list factory calls were updated. No editing or wave-data behavior changed. Source-checked only; rebuild in Godot .NET to verify and report any subsequent errors.

---

# Wave Editor — Godot main-screen plugin (pilot)

**Source implemented 2026-10-29 on `dev`; Godot/.NET build and editor UI acceptance pending.** This is a Godot **EditorPlugin**, not an in-game menu. It adds a `Wave Editor` workspace button beside Godot's 2D / 3D / Script views. Its UI is a single editor-only, event-driven `WaveEditorPanel` instance, not gameplay scene nodes.

## Activate

1. Pull `dev`; use the project's **Godot .NET editor** (the project targets Godot.NET.Sdk/4.7.2).
2. **Build the C# project** in Godot / MSBuild so its `[Tool]` resource classes and editor plugin are available.
3. Open **Project → Project Settings → Plugins**, then enable **Wave Editor**. A new **Wave Editor** top workspace tab appears.
4. Click the tab. It opens `res://LEVELS/Level003/Level003Waves.tres` as a pilot. You can also click **Open .tres** and choose another editable `WaveSetResource` file, or **New .tres** to create one in a level folder.
5. Select a wave on the left, a batch in the center, and an enemy entry in the details pane. Edit total batch count, seconds between successful spawns, delay after batch, enemy catalog ID, relative weight, optional required portal, shield/stealth, wave alert/banner/narration. Add/duplicate/delete/reorder waves and batches; add/duplicate/delete entry rows.
6. Click **Save**. Production `WaveCatalog.Validate` checks unknown enemy IDs, invalid entries/counts/timings/weights and required portal path format before writing native Godot `.tres`. Fix validation errors shown at the bottom of the tab. **Reload** discards unsaved changes only after confirmation. **Open/New** refuse to lose unsaved work.
7. Press **F6/Play** to test the existing level. Godot resource data, rather than newly compiled per-level C# wave recipes, now controls Level003.

## Architecture

```text
addons/wave_editor/
  plugin.cfg               # Godot's main-screen registration
  WaveEditorPlugin.cs      # [Tool] EditorPlugin, panel lifetime / tab visibility
  WaveEditorPanel.cs       # [Tool] resizable event-driven editing interface

LEVELS/Waves/Resources/
  WaveSetResource.cs       # WaveSetSource.Build() converts .tres into existing immutable runtime waves
  WaveResource.cs          # Number, ordered Batches, optional WavePresentation
  WaveBatchResource.cs     # Count, Interval, After, weighted Entries
  WaveEnemyResource.cs     # EnemyId, Weight, Portal, Shield, Stealth

LEVELS/Level003/
  Level003Waves.tres       # pilot 4-wave authoring data
  Level003Config.tres      # WaveSource points to the new resource
```

The editor itself never spawns enemies or advances wave clocks. `WaveCatalog`, `WaveManager`, enemy spawning, weighted composition, shield/stealth, narration and manual wave starts are **unchanged**. All other levels remain C# authored; this file type is optional per level. Level003's original `Level003Waves.cs` is kept as a reference/comparison fixture and is **not** the active Level003 source.

**Important:** editing a wave changes source asset data; a level already running will not reconfigure its live wave schedule until the next level load. `Level003.tscn` remains deliberately blank and still requires connected passable tiles to be playable.

## Scope / follow-up

This first editor provides functional authoring, not drag-and-drop wave timelines, a visual spawn preview, automatic wave-number renumbering, Undo/Redo, enemy thumbnails, live-hot-reload or port-existence validation against the map. Reordering *moves* numbered waves; edit the Number field to make their identifiers match the desired new order. The validation step checks registered enemy identities using the current EnemyCatalog. Keep wave numbers unique/positive and provide at least one batch with an enemy entry.

The plugin and resource classes have no `_Process` loops; lists refresh only on changes. It creates no additional gameplay managers or physics work. Static code/resource checks and a regression source comparison were added; **Godot 4.7.2 .NET build and UI run were not possible in this environment**. If Godot reports a C# assembly/plugin load issue, first ensure it is the .NET edition, rebuild the C# project and re-enable the plugin.
