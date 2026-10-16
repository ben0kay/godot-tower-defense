# Menus — Boot / Main / Campaign / Pause

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
