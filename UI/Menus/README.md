# Menus — Implemented Boot / Main / Campaign foundation

Godot startup (`project.godot`) is `UI/Menus/Boot/BootScreen.tscn`. From F5:

```text
BootScreen — select/create local Profile
    → MainMenu — Campaign / Change Profile / Exit
        → CampaignMenu — Play Level 001 / Back
            → res://Main.tscn — existing World, Camera, GameplayHUD, Economy, Waves
```

`MenuNavigation.cs` owns the four scene paths and transition error handling; no menu duplicates that routing. Boot/Main/Campaign are lightweight standalone Control scenes with UI created by their C# scripts via `UI/Shared/MenuLayout.cs` and `UIButtonFactory.cs`. Screens own button actions; shared helpers own appearance/layout/navigation only.

Main/Campaign redirect to Boot if reached without a selected profile; Boot does not load the gameplay world. Campaign currently **offers Level001 only** via its Play button. No Levels selector, campaign data, automatic saving or Continue/Load actions exist yet.

**Development:** `Main.tscn` has deliberately **not** been renamed or edited. Open that scene and press **F6** to test gameplay directly, even without a selected profile; existing `TESTS/EnemyIntegrationChecks.tscn` still instantiates it. Press **F5** to test the full menu-to-Level001 flow. Pause scene remains reserved under `Pause/` (see its README), and returning from gameplay to menu will come with that pass.

Future: Campaign → Levels selection with unlock rules, saved slots and Continue/Load, plus Pause/Save/Exit; keep all persistence under `SYSTEMS/Saving`. Styling comes from the common factory instead of duplicating per-screen StyleBox overrides.
