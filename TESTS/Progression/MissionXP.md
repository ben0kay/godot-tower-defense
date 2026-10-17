# Mission XP checks

Run `res://TESTS/Progression/MissionXPChecks.tscn` in Godot .NET. It isolates profile identities in memory and progression files under a temporary directory; no real profile/index writes.

Coverage: provisional outcome scoring; signed-row reward floor; invalid settings; multiple player promotions/RP; v2 XP/research migration without write-on-read; atomic receipt persistence; duplicate after reload, original receipt after RP spending, older retired requests and coherent backup recovery; both actual battlefield victories; actual defeat; failed disk commit and paused Results retry; repeated retry; deployment profile switching; profileless F6; Results saved text/level change; presentation never awards; 1920×1080, 1366×768 and 960×540 Results-frame geometry.

Executed for this pass: .NET SDK build (0 warnings/errors), MissionXPChecks, PlayerProgressionChecks, ResultsChecks, ResearchFoundationChecks, ResearchMenuChecks and BattleResearchChecks passed. Existing Results navigation checks now verify Campaign return; the Results runner uses a temporary progression store because deployments now reserve persisted tickets. Research v1 migration assertions now expect current schema v3.

Desktop rendering/audio and animated XP count-up remain unverified/deferred. This is targeted coverage, not a claim that every historical integration/performance suite passes.
