# HD1/HD2 verification — 2026-10-17

Base GitHub main1aa83f1. Implements generic tower disabling and optional heat; no enemy EMP, power grid, save persistence or HD3 Inspector redesign. The latest projectile-speed/fire-rate balance is retained. Every installed authored Heat row remains null until gameplay balance is chosen; focused fixtures opt in through private complete row snapshots.

## Compilation

All project C# compiled with Roslyn4.11, .NET8 references, Godot4.7.2 API and its source generators. Godot editor import discovered the new scripts/scenes, and actual engine runners loaded the new assembly.

Standard `dotnet build --no-restore` was attempted and fails before compilation because this environment's SDK cannot load NuGet.Protocol6.11.2.1. Editor SDK discovery is likewise incomplete. This is a successful direct C#/Godot compilation and headless execution, not a successful standard SDK build.

```sh
dotnet build
godot --headless --path . TESTS/Towers/TowerConditionsChecks.tscn
```

## Focused checks — PASS

- Distinct simultaneous EMPs, same-source refresh, different-source/reason removal, foreign/expired handles, invalid durations and indefinite-only idle clocks.
- Generic IsDisabled/CanAttack separate from construction IsOperational; pause freezes duration/cooling and prevents attacks.
- Real Battle, Machine Gun, Sniper, Cryo, Laser, Blaster and Shockwave scenes: no disabled attack/ammo/heat, committed threshold attack, subsequent shutdown, cooling under a continuing EMP and independent recovery.
- Sniper aim and Laser beam shut off immediately. An already-launched Battle projectile still deals damage while its source is disabled. A simultaneous Machine Gun volley commits/charges both bullets before overheat; no partial volley/refund.
- Unfunded launches and zero-effective-damage beam time produce no heat. Laser thermal headroom limits the funded/damaging beam portion; half-second heat saturation/damage verified at30/60/120Hz. Float accumulation near capacity snaps within one-millionth to avoid a subcapacity stall.
- Matching cooling/hysteresis at30/60/120Hz, large-delta recovery, paused clocks and neutral cold state.
- Independent complete level rows/previews, paid upgrade, retained XP/rank/absolute heat, smaller-capacity clamp/trip and heat-capability removal that preserves EMP.
- Actual lazy Godot clock expiry even while the weapon itself is not processing; an expiry callback can queue owner removal safely.
- Invalid/zero capacities become absent; invalid rates become0; restart fractions are sanitized. No per-tick FinalStats rebuild.

Final benchmark:100,000 warmed single-active-tower timer ticks **40ms,0 managed bytes**. An additional500 independent source applications coexist on the same tower;1,000 expiry ticks across those applications also allocate0 managed bytes. These are clock/source stress observations, not rendered FPS or a claim about an entire500-enemy battle.

## Regression runners — PASS

| Scene | Actual result |
|---|---|
| TowerConditionsChecks | PASS |
| ProjectileVisualChecks | PASS |
| GenericEnemyChecks | PASS, including Laser, Blaster instant AoE, swept Shockwave and500-enemy cases; rerun after final Laser damage comparison change |
| ConstructionChecks | PASS |
| AmmunitionLoopChecks | PASS |
| InspectorTargetingChecks | PASS |
| PauseControlsChecks | PASS |
| ResultsChecks | PASS |
| CheckpointChecks | PASS |
| CorridorNavigationChecks | PASS |

`EnemyIntegrationChecks` was attempted: it still fails before reaching its linked HD1/HD2 group at the existing Level020 assertion, “The western open ground and eastern maze are fully painted” (exit1). No map/assertion changes were made to conceal that blocker. The new group has its own actual-world standalone runner.

## Remaining

HD3 heat/status Inspector display, actual per-tower thermal balance/opt-ins and desktop gameplay QA. Existing current/next/gain rows and620px non-scrolling layout remain unchanged. No claim that heat is currently active on authored towers or visible in the Inspector.
