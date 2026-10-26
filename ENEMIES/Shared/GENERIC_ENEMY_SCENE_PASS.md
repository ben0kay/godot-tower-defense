# Shared Generic Enemy Scene — Inherited-Type Migration (GE1–GE3)

**STATUS: IMPLEMENTED (GE1–GE3).** Native Godot inherited scenes; see `TESTS/Enemies/GenericEnemy.md` for executed checks and environment limits. Follow `NOTES/CodePreferences.md` and read `ENEMIES/README.md`, `Shared/Enemy.cs`, `Shared/EnemyCatalog.cs` and the existing enemy scenes before implementing.

## Objective

Keep a separate, easy-to-edit `.tscn` for **every enemy type**, but remove duplicated **common scene nodes** by making each type a Godot **inherited scene** of one shared `ENEMIES/Shared/GenericEnemy.tscn`.

The shared parent should contain only the ordinary, universally required children:

```text
GenericEnemy (Node2D; Shared/Enemy.cs)
├── Sprite (Sprite2D)
├── Movement (Node; EnemyMovement.cs)
└── DefenseBar (instance of FEEDBACK/DefenseBars/DefenseBar.tscn)
```

Enemy-specific scenes retain their **own root `EnemyDefinition.tres` reference, Sprite texture and individually authored overrides**. A changed common `DefenseBar` offset, visual defaults or `Movement` child setup should be inherited by every type **unless that type deliberately overrides the property**. The shared instanced `DefenseBar.tscn` still owns its internal drawing.

**Do not add an empty `Abilities` node to every ordinary enemy.** Only types with actual abilities own an `Abilities` container and the relevant component children:
- `Enemy003.tscn` → `ShieldEmitter` and its typed config.
- `Transporter.tscn` → `DeathSpawn` and its typed config.
- `Enemy001`, `Enemy002`, `Enemy004` → no ability nodes.

## Phases

### GE1 — Shared scene and migration
- [x] Create `Shared/GenericEnemy.tscn` with the common nodes/paths exactly `Sprite`, `Movement`, `DefenseBar`. Retain `Enemy.cs` and `EnemyMovement.cs`—do not move or duplicate their code.
- [x] Convert `Types/Enemy001/Enemy001.tscn`, `Enemy002`, `Enemy004`, `Enemy003` and `Transporter` to inherited scenes of `GenericEnemy.tscn`. Preserve each scene file **path**, root resource `Definition`, SVG, size/transforms, optional abilities and all stable `EnemyId` values.
- [x] Inspect inherited Godot scene overrides carefully: avoid accidentally recording a per-type offset override for `DefenseBar`/shared `Movement` on every type, as that would defeat shared edits. Use per-type bar offsets only when really needed for unusually large ships.

### GE2 — Catalog and runtime preservation
- [x] Existing `EnemyCatalog` registers the *individual inherited scenes* exactly as before. Keep `EnemyDefinition` values, `EnemyCatalog.Find`, F2 cycler, authored wave IDs and `SpawnManager` spawn lifecycle unchanged. No switch on enemy ID, no new universal ability manager or runtime scene creation per child.
- [x] Preserve optional abilities and their host/callback lifecycle. Transporter death releases must still work with wave provenance and checkpoint-route inheritance; Enemy003 must still restore shields. Normal enemies must not incur per-frame ability processing.
- [x] Confirm inherited nodes appear and remain editable in Godot Inspector. Editing common `GenericEnemy` child defaults should propagate across all five types; per-type SVG/Definition/ability overrides should stay independent.

### GE3 — Verification and documentation
- [x] Run Godot .NET compile, enemy catalog/spawn/ability tests, `TESTS/Enemies/DeathSpawn.md` checks, shield emitter tests, movement/navigation, portal/wave, Citadel arrival, DefenseBars, damage and relevant integration tests. Specifically validate the root still resolves `Movement`, `Sprite` and `DefenseBar` by their exact existing paths.
- [x] Test all five scenes instantiating through the catalog and verify **DefenseBar placement change once in the parent** affects all *non-overridden* inherited types; test Transporter/Enemy003 special children untouched.
- [x] Compare 100/500 concurrent enemies to current baseline. Inherited source files should not add material runtime processing/nodes. Record real executed results and any existing test/environment blockers.
- [x] Update `Shared/README.md`, `ENEMIES/README.md` and the relevant type READMEs only after implementation; mark GE1–GE3 done only when verified.

## Boundaries / not in this refactor

Do not redesign enemies as **one generic scene instantiated with a definition parameter**—the user explicitly wants **one scene per type** for browsing/editing. Do not alter stats/balance, sprites, footprints/radius, route scheduling, DeathSpawn quantities/timings, ShieldEmitter behavior, wave composition or combat. Preserve the light, composable existing architecture.

**Definition of done:** One shared scene controls the basic three child nodes for all enemies; each individual enemy scene still opens independently, uses its own artwork/definition, and includes only its actual optional ability components. No gameplay changes or regressions.
