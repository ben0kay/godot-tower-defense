# Battle Tower recoil — R1 + R2 checks

## Status

Source-reviewed only on2026-10-20. No Godot import, C# build, headless checks or visual playtest was executed: the execution environment was unavailable. These checks are authored coverage, not evidence of passing execution.

## Run after pulling

Use the project's Godot .NET editor to import new SVGs/resources, then build the C# project. From the repository root, with your Godot .NET executable on PATH:

```sh
dotnet build
godot --headless --path . res://TESTS/Towers/TowerRecoilChecks.tscn
godot --headless --path . res://TESTS/Towers/ConstructionChecks.tscn
godot --headless --path . res://TESTS/Towers/ProjectileVisualChecks.tscn
```

TowerRecoilChecks covers deferred funded-shot impulses, launch/flash snapshots, fixed root/base/aim transforms, bounded repeat impulses, exact rest recovery, idle sleeping, pause, live style edits, target replacement, disable/destruction, initial construction, paid upgrades, rank/research snapshot isolation and Battle-only scene adoption. Existing art and projectile checks retain their other tower assertions with updated Battle paths.

## Manual acceptance

At normal game zoom, place/select Battle, observe all build stages, fire in several directions, purchase levels2/3 and observe recoil during old-tier upgrade firing. Confirm base stays fixed, barrel settles without drift, flashes and shots originate at the barrel tip, and the effect remains subtle. Check pause/resume, EMP, destruction/removal and preset disable/replacement. Compare another projectile tower to confirm its presentation is unchanged.

Try optional monolithic tier/upgrade artwork: the composite overlay should show alone, with split base/gun hidden and recoil suspended. Remove that custom artwork to restore default split presentation. Recoiling custom tier art needs matching split assets in a future pass.

No new gameplay/balance values are introduced. Physical barrel displacement intentionally changes subsequent launch origins by the small visual offset.
