# Gameplay HUD — status, catalog build tray and tower Inspector

The CanvasLayer HUD displays gameplay state. ECONOMY owns balances, CitadelSession owns objective HP, WaveManager owns scheduling, TowerCatalog owns registered build choices and TowerPlacementController owns placement/upgrade decisions.

## Current implementation

TopBar remains permanent: Credits left, Citadel current/max HP and compact health fill at the true viewport center, Wave right. Start/Next Wave sits below the right zone; SetWaveControls updates button/tooltip from real wave state. Health colors/clamping and the compatibility SetCoreHealth forwarding API remain intact. UIMasterConfig controls layout and appearance.

Ctrl opens/closes TowerBar with the existing slide/fade. Closing emits TowerBarClosed so placement immediately clears its ghost and build-preview details. RMB/Esc cancels placement while keeping the tray open; reopening does not restore an old placement. Placed-tower inspection remains independent. Its horizontal ScrollContainer supports arbitrary registered type counts without widening the HUD. `BindCatalog` reads available definitions and generates `UIButtonFactory` TowerTile buttons with canonical config name, category, construction recipe, icon and tooltip; it never instantiates tower scenes or owns stat math. DefaultTowerCatalog registers Battle and Laser towers. Unavailable entries are omitted; an empty catalog shows a safe message. No nonfunctional Laser/Missile/Repair/Solar/Resource samples remain.

Placement validates/rebuilds the exported Catalog at startup, then calls BindCatalog after the HUD is ready. Explicitly rebuild/rebind after runtime registration/availability changes; there is no frame-based registry/menu polling. GetBuildButton(typeId) accesses a generated action independently of scene-node names. TowerChoiceRequested carries that stable ID. Buttons retain keyboard focus and the shared factory's visual states; horizontal scrolling makes distant entries reachable.

The independent bottom-right Inspector shows the actual selected instance's identity, private config, level and capability-specific FinalStats. Upgrade details come from its real next-tier resolver and shared recipe display. TowerUpgradeRequested is an action request; the controller checks the expected level, same-frame duplicates, global AllowUpgrades and wallet transaction. No tiers means a safe No upgrades available state. Closed Inspector stays closed on stat/balance refresh; Ctrl tray state does not control Inspector visibility. The Inspector can grow upward while the tray retains BottomBarHeight.

The HUD root ignores unused mouse space; visible GUI controls consume input. Selection/range rendering stays in placement and draws only when the selected tower exposes InspectionRange.

## Files and verification

GameplayHUD.cs supplies status/action APIs and dynamic entries; TowerBar/TowerBar.tscn supplies header/scroll/entries container; Inspector/InspectorPanel.tscn supplies close/stats/upgrade controls; UI/Shared/UIButtonFactory supplies generated and authored button styles. The old six static button nodes and SampleTowers list have been removed.

TowerBuildBarChecks covers 40 definitions, unavailable types, prices/icons, action IDs, focus, scrolling bounds, rebinding/empty state and independent panels. TowerCatalogChecks covers actual mixed-type placement/selection. Existing HudTowerChecks, MenuFoundationChecks, TowerStatChecks and TowerUpgradeChecks remain active. MenuTowerFlowChecks separately drives actual Boot/Main/Campaign/Level001 menu buttons and paid Battle construction/upgrades with an in-memory test profile, without writing saves.

Category filtering, extra inventories, selling, tower health and new weapon types remain future work. Phase 4C weapon extraction is not part of this HUD pass.


Phase 4C-2 registers real Laser Tower beside Battle Tower; its generated entry/Inspector/upgrade action require no generic HUD or placement branches. Laser displays effective DPS/range and Energy type rather than projectile rate/damage.

## Top-bar polish

Credits use `Credits: 250 (+10)` inline, with a quick pulse/glow on repeated rewards and amber `(-200)` feedback for successful spending. The displayed balance counts smoothly to the already-committed wallet amount. Citadel HP text flashes and its bar eases on damage; newly advanced wave numbers briefly brighten and pulse. All values and animation durations are Inspector-editable under `UI/DefaultUIConfig.tres`; see `TopBar/README.md`. No combat or economy balance rules have been changed.

The placed-tower Inspector now includes the current named earned Rank, fractional within-rank/total XP, next-rank progress bar and active Damage/Range bonuses. Legendary shows MAX RANK. Selection events refresh only the current tower; XP-only updates are coalesced through a deferred callback and do not reset upgrade details or recalculate combat stats. Promotions and paid upgrades use the same ranked cache/preview pipeline. Build previews hide the rank section. See Inspector/README.md and TOWERS/Shared/Ranks/README.md.

Keyboard routing now lives in `CONTROLS/GameInput`: this HUD registers/unregisters its scoped Ctrl callback, while it retains ownership of tray animation and signals. Escape cancels placement first, then toggles the separate Pause overlay. Ctrl is consumed while paused; existing tray/Inspector state survives resume.

The placed-tower Inspector also has a separate RuntimeStats label for ammo, delivery status, factory buffers/production and depot stock/slots. Updates are event-driven and coalesced by placement; they do not erase rank/upgrade details or recalculate FinalStats.
