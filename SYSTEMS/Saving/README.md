# Saving — Campaign persistence contract (not yet implemented)

**Current state:** Only local profile index persistence is implemented in `SYSTEMS/Profiles/`. There is no campaign serializer, campaign slots, Continue/Load action or in-game Save button yet. Playing Level001 from Campaign currently starts a **fresh session**.

## Recommended next phase

Implement `CampaignData`, `CampaignStore`, `CampaignSession`, and typed `LevelSaveData` / `TowerSaveData` under this folder. Follow the mature safety pattern in the isometric 2D game: versioned JSON, GUIDs, validated profile ownership and file sizes, temporary write + disk flush, atomic replacement, valid backup recovery, and explicit errors without destroying corrupted data. Support multiple campaign IDs under `user://Profiles/{profileId}/Campaigns/` later, with a per-profile index or lookup and a consistent Continue decision.

The profile is identity and potentially profile-wide research/preferences. The **campaign** owns level unlocks, progression and any campaign-bound permanent research. The **active level session** owns Credits, Citadel *current* HP, placed towers, per-instance purchased upgrade tier/rank, and the completed/next wave checkpoint. Base health, source tower config, map art, wave definitions and base enemy definitions remain in `res://` assets and must not be copied into saves. Store stable IDs and progression inputs, **never cached `TowerFinalStats`**; reconstruct and resolve from definitions on loading.

**Start with saves only between waves / at safe checkpoints.** A mid-wave snapshot would need spawn timing, remaining composition, active enemies and positions, projectiles and transient effects; don't silently call a partial snapshot an exact save. Do not overwrite the authored Level001.tscn, the `.tres` level config or the global default resources when restoring.

The campaign UI's Level001 gateway will later be replaced with `Levels` selection, Continue and Load Campaign screens; the current `UI/Menus/Campaign/` is deliberately simple.

Future tests: multiple profiles and campaigns stay isolated, round-trip credits/health/tower positions and tiers, wave checkpoint parity, corruption fallback, invalid file/ID handling, clean new campaign, no temp residue, and persistence across application restarts.
