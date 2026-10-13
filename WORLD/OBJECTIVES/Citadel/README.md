# The Citadel — Defended Destination

Reusable map object scene: `Citadel.tscn`.

```text
Citadel (Node2D)
└── ApproachPoints (Node2D)
    └── Approach01 (Marker2D)
```

The scene shell can be instanced and moved in Godot. The marker is an **editable placeholder for a valid enemy approach/destination**, **not** proof of a traversable tile. When adding solid Citadel art/collision, move the marker to an unblocked reachable position near its boundary; add more approach points if enemies should attack from multiple sides.

The Citadel will eventually expose durability, collision/footprint, team/ownership and defeat signals. None are programmed yet. Enemy AI should seek an appropriate valid `ApproachPoint` chosen by navigation; spawning does not belong in this folder.

No art, health, collision, combat or live-level scene instance yet.
