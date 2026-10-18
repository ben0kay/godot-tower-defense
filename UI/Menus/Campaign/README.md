## P3 Campaign hub / Levels split — implemented 2026-10-17

- Campaign now shows the selected profile's saved player level, XP and Research Points, followed by distinct **LEVELS** and **RESEARCH** destinations.
- **LEVELS** opens UI/Menus/Campaign/Levels/LevelSelectionMenu.cs/.tscn with the existing Level001 and Level002 mission descriptions/launch buttons. Back returns to Campaign. Research continues to return to Campaign; Results already returns to this hub after mission rewards.
- Existing MenuLayout, UIButtonFactory, profile guards and MenuNavigation are reused. No Research access was added to Results/gameplay. Both missions remain available; mission unlocking, completion stars, campaign saves/slots and Continue/Load remain separate future work.
- This completes the remaining **P3 menu organization slice**, alongside the previously implemented progression display. It does not complete all P1–P5/R2/R4 scoring or polish.
- Verification: SDK build succeeded with zero warnings/errors. MenuTowerFlowChecks, ResearchMenuChecks and ResultsChecks passed; viewport checks passed at 1920×1080, 1366×768 and 960×540. The broader controls runner hit a native Godot finalizer/resource crash; no full-suite pass is claimed. Details are recorded in TESTS/UI/CampaignHub.md. Desktop visual review remains for the user.

Older menu descriptions below are retained as history and superseded by this amendment.

---

## Mission XP foundation — 2026-10-17

Results now returns directly to Campaign after committing profile XP. The existing profile panel reads the updated XP/level/RP; Research remains accessible only here between deployments. A separate Levels area and mission unlocking are still deferred.

---

# Campaign — Two standalone playable levels

## Research-first extension — 2026-10-17

Campaign now offers RESEARCH, opening Research/ResearchMenu.tscn between deployments. Existing Play Level001/002 actions remain unchanged; no fake unlock/Continue/Load actions. Research graph/purchases/Citadel Master I are implemented; profile XP scoring and the separate LEVELS-area hub split remain future work. See Research/README.md.

---

`CampaignMenu.tscn` / `CampaignMenu.cs` display **Level 001 — Defend the Citadel**, **Level 002 — The Five Gates**, buttons to launch either level, and **Back to Main Menu**. It requires a selected local profile. Level001 opens the original `res://Main.tscn`; Level002 opens `res://MainLevel002.tscn` (which shares combat/UI services via a small World variant). These are standalone fresh levels; profile saves and unlock gating are not implemented.

**No campaign save is created or restored in this pass.** This button starts a fresh Level001 session. Do not enable misleading Continue, Load, stage completion or level unlock buttons before `SYSTEMS/Saving` and progression actually support them.

Later replace this single-level gateway with **Campaign → Levels screen** where the player selects unlocked levels. Keep the interface scene-independent and use `MenuNavigation` for transitions; `LEVELS/` owns map definitions/identities and `SYSTEMS/Saving` owns progression state.

## Planned Campaign Hub + Research Room (NOT IMPLEMENTED)

See [Player XP & Research Foundation P1–P5](../../../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md). Target: show **Profile Name / Player Level / next-level XP progress / RP**, provide **LEVELS** (existing Level001/Level002 selection) and **RESEARCH** entries. Research will be an expandable center-origin hex tech tree with one real node: **Citadel Master I** (1 RP, +10% Citadel maximum health). **Existing direct level buttons are current behavior until Codex implements this.**

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
