## ARG automatic income correction — 2026-10-19

ARG eligible kills now grant Credits directly to the existing wallet: no collection button, stored income or storage limit. The Resource Tower and Citadels retain manual collection. A single mouse-transparent green floating number beside each ARG combines rapid successful payouts (+1 → +2 → +5), then floats/fades away after a quiet interval (default 0.8s). Popup duration/color are editable on ARG config. Higher tiers still earn 2/3 per eligible kill. Full-wallet rejection displays no false income and is not banked. Existing kill eligibility, pause/construction/upgrade gates and ordinary kill rewards/XP remain intact. ARG previews omit unused storage. Earlier buffered-ARG descriptions below are historical and superseded by this correction.

---

# Shared Credit-producing towers

CreditProducerLevel is an Inspector Resource for one absolute paid tier. CreditProducerConfig adapts those resources to ordinary TowerLevel purchases and optional immutable CreditProductionStats snapshots. PrepareLevels runs on preflight/placement; config copies own private tier resources. Towers do not acquire weapon, ammo or earned ranks through this capability.

CreditProducerTower uses the existing CreditBuffer/CreditProductionTimer and scene wallet/session/waves. Collection is an explicit Inspector action, stock survives increasing-capacity upgrades and disabled states; upgrades reset fractional periodic progress. Mission outcomes/pausing/unfinished construction/busy upgrades block work/collection as appropriate. Removal loses uncollected stock along with the structure and releases subscriptions. ARG adds one event listener; no timer unless a periodic yield is authored. All producers enforce same-world source identity through their existing hierarchy.

To author more producers, derive from this base, supply compatible config, scene/SVG/Definition and register it normally. Source payouts are separate from ordinary kill rewards; never grant from UI or estimate a killing tower by proximity. Full current rules are in ECONOMY/Production/README.md.
