# Purchased levels and atomic upgrades

`TowerLevel` is a plain C# runtime row: target Level, display name, independent WorkSeconds, complete TowerStatBlock and ordinary Array<ResourceCost> recipe. Every real tower's `*Levels.cs` table supplies Level 1 construction and each subsequent upgrade price alongside all actual combat values. There are no level-specific .tres assets, stat-operation resources or cumulative purchased bonuses.

TowerBase starts at UpgradeLevel 1. NextUpgrade selects Levels[UpgradeLevel] because the list includes construction row 1. The last row is the maximum level; an empty passive fixture or single construction row has no upgrades. Adding the next consecutive row needs no controller change.

TowerUpgradeController validates consecutive rows, resolves the next snapshot without altering the tower, and calls the existing EconomyManager.TryPurchase. Its guarded callback silently commits the expected next level/snapshot; payment completes before StatsChanged is published. Failed/stale/disabled/unaffordable/max-level requests cannot spend. HUD same-frame repeat guards remain intact. Multi-resource recipes retain atomic all-or-nothing deductions.

CreateInstanceCopy clones row objects and every mutable ResourceCost, preserving shared ResourceDefinition identities. Purchasing never mutates another tower, static authored rows, target lock, aim, cooldown or navigation. SetLevelStats replaces one absolute runtime row and invalidates caches; direct recipe edits use the existing explicit refresh path.

The selected Inspector displays current effective stats and next-row differences/cost from the same resolver as application. Price/identity/target policy stay outside combat math. Earned ranks are active at Stage03 after complete level selection; permanent research, map conditions and temporary effects remain neutral.

TowerUpgradeChecks drives real HUD purchases, expected-level/repeat guards, insufficient/secondary-resource cases, maximum level, preview parity, row independence, copies, layout and unchanged navigation. Weapon-specific integration suites retain beam/penetration/AoE coverage.

Paid upgrade resolution now retains the placed tower's selected named Rank. CommitUpgrade changes only UpgradeLevel/FinalStats and never resets TowerRankProgress. Credits and XP remain separate currencies/state; rank promotion is automatic and cannot trigger a purchase. TowerRankChecks verifies150/250-Credit Battle purchases preserve XP/rank and match ranked previews.

## Planned timed upgrades — NOT IMPLEMENTED

[Construction C1/C2](../Construction/README.md) now supplies explicit `WorkSeconds` per level and timed initial placement. Upgrades are blocked while constructing; **current upgrades still commit immediately after construction**. C3 will add deferred paid upgrade application and Inspector progress. During a timed upgrade, the proposed first-pass rule keeps the old tier operational and applies the next absolute row only when work completes; preserve existing wallet, rank XP and multi-resource safeguards.
