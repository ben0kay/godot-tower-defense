# Mission catalog

`DefaultMissionCatalog.tres` is the explicit campaign roster. Add/remove/reorder resources here; the menu sorts enabled missions by Number. No menu code or PlayableLevelCatalog switch edits are needed. Duplicate enabled numbers or incomplete enabled maps reject explicitly. Disabled drafts do not appear or become Next targets. Numbers 1–999 produce stable Level001-style progression/result identities: do not renumber released missions casually.

Each `LevelXXXMission.tres` references its map file path (loaded only on deployment) and contains Number, Title, Description and Enabled. Starting Credits, Citadel health, portal references and XP remain in the map's existing LevelConfig: there is no second copy of gameplay settings here. Waves are referenced directly from that config via WaveSource.

LegacyLauncher is optional compatibility data for the three existing F6 launchers. Leave it **empty for new missions**. Their own saved .tres path is a valid deployment/retry target; MenuNavigation builds the shared battlefield with the selected mission, then uses Godot's scene transition lifecycle. No global pending-selection state or filesystem scanning is involved.

See `LEVELS/AUTHORING.md` for creating a level. This catalog is not campaign unlock/save progression and does not invent prerequisites or completion records.
