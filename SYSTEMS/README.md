# SYSTEMS — Cross-scene application services

## Research-first extension — 2026-10-17

Progression now includes a v2 purchased-research-ID record and the first Campaign-only Research purchase/effect loop. Existing profile XP/RP migrate intact; no campaign/battlefield serializer was added. See Progression/Research/README.md. Mission scoring/replay/idempotent awards remain future work despite older planned/completed wording below.

---

This folder holds systems shared between Boot, Main Menu, campaigns and gameplay, not individual tower/enemy/world mechanics.

- `Profiles/`: **implemented** persistent local player identity, selection and backup recovery.
- `Saving/`: **S0/S1 foundation implemented**: audited checkpoint plan, typed campaign metadata and safe profile-owned storage. No gameplay callers, battlefield snapshots or Save/Load/Continue yet; S2/S3 remain pending.

The Boot screen selects the identity. Game scenes remain independent of menu lifetime. Future campaigns belong to stable profile IDs, never profile display names. Keep save I/O and persistence away from `UI/`, and do not move `ECONOMY/`, `LEVELS/`, `TOWERS/` or `WORLD/` gameplay rules into this folder.

Reference design: `ben0kay/isometric-new-2d/SYSTEMS/Profiles/ProfileStore.cs` and `SYSTEMS/Saving/`. Port only the patterns appropriate to a finite wave-based tower defense, not that game's world generation, entities or player survival serializer.

- `Progression/`: **initial foundation implemented** profile-wide player XP, levels and Research Points; mission rewards and Research Room remain planned in P1–P5; see [implementation brief](Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md). This is distinct from profile identity and future campaign/battlefield saves.

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
