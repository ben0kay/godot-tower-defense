## P3 Campaign hub / Levels split — implemented 2026-10-17

- Campaign now shows the selected profile's saved player level, XP and Research Points, followed by distinct **LEVELS** and **RESEARCH** destinations.
- **LEVELS** opens UI/Menus/Campaign/Levels/LevelSelectionMenu.cs/.tscn with the existing Level001 and Level002 mission descriptions/launch buttons. Back returns to Campaign. Research continues to return to Campaign; Results already returns to this hub after mission rewards.
- Existing MenuLayout, UIButtonFactory, profile guards and MenuNavigation are reused. No Research access was added to Results/gameplay. Both missions remain available; mission unlocking, completion stars, campaign saves/slots and Continue/Load remain separate future work.
- This completes the remaining **P3 menu organization slice**, alongside the previously implemented progression display. It does not complete all P1–P5/R2/R4 scoring or polish.
- Verification: SDK build succeeded with zero warnings/errors. MenuTowerFlowChecks, ResearchMenuChecks and ResultsChecks passed; viewport checks passed at 1920×1080, 1366×768 and 960×540. The broader controls runner hit a native Godot finalizer/resource crash; no full-suite pass is claimed. Details are recorded in TESTS/UI/CampaignHub.md. Desktop visual review remains for the user.

Older menu descriptions below are retained as history and superseded by this amendment.

---

# Menus — Boot / Main / Campaign / Pause

## Research-first extension — 2026-10-17

Campaign now includes RESEARCH alongside its existing two level deployments; Research has only BACK TO CAMPAIGN as its navigation exit. Purchases are profile-persistent and guarded to the actual active ResearchMenu scene. Main and Results do not expose a Research shortcut. The separate LEVELS-area reorganization remains future P3 work.

---

Godot startup (`project.godot`) is `UI/Menus/Boot/BootScreen.tscn`. From F5:

```text
BootScreen — select/create local Profile
    → MainMenu — Campaign / Change Profile / Exit
        → CampaignMenu — Play Level 001 / Back
            → res://Main.tscn — existing World, Camera, GameplayHUD, Economy, Waves
```

`MenuNavigation.cs` owns the four scene paths and transition error handling; no menu duplicates that routing. Boot/Main/Campaign are lightweight standalone Control scenes with UI created by their C# scripts via `UI/Shared/MenuLayout.cs` and `UIButtonFactory.cs`. Screens own button actions; shared helpers own appearance/layout/navigation only.

Main/Campaign redirect to Boot if reached without a selected profile; Boot does not load the gameplay world. Campaign offers **Level001 and Level002** via its Play buttons. No Levels selector, campaign data, automatic saving or Continue/Load actions exist yet.

**Development:** `Main.tscn` remains the direct battlefield entry. Open that scene and press **F6** to test gameplay directly, even without a selected profile; existing `TESTS/Integration/EnemyIntegrationChecks.tscn` still instantiates it. Press **F5** to test the full menu-to-Level001 flow. Both battlefield scenes now instance `Pause/PauseMenu.tscn`: Escape pauses/resumes, with working Resume, Quit to Main Menu and Exit Game; Save/Load/Options are disabled placeholders. Shared shortcuts live in `CONTROLS/GameInput.cs`.

Future: Campaign → Levels selection with unlock rules, saved slots and Continue/Load, plus campaign Save/Load; keep all persistence under `SYSTEMS/Saving`. Styling comes from the common factory instead of duplicating per-screen StyleBox overrides.
