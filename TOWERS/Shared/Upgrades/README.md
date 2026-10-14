# Purchased tower upgrades — Phase 3B

TowerUpgradeTier is shared Inspector data: consecutive target Level (2, 3, ...), DisplayName, Array<ResourceCost> Costs and Array<TowerUpgradeModifier> Modifiers. TowerUpgradeModifier selects a typed stat, operation and Amount. Flat adds a value; AdditivePercent uses fractions (.2 = +20%); Multiplier uses direct factors (1.2 = ×1.2).

TowerBase owns per-instance UpgradeLevel, final snapshot and change event, without any targeting/firing loop. BattleTower supplies its Config/tiers to the shared resolver. Empty tier lists mean no available upgrades; the same helpers can support future typed support/production stats without adding weapon behavior.

Stage 02_TowerUpgrades resolves every purchased tier's operations together: (base + sum(flat)) × (1 + sum(percent)) × product(multiplier), per stat. Damage rounds to nearest integer, ties to even, and clamps to the nonnegative integer range. Other safety clamps apply at Stage 07. It never modifies Config or charges money. Later rank/research/level/effect stages remain neutral.

TowerUpgradeController validates consecutive levels and finite typed operations, resolves the preview, then uses the existing EconomyManager.TryPurchase. The callback silently commits the precomputed level/snapshot; payment is then deducted and StatsChanged published. Invalid/stale/max-tier/disabled/unaffordable requests never charge. Expected current levels reject duplicate tier requests; the HUD controller also ignores repeated action signals within the same frame.

TowerConfig.CreateInstanceCopy explicitly copies external tier assets and nested costs/modifiers; BattleTowerConfig retains its typed override. Godot Duplicate(true) alone retains external .tres references inside these arrays. Stable resource identities remain shared; mutable amounts and modifiers do not. Base values stay unchanged. Existing aim, target and shot cooldown remain intact; upgrades do not touch occupancy/navigation.

Preview/apply use the same pipeline and current source inputs. The Inspector shows actual final-stat changes, price and insufficient-resource/max-level status; wallet/stat events refresh it without polling. Close/Ctrl behavior remains independent. Resource editing in the saved tier .tres is permanent; runtime edits to nested tier data are per-instance and require RebuildStats() to refresh the snapshot.

TESTS/TowerUpgradeChecks exercises production HUD purchases, preview parity, level progression, multi-resource atomic payments, missing funds, disabled rules, stale/same-frame requests, max tier, external resource isolation, arithmetic, repeated rebuilds, layout bounds and unchanged navigation.

Phase 4A/4B placement/Inspector use the selected TowerBase's private config, tiers and resolver rather than the build choice's Battle defaults. No-upgrade passive fixtures show a safe disabled state with no combat snapshot. Per-type configs describe real preview differences; payment/math remain unchanged.
