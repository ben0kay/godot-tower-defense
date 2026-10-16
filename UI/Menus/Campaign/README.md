# Campaign — Two standalone playable levels

`CampaignMenu.tscn` / `CampaignMenu.cs` display **Level 001 — Defend the Citadel**, **Level 002 — The Five Gates**, buttons to launch either level, and **Back to Main Menu**. It requires a selected local profile. Level001 opens the original `res://Main.tscn`; Level002 opens `res://MainLevel002.tscn` (which shares combat/UI services via a small World variant). These are standalone fresh levels; profile saves and unlock gating are not implemented.

**No campaign save is created or restored in this pass.** This button starts a fresh Level001 session. Do not enable misleading Continue, Load, stage completion or level unlock buttons before `SYSTEMS/Saving` and progression actually support them.

Later replace this single-level gateway with **Campaign → Levels screen** where the player selects unlocked levels. Keep the interface scene-independent and use `MenuNavigation` for transitions; `LEVELS/` owns map definitions/identities and `SYSTEMS/Saving` owns progression state.

## Planned Campaign Hub + Research Room (NOT IMPLEMENTED)

See [Player XP & Research Foundation P1–P5](../../../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md). Target: show **Profile Name / Player Level / next-level XP progress / RP**, provide **LEVELS** (existing Level001/Level002 selection) and **RESEARCH** entries. Research will be an expandable center-origin hex tech tree with one real node: **Citadel Master I** (1 RP, +10% Citadel maximum health). **Existing direct level buttons are current behavior until Codex implements this.**
