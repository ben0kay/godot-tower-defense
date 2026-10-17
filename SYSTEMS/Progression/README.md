# Player progression — initial menu foundation

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
