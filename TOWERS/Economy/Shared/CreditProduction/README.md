# Shared Credit-producing towers

CreditProducerLevel is an Inspector Resource for one absolute paid tier. CreditProducerConfig adapts those resources to ordinary TowerLevel purchases and optional immutable CreditProductionStats snapshots. PrepareLevels runs on preflight/placement; config copies own private tier resources. Towers do not acquire weapon, ammo or earned ranks through this capability.

CreditProducerTower uses the existing CreditBuffer/CreditProductionTimer and scene wallet/session/waves. Collection is an explicit Inspector action, stock survives increasing-capacity upgrades and disabled states; upgrades reset fractional periodic progress. Mission outcomes/pausing/unfinished construction/busy upgrades block work/collection as appropriate. Removal loses uncollected stock along with the structure and releases subscriptions. ARG adds one event listener; no timer unless a periodic yield is authored. All producers enforce same-world source identity through their existing hierarchy.

To author more producers, derive from this base, supply compatible config, scene/SVG/Definition and register it normally. Source payouts are separate from ordinary kill rewards; never grant from UI or estimate a killing tower by proximity. Full current rules are in ECONOMY/Production/README.md.
