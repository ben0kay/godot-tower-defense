# Shield-preferred targeting

Run `res://TESTS/Towers/ShieldTargetingChecks.tscn` in Godot .NET. It uses the real spatial registry, Laser scene/config and route manager, not a mock target selector. No profile files are written.

Coverage: None preserves ordinary targeting for other towers; out-of-range shields cannot block fallback; Laser defaults to Shielded; actual Laser acquires a farther shielded ship before a nearer ordinary ship; shield depletion falls back on the next scheduled search without rebuilding FinalStats; private opt-out leaves authored defaults intact. The existing JourneyTargetingChecks fixture now tests all six preferred modes, First/Last within a shielded group across two actual portal routes and six-mode fallback after depletion. It retains route-budget/RNG and 500-enemy cache checks.

Verified: .NET SDK build (0 warnings/errors), ShieldTargetingChecks, PlacementHudChecks and CompactInspectorChecks. Current Inspector HP/ammo/rank layout and build browsing regressions passed; desktop visuals were not reviewed. This pass adds no stealth/detection mechanic, in-game preference selector or extra per-frame scans. The config setting is independent of the existing in-game six-mode selector.
