# Research-first foundation — validation 2026-10-17

## Battle damage research extension — 2026-10-17

Added res://TESTS/Research/BattleResearchChecks.tscn. It uses temporary profile files and checks an actual purchase button/save/reopen, deployment snapshots, every Battle rank/level, both launchers, researched build/paid previews, actual projectile damage, unchanged unrelated stats/towers, config rebuilds, profile isolation and neutral profileless play. Existing ResearchFoundationChecks now expects three real catalog nodes. Execution evidence and exact limits are in BATTLE_RESEARCH.md.

---

## Dedicated runners

- res://TESTS/Research/ResearchFoundationChecks.tscn: pure catalog validation; free root; RP/prerequisite/duplicate purchase rejection; purchased IDs persistence; v1 progression migration and backup recovery; profileless HP; six-direction axial geometry; mouse hit parity after pan/zoom; keyboard selection; actual paid placements vs failed clicks; construction completion; upgrade payment vs completion; paused/frozen clock; immutable Results counters on both actual launchers.
- res://TESTS/Research/ResearchMenuChecks.tscn: actual Campaign button → Research scene; real Purchase button; filesystem-induced save failure with no RP/ownership change; repeated purchase/reopen; fresh-store reload; independent profile B; +10% max/current starting HP on both actual levels; unchanged authored LevelConfig; purchase API denied to battlefield; no Results Research link; research canvas/Back geometry at 1280×720, 960×600 and 800×600.

Tests use unique temporary directories and restore in-memory service/profile state. They do not write real profile identity or progression files. Graph selections never purchase; failure tests deliberately create a temporary conflicting .tmp directory and remove it afterwards.

## Actual execution

Godot 4.7.2 Mono / .NET 8 runtime; production C# and tests compiled with Roslyn 4.11 plus Godot source generators and net8 reference assemblies. Latest GitHub main C#/tscn/tres/md/project files and changed SVG were synchronized into the available local checkout before this pass. A concurrent Battle heat timing commit was incorporated before publication; source-generator compilation, both Research runners, Results, menu flow and BattleHeatTimingChecks were re-run on the combined sources.

PASS: source-generator C# compilation; PlayerProgressionChecks; ResearchFoundationChecks; ResearchMenuChecks; ResultsChecks (R1 kill/breach/race/reveal/Skip/navigation checks, updated tracked-stat expectations); ConstructionChecks; FootprintChecks; DeathSpawnChecks; PauseControlsChecks; MenuTowerFlowChecks. Existing missing-Catalog fallback warnings may be printed by legacy fixtures. After merging the heat commit, the combined Results runner initially hit a native resource-wrapper/finalizer failure between the outcome and navigation fixture groups. Its existing DrainFreedFixtures helper is now called between those groups; the complete runner then passed, including actual Retry/Next/Menu navigation. No engine/test errors remained in that completed run.

Standard dotnet build --no-restore was attempted but this environment's CLI crashed while retrieving process start information (Win32Exception/exit 134). Godot editor import additionally reports missing expected SDK 8.0.31 / Microsoft.Build. The source-generator compile and headless test paths succeeded; a standard SDK build is not claimed passing. The monolithic integration suite was not re-run for this pass.

Headless geometry is not desktop visual acceptance. Real viewport appearance, graph readability at zoom extremes, scrolling, mouse drag feel and Results reveal still need a Godot editor playtest. No screenshot-based visual approval is claimed.

## Manual acceptance

1. F5 select a disposable profile. Enable ShowProgressionDebugGrant in Main/Campaign only if test RP is needed (off by default, DEBUG only). Grant 600 XP → Level2 / 1 RP.
2. Campaign → RESEARCH. Select Citadel Master I by mouse or arrow keys; inspect prerequisites, description, +10% effect and 1 RP cost. Purchase: balance 0 RP, node/info/button RESEARCHED. Repeat cannot charge.
3. Back to Campaign; deploy either existing level. Starting max/current Citadel health is 110% of its authored maximum. Results uses that effective denominator.
4. End a mission after placing/building/upgrading towers; inspect distinct placed/constructed/upgrades and elapsed game time. Failed builds do not count; pause/results do not add time; Credits remain —.
5. Results offers its existing navigation with no Research shortcut. Return Main → Campaign → RESEARCH. Relaunch and select the same profile: upgrade still owned. Another profile has separate RP/effects.
6. Restore the debug option to off before saving/committing scene changes.

Deferred: mission XP scoring/replays/once-only award tokens, finalized XP balance, tower damage research, giant tree content, refunds/doctrines, campaign/battlefield saves and optional R2 polish.
