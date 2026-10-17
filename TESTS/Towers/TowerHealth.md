# Tower health checks

Run `res://TESTS/Towers/TowerHealthChecks.tscn` in Godot .NET. It uses the real catalog, level, placement/economy, construction manager, shared DefenseBar and Inspector.

Coverage: all nine registered weapon/passive types; full provisional 100 HP; hidden/no-tick healthy bars; damage visibility and footprint clearance; typed hull multipliers; damage does not invalidate FinalStats; pause; explicit capped repair; active rank HP (including HP-only ranks), eight-stage safety clamps; config/paid-tier capacity changes preserve missing health; HP comparison and live Inspector refresh; once-only destruction; no revival/refund; selection/whole-footprint occupancy/navigation release for weapons and economy structures; construction cancellation.

Executed: full source-generator C# compilation, TowerHealthChecks, HeatInspectorChecks, CompactInspectorChecks, PlacementHudChecks, ConstructionChecks and BattleResearchChecks passed. Broader legacy suite blockers are recorded in WORK.md; no full-suite pass is claimed. Desktop rendering/playfeel still needs review; automated headless geometry is not desktop visual QA. Enemy attacks remain a separate producer pass.
