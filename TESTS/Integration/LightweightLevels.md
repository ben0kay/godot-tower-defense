# Lightweight levels — 2026-10-19

Scope: one shared battlefield/world composition, data-driven campaign roster, resource-only deployment/retry, direct C# recipe references, preserved legacy F6 shortcuts. Authored map scenes/tiles/object coordinates and Create() wave batches are unchanged. Existing stable mission numbers and gameplay settings remain intact.

Validation:
- .NET8 build: 0 warnings/errors.
- `MissionCatalogChecks.tscn`: existing 001/002/020 deployments, correct map before Grid Ready, direct wave recipes, private two-Citadel fixture/source assignments, add a resource-only003 with no launcher, immediate Next registration, disabled/duplicate entries, actual resource-only deployment and defeat/retry.
- `Level002TutorialChecks.tscn`: unchanged four manual waves (12/16/20/24), portal routing and real victory.
- `ResultsChecks.tscn`: outcomes, result UI/reveal, actual legacy Retry/Next/Menu, missing target safety and pause/input cleanup.
- `CreditTowerChecks.tscn`: real placement, construction, income, Inspector collection, automatic ARG gains, upgrades and final owned kill.

The broader existing `MultiCitadelChecks.tscn` stops at “Single-objective placement adds no comparisons”: its initial authored-map fixture cannot find a usable placement cell. Reproduced the identical failure on unchanged parent b14c89b in a separate worktree; this is not claimed passing or fixed. The new focused runner separately verifies off-tree multi-Citadel map preparation and independent source assignment without altering user terrain to satisfy that assertion.

Godot editor import generated the new script UID sidecars. The container editor's MSBuild discovery reports missing SDK/assembly despite successful explicit CLI build and runtime checks; this environment-only integration issue is not a project fix. Desktop visual playtesting is still needed for the scrollable Campaign list and direct F6 shortcuts. No FPS or full-suite claims.
