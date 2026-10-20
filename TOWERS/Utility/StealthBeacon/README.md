# Stealth Beacon — SE1

Registered as `stealth_beacon` under Ctrl → Utility. One1×1 paid tier:250 Credits,2 seconds construction,180 HP and256px reveal range. Artwork is an original muted teal/green antenna beacon. Uses existing paid placement, footprint, construction, hull/destruction, selection and range-ring flows. No attack, ammo, earned combat ranks, artificial firing loop or paid upgrades are authored.

DefaultStealthBeaconConfig.tres exposes RevealRange (1–1024px) and MaxHealth; default cost/work are in StealthBeaconConfig's single level row. Private placed configs prevent changes leaking across towers. Operational beacons reveal enemies inside their inclusive center-to-center radius for all ordinary towers. Leaving the radius restores stealth unless another source still reveals the enemy. The beacon does not grant detection to nearby towers.

Construction, upgrade work, generic disable and destruction/removal all stop revealing. Reactivation/range edits/source relocation refresh stationary enemies without waiting for a scan interval. The shared world index consumes movement events and uses spatial buckets; this tower has no Process/PhysicsProcess callback. World/Registry unload and tower removal release source references.

Existing offensive towers retain CanDetectStealth=false. Future specialists may enable the shared config flag independently; future tower-grant beacons can be a separate mechanic. This pass does not implement Radar Tower, research unlocks or beacon upgrades. Existing waves remain unchanged; author stealth:true explicitly when ready. See ENEMIES/Shared/Stealth/README.md and TESTS/Stealth/README.md.
