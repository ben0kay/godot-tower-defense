# Shared Tower Foundation (Future)

Common tower attributes belong here when implementation begins: a small base class or composition helpers, per-type `Resource` definitions, footprint/cost/upgrade data, and optional behaviors.

**Do not** force every tower to inherit weapon targeting, recoil or projectile code. A repair tower and income tower may never attack. A hybrid Solar Tower may add a weapon behavior after an upgrade.

Prefer simple, data-driven Godot **C#**, not a large inheritance hierarchy. No runtime code yet.
