## CD4 extension — builder-assisted upgrades (implemented source 2026-10-27)

The next approved pass extends **DU1/DU2** using the existing `ConstructionDroneManager` and `TowerWorkPhase.Upgrading`, rather than a second upgrade clock or standalone builder. It is source-implemented on `dev`; Godot/.NET build and visual acceptance pending.

- A successful paid **nonzero-duration** upgrade creates a builder hold before registering work; only the **post-payment** notification enqueues the flight. No transaction or reservation occurs for rejected/instant upgrades.
- Tower remains disabled from payment until the existing timed upgrade finishes. Work progress starts at zero and stays frozen while the drone flies/deploys; on cyan beam activation, its reservation is released once. Completion commits the purchased tier, resumes the tower and sends the same drone home; the return never delays shooting.
- The existing builder dispatch cap/FIFO/nearest-Citadel/pool/fallback/hover/beam applies equally to initial builds and upgrades. `DefaultConstructionDroneConfig.tres → ServiceUpgrades` is the enabled-by-default opt-out. No per-tower physics polling or new currency. No new initial construction stage is shown during an upgrade.
- Shared `TowerLevelArtwork.UpgradeStages` optionally contains several thresholds; Battle Level2 and Level3 each show intermediate composite SVGs at **35%** and **70%**, retaining the prior finished tier until the first step and switching to the new split fixed base/gun at 100%. Existing single `UpgradeTexture` remains compatible with other towers.
- The established `Held` bit and `BuilderSaveData` are reused for mid-upgrade checkpoints. Loader validation now accepts held `Upgrading` jobs, verifies that corresponding drone assignments exist, and preserves the exact resumed progress. Beaming upgrades save as active work with a builder that is already deployed.
- HUD exposes `AWAITING UPGRADE DRONE → N`, `UPGRADING → N` and existing `DISABLED: UPGRADING`. The ordinary work bar remains the only upgrade bar; all other disable/heat policies persist independently.

Scope deliberately leaves upgrades as the **same paid work durations** (e.g. Battle 4s/5s after beam) and still subject to work pause. Existing regression tests use a builder-disabled fixture when testing independent upgrade timing. Manual acceptance: buy Battle L1→L2 and L2→L3, verify launch/deploy, frozen timer, 35/70/final SVG stages, return, and save/load in flight and beaming; turn `ServiceUpgrades` off and confirm original behavior.

---

# Upgrade disabling and purchased-level artwork — DU1/DU2

**Status (2026-10-27): DU1 and DU2 implemented in source on `dev`; local .NET/Godot and visual acceptance pending.** This plan belongs in `TOWERS/Shared/Upgrades/` because paid work, the disable policy and tier visuals apply to all tower types. Battle is the art pilot; the authored art lives under its own `Artwork/Upgrades/` folder.

## Existing foundation

- Paid level tables in each `*Levels.cs` already define prices, upgrade `WorkSeconds` and whole-level final stats. `TowerBase.WorkPhase`, `PendingUpgradeLevel`, `ConstructionWorkManager` and the selected work HUD already implement paid timed work.
- `TowerBase.Conditions` provides generic `IsDisabled` / `CanAttack`, HUD condition reasons, and per-weapon `OnDisableChanged`. Construction/paid-upgrade state persists through S2/S3 save/restore.
- `TowerConstructionArt` and `TowerLevelArtwork` already expose optional `UpgradeTexture`, `UpgradeProgress`, `FinishedTexture`. `TowerConstructionVisuals` observes `WorkStarted` / `WorkProgressChanged` / `WorkCompleted`. Battle's completed artwork uses a static base and a rotating/recoiling gun node, not a fixed composite.

## DU1 — disable towers during paid upgrades

1. **Authoritative state:** treat `TowerWorkPhase.Upgrading` as a shared derived `TowerDisableReason.Upgrading`, not a new independently applied condition/timer. This avoids duplicate save entries, stale handles, and redundant ticking. It combines with EMP/overheat/no-power statuses; the normal reason list displays **DISABLED: UPGRADING** (plus any other active reasons). Manually applying `Upgrading` as an unrelated effect is invalid.
2. **Activity:** once the existing atomic wallet purchase succeeds, immediately disable acquisition, firing, beam damage, wave emission and other owner physics via the existing `CanOperate` gate and `SetPhysicsProcess(false)`. Notify `DisableStateChanged` after payment, so recoil/targeting/beam clean up and the selected Inspector updates. Do **not** cancel already launched projectiles or snap spent ammo/refund payment.
3. **Completion:** the shared work clock applies the paid absolute tier/earned rank once, restores owner processing, and publishes the normal disable/WorkCompleted events. The tower resumes against its usual state, unless a *different* reason (EMP/heat) remains. Zero-duration upgrades are immediate and never enter the disabled phase. Failure/cancel/free must not leave a tower falsely enabled.
4. **Savings:** `WorkPhase` already persists. Reconstruct the derived `Upgrading` reason on reload; never serialize it in `Disables` or allocate a `TowerConditions` node merely for work. Existing heat/EMP states retain independent ownership.
5. **Scope:** `IsOperational` remains true during upgrading for backwards-compatible health/occupancy/logistics rules; **`CanOperate` and `CanAttack` are false** and the normal tower owner physics loop sleeps. Suppressing non-physics production outside the owner loop is a separate follow-up if such producers opt into the mechanic.

## DU2 — visual tier pilot on Battle Tower

1. Keep Level1 original `battle_base.svg` + `battle_gun.svg` fully intact. The shared construction presenter gains *optional* `FinishedBaseSpritePath` and `FinishedGunSpritePath` plus independent `TowerLevelArtwork.FinishedBaseTexture` / `FinishedGunTexture`.
2. Author **Level2** and **Level3** pairs of 64×64 upward-facing SVGs sharing exactly the original registration. Level2 adds reinforced cooling/blue-light details; Level3 has heavier reactor bracing and distinct higher-tier gun shroud. The single muzzle marker remains `(0, -28.5)`. Fixed base stays fixed; gun still tracks and recoils. This avoids swapping the finished tower into a single static composite.
3. Both levels also get a distinct temporary 64×64 `UpgradeTexture` overlay at `UpgradeProgress = .35`, hiding split layers for the remainder of the short disabled work clock. On completion the overlay disappears immediately and the purchased pair becomes visible. The original initial-build 0/35/70% construction stages are unchanged.
4. Bind all textures in the existing `Artwork/Construction/BattleConstructionArt.tres` Level2/Level3 subresources. No separate tier-aware tower code, duplicate nodes, new scene classes, per-frame texture polling, gameplay stat changes or level-specific upgrade controller is introduced.
5. Other towers currently retain their existing finished art; the same optional split pair paths and per-level sprite slots are available later with no new shared machinery.

## Checks and follow-up

- Focused `TimedUpgradeChecks`: paid work prevents attacks/ammo spending, resumes physics at commit, preserves current XP/rank/ammo, respects pause and overlapping work, and still handles instant/invalid purchases.
- Focused `ConstructionArtChecks`: old custom-composite fallback stays supported; Battle's shipped Level2/3 scaffold and split art swap at authored thresholds while muzzle and recoil nodes remain.
- `TowerRecoilChecks`: disable cancels active recoil and prevents fresh impulses during work.
- Manual Godot check pending: click Upgrade on a firing Battle Tower, see **DISABLED: UPGRADING**, no shots during work, blueprint overlay at 35%, and a visually different level2/3 rotating/recoiling turret after completion. Reload a between-wave save captured mid-upgrade and check the same state. QA also needed for selection tint and scale at normal zoom.

**Deferred:** changing every tower's art, more animation phases/VFX, assigning builders to *upgrade* jobs, balance or timescale changes, and production/logistics redesign.
