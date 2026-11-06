## CTX1 checkpoint boundary

**Source Implemented / Awaiting Owner Validation.** Corruptor is a normal registered tower: identity, paid tier/rank, actual Credits invested, hull, work, ammo, heat, power and cooldown reuse existing capture/restore. No new JSON fields or version. Live enemies/projectiles already prohibit saving; Corruption runtime is cleared with those enemies, so no active effect/source serialization is added. Load re-resolves capabilities from the authored config without replaying shots/ticks/refunds. Existing fingerprints and strict compatibility remain unchanged.

---

## SPAWN1 — between-wave portal availability

**Source Implemented / Awaiting Owner Validation.** No new save fields or envelope version. WaveManager restores portal state from the actual Numbers of the first `CompletedWaves` definitions. START portals stay active; only already-completed activation waves are reapplied. The next wave's new portal stays hidden until Start. Restore does not replay spawns, pulse/burst feedback or materialization. Existing dependency fingerprints, strict validation and combat-save restrictions remain authoritative; changed authored scene/shader dependencies can still reject older checkpoints.

---

## TS1 — Health-based tower selling

**Source Implemented / Awaiting Owner Validation.**

TowerSaveData.CreditsInvested persists nullable actual paid Credits. The only new optional input is this field: missing/null legacy history stays unknown and that tower cannot be sold, even after subsequent upgrades. Never infer past spending from current prices. Negative history rejects validation. Envelope v2, content fingerprints, strict other fields and combat-save restrictions remain unchanged. Loading restores history without purchase/refund replay. Sold towers leave the tree immediately and cannot enter subsequent checkpoints.

---

# Saving — between-wave checkpoints (S0–S3)

S2/S3 source implemented 2026-10-26 on the shared battlefield used by all catalog missions.
**Build/runtime acceptance is pending:** this environment has no .NET/Godot executable.
No extensive tests, live render checks or profiling were run.

## Player flow

Select a profile, deploy, and use **Esc → Save** before the first wave or between completed waves.
The disabled Save button displays **Unavailable during combat** while authored waves, live ships,
death releases or damaging projectiles remain. Pause **Load** and Campaign **Continue** restore
that profile's single current checkpoint. Load is allowed during combat; Save is not.
Profileless F6 remains playable, with Save disabled. Options remains a placeholder.

Construction/upgrades, queued/outbound/beaming/returning builders, ammunition in transit and
production clocks do not block saving. Pending F2 debug batches are cancelled on Save; live F2
ships count as combat. Cosmetic effects/target locks are recreated, not serialized.

## Ownership and format

`CampaignData` v2 embeds typed `LevelSaveData`/`TowerSaveData`. The record stores mission identity,
completed-wave index/RNG state, wallet, every Citadel's current HP/stored income, tower identity/
cell/tier/XP/HP/targeting/work/heat/disables/cooldown, actual ammunition/production/cargo/counters,
builder state, mission telemetry, frozen deployment research and the original reward ticket.
Derived final stats/capacities are resolved from unchanged definitions. Starting Credits and
weapon initial ammunition are suppressed on resume. No purchase, placement reward, fresh mission
ticket or arrival transaction is replayed.

Files stay in `user://Profiles/{profileId}/Campaigns/current.json`, with `.bak` recovery. Version-one
metadata remains readable but cannot Continue; the first explicit real Save upgrades the same
campaign identity. New deployments explicitly overwrite the current checkpoint when Save is
pressed, retaining its campaign ID and previous valid backup. No automatic save or slot chooser.

Reads enforce required nested fields, duplicate/unknown-field rejection, finite/bounded values,
profile/reward ownership and a 1 MiB file cap. Writes retain flushed temporary-file/readback/atomic
replacement from S1. Backup recovery is read-only: damaged originals remain intact and further
writes fail until an explicit repair/archive is performed. Recovery information appears in the
Load/Continue tooltip. Neither corrupted files nor future versions silently become a new game.

## Restore and compatibility

`CampaignSession` is a dormant plain class owned by `BattlefieldLauncher`, with no processing node
or timer. Eligibility is queried at pause-open and command execution. Disk and dependency hashing
run only on explicit save/load/menu actions, never per frame.

The loader validates the selected profile, original ticket and catalog mission, then fingerprints
authored dependencies. Changed content is rejected conservatively. A new paused battlefield is
prepared beside the original; all instances, stock, work, references, reservations, scheduler order
and telemetry are rebuilt before the old scene is removed. Failure retains the original scene and
file. No scene path or runtime instance ID from JSON is used to load arbitrary content.

The mission reward ledger remains authoritative. A checkpoint whose ticket has already been
awarded/retired cannot be resumed for another XP payout. That means an older unfinished checkpoint
can become unavailable after a later mission awards XP. Continue/Load disables with that explanation.
Profile XP/RP/research ownership is never rolled back by a battlefield load.

Dependency checks currently read the authored resource files available in the editor/project;
packaged-export remapping needs explicit local verification before claiming exported-build support.
Runtime targeting locks, recoil, narration, popups and other cosmetic presentation restart normally;
shot cooldowns and gameplay heat/work/production state persist. Sustained disable sources must be
known durable tower/Citadel identities; an unknown sustained source rejects saving explicitly.

## Files and checks

- `CampaignData.cs`, `LevelSaveData.cs`: typed records; `SaveValidation.cs`: format/ownership bounds.
- `CampaignStore.cs`: atomic storage; `CampaignSession.cs`: capture, compatibility and scene preparation.
- Feature-local `*.Saving.cs` plus narrow owner methods keep restoration with existing gameplay owners.
- [Implementation plan/audit](IMPLEMENTATION_PLAN.md); [focused checks/local smoke steps](../../TESTS/Saving/README.md).

## PW4-A capacitor checkpoints — 2026-11-05

Optional `TowerSaveData.Power` stores energy, 0–100 priority, recovery latch/minimum and prepaid active remainder. Authored capacity/rates, island labels and refill allocations are not saved. Capture settles the shared power interval; restore reconstructs the owned NoPower handle before unpause, preserving unrelated disables (including external NoPower). Missing old power data restores empty; it never reapplies a configured free initial fill. The new field is optional only for reading pre-capacitor v2 data; fields within a present power snapshot remain required and validated. The existing content fingerprint can still reject earlier checkpoints; no compatibility bypass or envelope bump. Godot .NET save/load validation remains for owner.
