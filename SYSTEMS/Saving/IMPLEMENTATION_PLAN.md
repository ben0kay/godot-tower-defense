# Between-wave saving — S0 audit and staged implementation

S0 completed 2026-10-26 against source `120f9e8`. S1 is storage only. S2/S3 remain pending.
User preference: brief initial checks; extended runtime/visual/performance testing stays local.

## Player contract

Esc → Save saves the selected profile's current battlefield. Save is disabled during combat,
with the exact explanation **Unavailable during combat**. Construction, upgrading and drones
between waves must be preserved; they are not additional reasons to prohibit saving.
No mid-combat enemy/projectile serializer, autosave or multiple-slot UI is part of this first feature.
Direct F6/profileless play must remain usable without creating a phantom profile or save.

## Significant S0 findings (communicated before S1)

- Ammo cargo is real inventory removed from its source at launch (`AmmoDeliveryJob`, logistics README).
  Restoring building stock alone loses it. Persist source/destination save IDs, cargo, reservation,
  outbound/return state, position and speed. Rebuild incoming reservations and occupied home slots once.
  Empty returning drones still occupy slots. Never call arrival twice, mint cargo or silently refund it.
- Research is frozen at deployment (`ResearchDeployment`); reopening Campaign can buy new research.
  Persist captured research inputs and resolve through the existing pipeline on resume, rather than
  recapturing today's profile research. Profile XP/RP/ownership remain in Progression, not campaign JSON.
- `LevelSessionController` reserves a monotonic mission ticket at startup. Resume must restore the
  original ticket/policy instead of reserving another. The reward ledger can retire an older ticket
  after a later mission awards XP: reject that stale checkpoint with a clear error. Do not reset the
  ledger or replay XP. Profile ledger and checkpoint are separate files; stale-save validation is mandatory.
- Builder assignment/hold is separate from `TowerWorkState`. Persist work elapsed/total/pending tier,
  held state, queued order and builder phase/home/position/deployment progress. Restore relationships
  before enabling clocks/dispatch. Recent builder visual changes do not change this ownership.
- No complete capture/restore APIs exist yet for these systems. S1 cannot truthfully enable Save/Load.

These require S2 integration, not a different S1 storage algorithm or a new gameplay restriction.

## Ownership and capture inventory for S2

| Source owner | Durable inputs to capture / restoration requirement |
| --- | --- |
| MissionDefinition / mission catalog | Stable LevelId; resolve allowlisted authored mission. Never load arbitrary scene paths from JSON. Validate map/content compatibility before replacing current scene. |
| WaveManager | Completed-wave index, next authored wave and random generator State (not just initial seed). Restore Ready/Completed without starting a wave or replaying completion events. |
| LevelSessionController / LevelRunStats | Original profile/ticket/policy, official kills/breaches, placements/completions/upgrades and unpaused mission seconds. Restore without synthetic reward/stat events. |
| EconomyManager | Wallet balances; do not repeat starting-credit grants or purchase costs. |
| CitadelSession / CitadelDestination | Every relative authored objective ID, current HP, stored Credits and production-clock progress. Scene path identities need compatibility validation after map edits. |
| TowerBase / rank / targeting | Assign durable per-instance save GUIDs (Godot instance IDs are not persistent), catalog type ID, footprint anchor/orientation, paid tier, total XP, current HP, targeting priority and captured research inputs. Never serialize TowerFinalStats or mutate shared `.tres` resources. |
| ConstructionWorkManager / builder manager | Work phase/clock/pending tier, holds, queued and active assignments, remaining deployment and returning flights. Rebuild event bindings once. |
| TowerConditions | Current heat, overheat recovery and disable reason/duration/source relationships. Runtime handles/ulong source IDs need durable remapping, never direct reuse. |
| CreditBuffer / CreditProductionTimer | Stored balance and fractional production progress; ARG pays immediately and has no stored income. |
| AmmoInventory / AmmoProduction / factory | Consumer reserve, depot/factory stock, production elapsed and existing accounting counters; reconstruct capacity from definitions without initial refill. |
| AmmunitionLogistics / TransportDrone | Cargo, endpoint references, occupied slots, destination reservation, outbound/return leg and position. Rebuild demands after jobs, not before. |
| Camera / presentation | Camera pose/zoom may be saved as convenience; rebuild feedback, inspector and caches. Cosmetic streaks/popups/flashes need no persistence. |

A safe capture is on the main thread while paused, after synchronous transactions settle.
Eligibility requires a Playing session and WaveManager Ready/Completed; not AllWavesComplete,
Disabled, PausedError, Spawning, SpawnBlocked or WaitingForClear. Check actual combat too:
F2 ships are deliberately unowned by WaveManager, pending F2 deliveries can spawn later, and hostile
bullets may outlive a wave. S2 must guard live ships, damaging projectiles and pending spawn/death
releases, and cancel/retire transient debug jobs explicitly. Do not let an empty wave-owned count
silently authorize a partial combat snapshot. Refresh the UI from lifecycle events, not idle polling.

Restore into a prepared fresh battlefield: validate all data/catalog references first; suppress
initial grants, payment, initial ammo refill, stats and reward issuance; instantiate all durable
objects; resolve cross-references/reservations; restore clocks/counters; rebuild occupancy/navigation;
then bind UI and unpause. On failure keep the original save and report the problem. Add typed
LevelSaveData/TowerSaveData only with their actual capture/restore seams, rather than guess an
incomplete battlefield format in S1.

## Passes

| Pass | Scope | Status |
| --- | --- | --- |
| S0 | Audit ownership, safe boundary and integration requirements | Complete; this document |
| S1 | Typed versioned campaign metadata and safe per-profile storage | Implemented; no callers/gameplay writes |
| S2 | Complete typed battlefield schema, main-thread capture/restore and original mission ticket integration | Pending |
| S3 | Esc Save/Load, Continue, combat explanation and profile-owned navigation | Pending |

## S1 storage contract

`CampaignData` v1 deliberately contains only profile/campaign GUIDs and UTC creation/save times.
This is metadata, **not a resumable checkpoint**. S2 must bump the version and explicitly migrate
or identify metadata-only records as having no checkpoint. Do not enable Continue for a v1 file.
No campaign unlock rules, separate research balances or speculative entity framework are introduced.

`CampaignStore` takes an explicit Profiles root for isolated checks. One fixed slot lives at
`user://Profiles/{profileId}/Campaigns/current.json`; the campaign GUID stays inside it.
No index or directory scan is needed to select Continue. Multiple slots and a deliberate
New Campaign overwrite/archive workflow are later features. Saving another campaign into an
occupied slot is rejected rather than silently deleting it.

Load returns null only when both primary and backup are absent. It rejects wrong ownership,
unknown version/members, missing/duplicate fields, invalid IDs/timestamps and files over 1 MiB.
A valid backup can be loaded read-only with a recovery message; neither damaged file is erased.
Save validates the supplied owner, current primary and existing backup; refuses damaged/different
campaign data or backwards timestamps; writes a unique temporary file, flushes, reads it back,
then atomically replaces the primary and retains the previous primary as backup. Temporary
files created by the operation are cleaned in finally. An OS/process crash may leave an orphan
unique temp: readers ignore it; future explicit maintenance may remove it, never promote it.
After corruption recovery, Save deliberately refuses to overwrite the damaged original until
an explicit repair/archive flow exists. Do not label recovery as a successful new write.

Store calls serialize within an instance. As with existing progression storage, independent
concurrent game processes editing the same profile are unsupported. The caller must use an
existing selected profile's ID; disk ownership validation does not itself authenticate users
or create/check the global profile index. No Node, process callback, autoload or timer is added.
