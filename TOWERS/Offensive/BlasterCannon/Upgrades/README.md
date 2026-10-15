# Level data moved to C#

Edit [`../BlasterCannonLevels.cs`](../BlasterCannonLevels.cs). It contains one complete typed row per level, including its construction/upgrade credit price. The old level-specific .tres assets and cumulative modifiers have been removed. Add another consecutive row to extend the maximum level; no controller changes are needed.

The existing shared wallet still handles ResourceCost recipes and atomic deductions. See the parent README and TOWERS/Shared/Upgrades/README.md for current behavior.
