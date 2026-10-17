**Latest IT1–IT4 update:**700×330 non-scrolling two-column Inspector supersedes the historical layouts below. Current executable checks and results: [InspectorTargeting.md](InspectorTargeting.md).

# Build categories / Inspector verification

**Update — IT1–IT2 (2026-10-16):** The panel now defaults to **540×325**, rather than the original 392×360. See [Inspector implementation plan](../../UI/GameplayHUD/Inspector/INSPECTOR_TARGETING_PASS.md). New code separates Rank XP from paid upgrades and adds a typed comparison grid. Existing HUD tests were adapted for the new rendered upgrade cells (`TESTS/UI/UpgradeTableChecks.cs`), but **no Godot/.NET compile or headless run has been completed against this change**. The historical test results below refer to the earlier Inspector build.

**Manual TODO:** Check 1920×1080, 1366×768 and smaller viewports; bottom-right anchoring/shorter height, Inspector vs tray, scroll to Upgrade, Details expand/collapse, earned Rank separation, long per-weapon rows, selection changes, during-work and max-level states, keyboard/focus and background-only 90% opacity.

## Historical compact Inspector verification (original 392×360)

`HudCategoryChecks.tscn` drives real catalog controls and paid placement in a production world. It verifies initial All, populated category tabs/counts, filtering, per-level tab memory across Ctrl, pending ghost cancellation, preserved placed-tower inspection, category retention/fallback on rebind, automatic new Support registration, safe empty catalogs, the existing40-entry scroll/action test, bounded392×360 Inspector geometry,176px tray height, access to scrolled upgrade actions, selection scroll reset and background-only90% alpha with normal input blocking.

```sh
dotnet build
godot --headless --path . res://TESTS/UI/HudCategoryChecks.tscn
godot --headless --path . res://TESTS/UI/MenuTowerFlowChecks.tscn
godot --headless --path . res://TESTS/UI/PauseControlsChecks.tscn
```

## Actual verification — 2026-10-16

- C# compilation passed with installed Roslyn/Godot source generators and reference assemblies. Ordinary dotnet build failed before compilation with an environment process-information exception (`Unable to retrieve the specified information about the process or thread`); the previously recorded incomplete SDK remains an environment limit.
- HudCategoryChecks, PauseControlsChecks, MenuTowerFlowChecks, ResultsChecks, FootprintChecks and AmmunitionLoopChecks passed with process exit0. No gameplay price/stat/wave/navigation changes were made.
- The unmodified EnemyIntegrationChecks remains blocked at the preexisting Level002 painted-terrain assertion. A diagnostic build temporarily omitted only Level002Checks.Run and restored its source immediately: dynamic catalog, catalog placement, pipeline, real paid upgrade preview/Inspector geometry, terrain, wallet and waves passed. It then stopped at the existing DamageTypeChecks assertion hardcoding the old12-damage Battle payload (`11.4` shield base / `59.4` remaining hull), while current Battle level1 is30 damage. This HUD pass preserves the current balance and does not claim a full integration pass.
- The diagnostic upgrade geometry failure introduced by category-row minimum height was fixed: the tray now fits its176px configured height, while the Inspector remains independently bounded and onscreen. Regression assertions remain active.
- Headless tests validate layout geometry/style/input/data behavior; a manual desktop visual review at all resolutions was not available.

Settings live in `UI/DefaultUIConfig.tres` via UIMasterConfig: InspectorWidth392, InspectorHeight360, BottomBarHeight176, BottomPanelOpacity0.90. Opacity affects lower panel backgrounds only, preserving readable foregrounds and existing tray animation. Category filtering is event-driven; no per-frame polling or new tower instances are introduced. HUD tab memory is level-local, with no saving/progression changes.
