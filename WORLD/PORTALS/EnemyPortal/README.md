# Enemy Portal — Scene Skeleton

`EnemyPortal.tscn` contains:

```text
EnemyPortal (Node2D)
└── SpawnExit (Marker2D)
```

It can be instantiated/positioned by a map designer in Godot. The `SpawnExit` marker is the future enemy-creation/entry location and should be on **navigable open space**, connected to the Citadel.

When Codex implements this later:
- Add replaceable portal visual art/VFX, optional collision/clearance, ID, active state and relevant Inspector exports.
- Expose spawn exit transform to `ENEMIES/Spawning`.
- Keep **wave timing, enemy type and spawn quantity out of this object**.

This marker-only placeholder isn't instantiated into `Level001` or `Main.tscn` yet and produces no ships.
