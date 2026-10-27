## CD4 drone-assisted paid upgrades (2026-10-27)

All paid nonzero-duration upgrades can now request the same nearest-Citadel construction drone used for initial builds. Work stays held at zero until beam deployment; then the existing upgrade timer advances and the combat-disabled tower receives its new stats only at completion. Drone departs independently. Inspector distinguishes waiting/drone-active upgrade and only Battle pilots 35% + 70% artwork steps. Shared config option `ServiceUpgrades=true` can disable this without affecting initial construction; strict/fallback, pause and saved work/drone state remain shared. [Implementation plan](UPGRADE_DISABLE_TIER_ART_PLAN.md).

---

## DU1/DU2 — disabled paid upgrades and split purchased-level artwork (2026-10-27)

**Source implemented; editor/runtime acceptance pending.** All tower types now derive an **Upgrading** disable reason from their existing paid `TowerWorkPhase`: attacks/owner physics stop while the old tier and ammo/rank remain cached, then the new paid tier applies and operation resumes. The normal Inspector displays `UPGRADING → N` and the condition reason `DISABLED: UPGRADING`, alongside any independent heat/EMP disables. Work is still timed by the shared event-driven manager; there is no extra disable condition node, interval clock or saved disable entry. Existing logistics/occupancy are preserved and zero-duration purchases remain instantaneous.

The existing `TowerConstructionVisuals`/ `TowerLevelArtwork` now optionally map **finished base and gun textures per tier** as well as the old monolithic overlay. The Battle Tower alone ships 64px Level2/3 split sprites and a mid-upgrade composite at 35%. Rotating/recoiling weapon and existing muzzle markers are preserved. Other tower art remains unchanged. [Design/implementation plan](UPGRADE_DISABLE_TIER_ART_PLAN.md) and [Battle art](../../Offensive/BattleTower/Artwork/Upgrades/README.md).

---

# Purchased levels and atomic upgrades

`TowerLevel` is a plain C# runtime row: target Level, display name, independent WorkSeconds, complete TowerStatBlock and ordinary Array<ResourceCost> recipe. Every real tower's `*Levels.cs` table supplies Level 1 construction and each subsequent upgrade price alongside all actual combat values. There are no level-specific .tres assets, stat-operation resources or cumulative purchased bonuses.

TowerBase starts at UpgradeLevel 1. NextUpgrade selects Levels[UpgradeLevel] because the list includes construction row 1. The last row is the maximum level; an empty passive fixture or single construction row has no upgrades. Adding the next consecutive row needs no controller change.

TowerUpgradeController validates consecutive rows, resolves the next snapshot without altering the tower, and calls the existing EconomyManager.TryPurchase. Its guarded callback schedules the expected next tier; payment completes before WorkStarted is published. Old level/cached stats remain active until work ends. Completion resolves the target tier with the current rank, synchronizes ammunition without refill and publishes StatsChanged once. Zero-duration rows commit immediately and publish only after payment. Failed/stale/disabled/unaffordable/max-level requests cannot spend. HUD same-frame repeat guards remain intact. Multi-resource recipes retain atomic all-or-nothing deductions.

CreateInstanceCopy clones row objects and every mutable ResourceCost, preserving shared ResourceDefinition identities. Purchasing never mutates another tower, static authored rows, target lock, aim, cooldown or navigation. SetLevelStats replaces one absolute runtime row and invalidates caches; direct recipe edits use the existing explicit refresh path.

The selected Inspector displays current effective stats and next-row differences/cost from the same resolver as application. Price/identity/target policy stay outside combat math. Earned ranks are active at Stage03 after complete level selection; permanent research, map conditions and temporary effects remain neutral.

TowerUpgradeChecks drives real HUD purchases, expected-level/repeat guards, insufficient/secondary-resource cases, maximum level, preview parity, row independence, copies, layout and unchanged navigation. Weapon-specific integration suites retain beam/penetration/AoE coverage.

Paid upgrade resolution now retains the placed tower's selected named Rank. CommitUpgrade changes only UpgradeLevel/FinalStats and never resets TowerRankProgress. Credits and XP remain separate currencies/state; rank promotion is automatic and cannot trigger a purchase. TowerRankChecks verifies150/250-Credit Battle purchases preserve XP/rank and match ranked previews.

## Timed upgrades — C3 implemented

ConstructionWorkManager now clocks both initial builds and paid upgrades. `WorkSeconds` comes from the one explicit target row; `PendingUpgradeLevel` identifies the paid target. IsBusy prevents duplicate/stale purchases; IsOperational stays true during upgrading. Current weapons, aiming, cooldowns and deliveries continue at the old tier. Rank promotions during work rebuild that old tier normally; completion uses the latest rank when resolving the new absolute tier. No extra occupancy/navigation, research, refunds or saving were added.

The existing Inspector shows state/progress/time, current stats and the pending-tier differences, with “Cost already paid” and a disabled upgrade button. Progress events update only work controls at approximately10Hz. C4 stage art/effects remain planned. See `TESTS/Towers/TimedUpgrades.md` for actual verification and environment limits.

## IT2 selected-tower current/next/gain display — code landed

`TowerPlacementController.RefreshUpgradePreview` resolves the same ranked current and next complete absolute tier as the authoritative paid upgrade controller. `UI/GameplayHUD/Inspector/TowerUpgradeComparison.cs` turns those immutable `TowerFinalStats` into typed comparison rows with correctly signed deltas, rather than parsing `DescribeChanges` text. Price is displayed separately; Rank XP is earned and cannot be spent. During work, the old tier remains operational and the next-tier comparison says `Cost already paid`; at maximum level no next-tier rows appear. Wallet, expected-level guards, construction timings, ammo and navigation are unchanged. Tests were updated, but not executed in this editing environment.
