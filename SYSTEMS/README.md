# SYSTEMS — Cross-scene application services

This folder holds systems shared between Boot, Main Menu, campaigns and gameplay, not individual tower/enemy/world mechanics.

- `Profiles/`: **implemented** persistent local player identity, selection and backup recovery.
- `Saving/`: **planned** tower-defense campaign/session save system. README sets ownership and format boundaries; no campaign saves are written yet.

The Boot screen selects the identity. Game scenes remain independent of menu lifetime. Future campaigns belong to stable profile IDs, never profile display names. Keep save I/O and persistence away from `UI/`, and do not move `ECONOMY/`, `LEVELS/`, `TOWERS/` or `WORLD/` gameplay rules into this folder.

Reference design: `ben0kay/isometric-new-2d/SYSTEMS/Profiles/ProfileStore.cs` and `SYSTEMS/Saving/`. Port only the patterns appropriate to a finite wave-based tower defense, not that game's world generation, entities or player survival serializer.
