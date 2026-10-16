# Generic rectangular tower placement — F1–F5 implemented

Main/TowerPlacement exports Settings and Catalog. Registered definitions supply scene/art/config and positive `Footprint` dimensions. The clicked cell is the top-left anchor; X is width and Y is height. Factory is **3×2**, Depot **2×2**, and current offensive towers **1×1**. Fixed rectangles support future sizes including 2×3; rotation/irregular shapes and upgrade-time resizing are out of scope. See [the completed pass](MULTI_CELL_FOOTPRINT_PASS.md) and [verification](../../TESTS/Towers/Footprints.md).

## Validation, claims and navigation

Every covered tile must be in bounds, buildable, unreserved and free. Multi-cell buildings require one homogeneous underlying terrain category, regardless of visual artwork: all PASSABLE or all IMPASSABLE. Mixed foundations reject before navigation/payment with a structural failure message and red preview.

WorldGrid validates the whole rectangle before claiming any cell. Exact anchor/size claims make occupancy/release atomic and prevent partial releases touching neighbouring structures. All-IMPASSABLE buildings gain occupancy/selection without navigation revisions, events, A* edits, floods or repaths. All-PASSABLE proposals run radius-aware active enemy checks and one combined reusable flood before purchase, preserving usable spawn sources and live enemy connectivity. Accepted changes publish one revision/rectangle batch after complete occupancy; shared A* profiles update incrementally and indexed route validation remains budgeted/coalesced. Existing single-cell wrappers/events preserve 1×1 callers.

## Construction and interaction

EconomyManager.TryPurchase remains authoritative: one construction recipe per building. Config/scene preflight precedes occupancy; scene/index/occupancy rollback never charges, leaves an orphan or releases neighbours. Tree exit removes all of the instance's indexed cells and exact grid claim. Every occupied tile selects the same TowerBase in constant time. The single root and ghost use the full rectangle center; preview draws its full 3×2/2×2 region. Build/placed Inspectors show actual dimensions. No required combat nodes are added to passive buildings.

Preview checks only terrain/occupancy/affordability; full navigation validation is click-time. Wallet/config events refresh price state. Stats/levels/ranks and selected-only coalesced runtime labels retain existing cached behavior. Ctrl closes tray/cancels placement; RMB/Esc cancels ghost while leaving tray open. Placement Escape precedes Pause; results retain final priority.

## Startup and economy

A null Catalog export loads `res://TOWERS/DefaultTowerCatalog.tres` with a warning; explicit catalogs remain unchanged, and a missing default reports its exact resource path. Factory/Depot reuse normal catalog, wallet and Inspector infrastructure. Factory production remains 100 rounds/2s with **1,000-round shipments**; Depot retains **10,000 storage**. Centered roots remain drone departure/return positions; transport/reservations/consumption and upgrade prices are unchanged. Selling UI and saving remain deferred.
