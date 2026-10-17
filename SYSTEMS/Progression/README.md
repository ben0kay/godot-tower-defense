# Player progression — initial menu foundation

## Battle Tower Weapons I extension — 2026-10-17

The Research Room now contains **three real nodes**, including battle_weapons_1 / Battle Tower Weapons I (cost1 RP, prerequisite research_core, axial -1,0). It permanently grants +5% Battle Tower damage in future deployments. The stored purchased ID uses existing progression v2; previous saves and Citadel research stay compatible.

TowerDamagePercent effects carry a stable TargetTowerId (battle for this node). ResearchDeployment resolves purchased IDs once at mission initialization; CitadelSession owns this immutable snapshot. Paid placement/build previews take their type's bonuses from it; TowerBase retains those values for rank/config/paid-level rebuilds. Stage04 performs arithmetic only and never loads profiles or Resources. Live missions retain their captured bonuses even if selection changes externally; a fresh mission captures current research.

Integer projectile damage rounds nearest/ties-to-even after Stage03 rank rounding: standard-rank Battle levels1/2/3 are32/35/38 rather than30/33/36. Authored source rows, costs, ammo and heat are not modified. Build Inspector and next-tier previews use the same researched values. Only Battle's node is authored; additional approved effects/nodes remain future work. Mission XP scoring/replays/once-only rewards remain deferred.

Graph reset/initial resize now fits the authored nodes and balances long names across two lines. Test/authoring details: TESTS/Research/BATTLE_RESEARCH.md. Original notes below remain for history; statements that tower research is neutral/future-only are superseded by this extension.

---

## Research-first extension — implemented 2026-10-17

Progression schema **v2** adds immutable PurchasedResearchIds. v1 records migrate in memory with their XP, RP and curve intact; saving the next successful grant/purchase writes v2 through the same atomic/backup path. Profile identity/index is unchanged. A Research purchase spends RP and adds one stable ID in one record replacement; save failures do not publish changes.

Research/ owns authored catalog validation, pure eligibility/payment and explicit effect resolution. UI/Menus/Campaign/Research owns graph/presentation only. CitadelSession resolves purchased HP bonuses once from authored max HP at deployment; direct profileless F6 keeps authored HP. No menu-time/live HP multiplication. Unknown purchased IDs are preserved for deliberate future catalog migration and do not fabricate effects.

Campaign-only purchases and the first real node are implemented; mission XP scoring, replay/first-clear history, persisted mission award tokens and Results rewards remain deferred. The DEBUG grant is still off by default and is the current way to obtain test RP. The initial v1 description below is retained as history; use this amendment for current behavior.

---

The selected profile GUID owns Player XP, player levels and Research Points independently of tower Rank XP, Credits and campaign/battlefield saves.

- PlayerProgression: immutable source fields and derived level/current XP.
- PlayerLevelCurve: pure linear curve, bounded binary search for multiple level-ups.
- ProgressionMasterConfig + DefaultProgressionConfig.tres: new-profile thresholds (500 first, +250 each subsequent level).
- ProgressionStore: version 1 JSON at user://Profiles/{GUID}/progression.json, flushed atomic replacement and validated backup recovery. Missing records start Level 1 / 0 XP / 0 RP without rewriting profile identity. Corruption with no valid backup raises an error and preserves files.
- ProgressionService.GetCurrent / GrantXP(amount): selected-profile authority, save-before-notify, one RP per promotion, no autoload/frame processing.
- UI/Menus/PlayerProgressionPanel: shared event-driven menu view; Main and Campaign display level, XP bar and RP.

Curve values are copied into the progression record on its first saved grant. Existing earned levels cannot shift just because the default resource changes. Deliberate migrations can be designed later. Derived levels are not serialized.

## Development verification

MainMenu and CampaignMenu expose ShowProgressionDebugGrant (default false). Enable it in the Inspector in a DEBUG build to show a clearly labelled 600 XP grant button. It **saves real progression** for the selected profile; use a disposable test profile. First click: Level 2, 100 / 750 XP, 1 RP. Release exports do not include the button.

GrantXP is a low-level foundation API, not a mission reward claim API: repeated calls intentionally grant repeatedly. P2 must add frozen scoring and persisted run-token idempotency before connecting Results. No mission grants, replay history, research purchases or Citadel bonuses exist in this smaller pass.

See PLAYER_XP_RESEARCH_FOUNDATION_PASS.md for remaining P1–P5 work and TESTS/Progression/README.md for actual verification.
