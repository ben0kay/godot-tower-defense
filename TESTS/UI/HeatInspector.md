# HD3 heat and generic disable Inspector — verification 2026-10-17

Rebased onto GitHub main c6e9731, preserving its Campaign research/results work. No combat/heat balance, stat stages, credits, navigation, research or player progression changes. Battle keeps its enabled3-minute cold-start/9-second recovery profile. Other towers remain heatless until configured, but generic disable states display for them too.

## Executed

All C# compiled successfully using Roslyn4.11, .NET8 references, Godot4.7.2 API and its source generators. Editor import discovered the added test scene. The installed standard SDK's previously documented dependency limitations remain; this is not a claim of a successful standard dotnet build. See TESTS/Towers/TowerConditions.md.

Nine headless runners exited0:

- HeatInspectorChecks
- InspectorTargetingChecks
- HudCategoryChecks
- ConstructionChecks
- AmmunitionLoopChecks
- TowerConditionsChecks
- BattleHeatTimingChecks
- PauseControlsChecks
- ResultsChecks

The focused runner uses real tower selection and event bindings. It verifies cold capacity/value, actual launch overheating, a live draining bar, overheat recovery while EMP remains, source counts/removal, no fake heat for a disabled heatless Laser, selection/close/reselect, pause, paid capacity changes without heat loss, preservation of the stat-cache reference and comparison child instances, and a pending UI callback across world disposal. Tests freeze only fixture clocks at observation points; gameplay clocks remain the normal shared subsystem.

## Compact geometry

At1920×1080,1366×768 and960×540, actual panel size is **620×330** and all visible child controls remain inside it. Verified all nine current building families with max-tier/construction/upgrade states using the existing Inspector layout group. Additional worst-state fixture combines a Legendary Battle Tower, full heat, all four disable reasons including two EMP sources, and an active paid upgrade. It also remains620×330 at all three sizes.

Spacing is tighter without changing font sizes. The busy upgrade status is already shown in its header/button, so its repeated explanation lives in a tooltip while the paid-cost line remains visible. Detailed current/next heat stats also use a tooltip instead of five new permanent table rows. Existing target buttons, rank/XP, ammo, comparisons and purchase behavior remain present. No scrolling or foldouts were added.

## Update ownership / remaining QA

Only selected towers have heat/disable UI subscriptions. Close/switch/unload detach them; deferred callbacks guard controller/HUD/tower lifetime and read the latest selection. Heat notifications update only the condition card/header; cached upgrade snapshots are never re-resolved from a heat event. Cooling events are bounded by the existing shared clock and notifications coalesce within a frame. No all-tower UI polling was added.

Manual normal-zoom artwork/readability approval remains pending; headless layout does not establish pixel appearance. Full EnemyIntegrationChecks was not rerun for this presentation-only follow-up; its previously documented Level002 terrain assertion remains outside scope. No EMP attack, power grid, cooling particles/audio, save state or new heat balance was introduced.

All nine listed runners were rerun successfully after the rebase; combined C# compilation also passed.

## Current 560px layout follow-up — pending execution

The Inspector has since narrowed from620 to560px. HeatText/HeatBar moved from `Columns/Main/Conditions` to `Columns/Side/Conditions`, beneath Combat Rank/XP; DisableReasons remains on the left as `Columns/Main/DisableReasons`. The regression test paths and width assertions were updated, but the above `620×330` execution results are **historical**. Godot editor/headless testing and desktop inspection of wrapped hot/disabled/Legendary states at1920×1080,1366×768 and960×540 are still required before reporting a passing 560px geometry check.
