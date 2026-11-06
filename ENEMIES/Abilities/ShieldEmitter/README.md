# ShieldEmitter — bounded friendly restoration

## Shield Emitter world-space radius — 2026-11-06

The Shield Emitter ship now owns a thin, faint violet range indicator through `ShieldEmitterRangeVisual.cs`, an optional Node2D child on `ENEMIES/Types/Enemy003/Enemy003.tscn`. It draws a low-opacity filled disc plus a 1.5px violet perimeter **behind the ship**. Its radius is taken directly from the emitter's **instance-local `ShieldEmitterConfig.RadiusPixels`** (192px by default), which is the same radius used by shield-restoration pulses; it is not a separate debug distance or fixed artwork size.

The `ShieldEmitter` ability is authoritative for visual activation: the ring is visible only for a live/registered, enabled emitter with valid pulse configuration and positive `ShieldPerPulse`, and is hidden on disable, invalid/null config, death, arrival and unload. Inspector edits to the live `Config.RadiusPixels` trigger the existing resource `Changed` event and redraw the ring **once**. No visual `_Process`, visual `_PhysicsProcess`, particle, shader, tween or spatial scan is added; the circle simply inherits its host's world transform. The visual does **not** alter pulse eligibility or guarantee that every ship inside currently receives shields—eligible targets still require MaxShield capacity, respect `Eligibility` and share the existing per-pulse budgets. Only `Enemy003` currently opts in; Shield Frigate has no emitter ability or aura.

`TESTS/Towers/ShieldEmitterChecks.cs` contains focused assertions for source-specific range changes, lifecycle visibility, absence on ordinary enemies and the sleep state. Godot/editor visuals and runtime checks are still pending.

---

Optional Node component with an explicit Host reference or ancestor Enemy lookup, and an Inspector ShieldEmitterConfig. It starts only after that host is alive/initialized with its world registry. Each component copies its config and owns its own timer/cursor. Exported Enabled, null/invalid configs and missing hosts are safe inactive states. Config changes restart one staggered schedule; duplicate initialization does not add another timer.

Default pulse: 192px radius, 20 shield points every 1 second, AffectSelf=false, AnyShieldCapacity eligibility. AlreadyShieldedOnly can restrict recipients to positive current shields. Restoration calls Enemy.RestoreShield, clamps at the recipient's authored MaxShield and does not change capacity, hull health or the hull-only DefenseBar. Zero-capacity enemies remain ineligible. Wave shield:true still controls starting shields only; an active emitter can raise eligible zero-current shields afterwards. It provides no separate linked/temporary pool.

Queries use EnemyRegistry.Living only on the pulse interval. Initial phase is staggered across 25–100% of the interval by host instance ID. Each pulse checks at most MaxCandidatesPerPulse (default 128, hard ceiling 512) and restores at most MaxRecipientsPerPulse (default 32, hard ceiling 512). Cursor advances through the living list so large populations are serviced across pulses; removal/reordering may shift the next slice. No scene/group rescan, A* calculation, per-target timers or same-frame catch-up burst is added. Among more eligible allies than the budget, some wait for a later pulse.

Death/arrival stops processing; component/level unload detaches resource/host subscriptions; queued-for-deletion sources/targets are ignored. Existing recipient shield points remain when a generator disappears. Shared config defaults remain unchanged; tune the saved resource for new instances or Remote component Config for an individual live emitter. Disabled/null/invalid sources can be re-enabled/reconfigured safely.

ShieldEmitterChecks covers two-source timers, recipient isolation/caps/range/eligibility/self policy, invalid/restoration overflow safety, no-ability plain types, candidate/recipient limits, lag behavior and death/arrival/unload/queued-source cleanup. These are headless behavior checks, not a benchmark of 500 simultaneous emitters. Spatial indexing should follow measured need.
