# Battle minutes-based heat verification — 2026-10-17

Base dc6040a retains the separately pushed player progression and shared beam work. Only Battle receives enabled default heat in this pass. HD3 gameplay Inspector heat presentation remains deferred.

## Executed

All C# compiled with Roslyn4.11/.NET8 references/Godot4.7.2 API/source generators, then actual Godot headless scenes ran. Standard SDK dependency limitations remain documented in TowerConditions.md; this does not claim a successful standard dotnet build.

Seven standalone runners exited0: BattleHeatTimingChecks, TowerConditionsChecks, ProjectileVisualChecks, ConstructionChecks, AmmunitionLoopChecks, GenericEnemyChecks and InspectorTargetingChecks.

The timing suite ran18 real weapon/cooling simulations: target3 and3.5 minutes × levels1–3 ×30/60/120Hz. Measured cold-start overheat:

| Config minutes | Target seconds | Observed range |
|---|---:|---:|
| 3 | 180 | 179.767–180.492s |
| 3.5 | 210 | 209.800–210.350s |

Recovery to35%:9.000–9.033s. Explicit idle cooling, no ammo spending while recovering, live private config tuning/off switch and unchanged per-tick stat cache were verified. Tests use the selected complete row as the timing fixture's first row; existing construction/purchase suites separately validate actual paid tier transactions/previews.

The old900-shot logistics fixture now advances the separate cooling clock by its same2-second weapon step before each shot, instead of freezing heat while pretending weapon time elapsed. HD1/HD2 explicit-capability fixtures disable the authored timing fallback when testing Heat=null removal/absence. These changes preserve the intent of the existing tests and do not bypass authored Battle heat in the timing suite.

Battle now retains fractional cooldown remainder, avoiding frame-rounded shot periods that would distort both authored firing rate and long thermal timing. No level price/rate/speed/damage rows changed. No accumulating purchased modifiers, new research, saving, UI layout or other tower heat balance was added. Rendered playfeel/UI approval remains pending; the full-suite inherited Level002 terrain assertion is unchanged.
