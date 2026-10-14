# Shared tower foundation

Phase 4A adds TowerConfig (canonical identity, construction recipe, optional tiers), TowerDefinition (stable ID, category, scene, config reference, icon and 1×1 footprint) and TowerCatalog (validated ID lookup). Name/description/prices are read from Config rather than duplicated in definition or HUD. The default catalog registers only Battle Tower. Unavailable types remain registered but cannot be built. Rebuild validates blank/duplicate IDs, costs, category, footprint, scene root, config compatibility and type-specific scene prerequisites; call Rebuild after editing registration data.

TowerBase owns type/definition, fixed grid anchor, footprint, placement/selection hooks, optional range capability, purchased UpgradeLevel, FinalStats and StatsChanged. Types supply config compatibility/application and resolution; it has no mandatory weapon nodes or processing. TowerConfig explicitly copies external tier/cost resources per instance. Passive types can have no combat snapshot, range or upgrades.

Targeting/, Weapons/TurretAiming and Projectiles/ are optional existing weapon helpers. Battle Tower's weapon loop remains local until Phase 4C has a second real offensive consumer. Stats/ preserves the eight-stage resolver; purchased upgrades are active and ranks/research/conditions/effects remain neutral. Upgrades/ uses the sole ECONOMY wallet.
