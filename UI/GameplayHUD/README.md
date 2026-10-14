# Gameplay HUD — status, catalog build tray and tower Inspector

The CanvasLayer HUD displays gameplay state. ECONOMY owns balances, CitadelSession owns objective HP, WaveManager owns scheduling, TowerCatalog owns registered build choices and TowerPlacementController owns placement/upgrade decisions.

## Current implementation

TopBar remains permanent: Credits left, Citadel current/max HP and compact health fill at the true viewport center, Wave right. Start/Next Wave sits below the right zone; SetWaveControls updates button/tooltip from real wave state. Health colors/clamping and the compatibility SetCoreHealth forwarding API remain intact. UIMasterConfig controls layout and appearance.

Ctrl independently opens/closes TowerBar with the existing slide/fade. Its horizontal ScrollContainer supports arbitrary registered type counts without widening the HUD. `BindCatalog` reads available definitions and generates `UIButtonFactory` TowerTile buttons with canonical config name, category, construction recipe, icon and tooltip; it never instantiates tower scenes or owns stat math. DefaultTowerCatalog registers Battle and Laser towers. Unavailable entries are omitted; an empty catalog shows a safe message. No nonfunctional Laser/Missile/Repair/Solar/Resource samples remain.

Placement validates/rebuilds the exported Catalog at startup, then calls BindCatalog after the HUD is ready. Explicitly rebuild/rebind after runtime registration/availability changes; there is no frame-based registry/menu polling. GetBuildButton(typeId) accesses a generated action independently of scene-node names. TowerChoiceRequested carries that stable ID. Buttons retain keyboard focus and the shared factory's visual states; horizontal scrolling makes distant entries reachable.

The independent bottom-right Inspector shows the actual selected instance's identity, private config, level and capability-specific FinalStats. Upgrade details come from its real next-tier resolver and shared recipe display. TowerUpgradeRequested is an action request; the controller checks the expected level, same-frame duplicates, global AllowUpgrades and wallet transaction. No tiers means a safe No upgrades available state. Closed Inspector stays closed on stat/balance refresh; Ctrl tray state does not control Inspector visibility. The Inspector can grow upward while the tray retains BottomBarHeight.

The HUD root ignores unused mouse space; visible GUI controls consume input. Selection/range rendering stays in placement and draws only when the selected tower exposes InspectionRange.

## Files and verification

GameplayHUD.cs supplies status/action APIs and dynamic entries; TowerBar/TowerBar.tscn supplies header/scroll/entries container; Inspector/InspectorPanel.tscn supplies close/stats/upgrade controls; UI/Shared/UIButtonFactory supplies generated and authored button styles. The old six static button nodes and SampleTowers list have been removed.

TowerBuildBarChecks covers 40 definitions, unavailable types, prices/icons, action IDs, focus, scrolling bounds, rebinding/empty state and independent panels. TowerCatalogChecks covers actual mixed-type placement/selection. Existing HudTowerChecks, MenuFoundationChecks, TowerStatChecks and TowerUpgradeChecks remain active. MenuTowerFlowChecks separately drives actual Boot/Main/Campaign/Level001 menu buttons and paid Battle construction/upgrades with an in-memory test profile, without writing saves.

Category filtering, extra inventories, selling, tower health and new weapon types remain future work. Phase 4C weapon extraction is not part of this HUD pass.


Phase 4C-2 registers real Laser Tower beside Battle Tower; its generated entry/Inspector/upgrade action require no generic HUD or placement branches. Laser displays effective DPS/range and Energy type rather than projectile rate/damage.

## Credit reward popup

`TopBar/CreditGainIndicator.cs` shows a temporary green `+credits` below the balance. It combines grants made within the UI-configured 0.8-second quiet window, then floats upward and fades. The authoritative Credits label still updates immediately. See `TopBar/README.md` for configuration, wallet integration and regression coverage.
