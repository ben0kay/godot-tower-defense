# Credit producers — shared mechanic (PLANNED / Codex handoff)

**Status: design only; NOT implemented.** This is a reusable economy mechanic for (1) click-to-collect Citadel income and (2) the **Adaptable Resource Generator** (ARG), with an easy path for the existing planned passive **Resource Tower / Resource Generator**. Do not silently change existing gameplay when implementing.

## Source material (keep history distinct from the proposed Godot design)

**Verified in the old GameMaker TD hybrid repo:**
- \`objects/o_end/Create_0.gml\`: \`citadel_credits_current = 0\`, \`citadel_credits_max = 80\`, \`citadel_credits_amount = 10\`, \`citadel_timer = 120\` frames (2 seconds at 60 FPS).
- \`objects/o_end/Step_0.gml\`: income accrues **only during \`o_wave_manager.wave_in_progress\`**, only while alive and below the cap, and does not increase while paused. Left-click on Citadel calls collection if buffer > 0.
- \`scripts/citadel_processes/citadel_processes.gml\`: renders buffer text \`current / max\` and a green bar. \`scr_collect_citadel_credits\` grants first Citadel's buffer then clears **all** \`o_end\` buffers (an old mirrored-multiple-Citadel behavior; **do not replicate** in new multi-Citadel system).
- The older childhood notes independently say a Citadel generates **40 every 15 seconds**, Resource Generator produces periodically, ARG extracts resources from kills by nearby towers and pays slightly less than normal RG; old research \`Generator.2\` adds ARG passive **20 every 10 seconds**, and \`New Interest\` boosts Citadel income **25%**. These are **different historical designs**, not automatically one set of canonical values.
- The old GameMaker hybrid's \`BUILDING_DATA.gml\` contains a passive \`Resource Tower\` (e.g. tier-1 \`amount:3\`, \`fire_rate:300\` frames) and a \`Prefab Shelter\` that pays at end of round. The ARG design is evidenced by childhood notes, **not confirmed as a working object** in that repository.

**Current Godot reusable foundations:**
- \`ECONOMY/Wallet/EconomyManager.cs\` already owns the one session wallet, \`CreditsResource\`, \`Grant(resource, amount)\`, balance validation/caps, \`BalanceChanged\`, and \`GameplayHUD.ShowCreditReward\`. All producers **must grant through this**. Do not add a second wallet or manually modify HUD Credits.
- \`LEVELS/Waves/WaveManager.cs\` has \`WaveState.Spawning\`, \`SpawnBlocked\`, \`WaitingForClear\`, \`Completed\`, etc., and \`Changed\` / \`WaveCompleted\` events. **Wave active** means Spawning, SpawnBlocked, or WaitingForClear; starts on accepted wave start, ends only when all authored enemies/death children resolve. Neither \`Ready\` nor \`Completed\` produces Citadel income.
- \`WORLD/OBJECTIVES/Citadel/\` already has independently identified Citadels/Health and multi-Citadel HUD; respect that ownership. \`TOWERS/Economy/ResourceTower/\` is currently a planned passive tower.

## Separation of responsibilities

1. **EconomyManager (unchanged wallet authority):** only place actual Credits exist. Keep and reuse \`Grant(CreditsResource, amount)\`. Optional small \`TryGrantCredits(amount)\` wrapper only if meaningful; never duplicate currency logic. A successful grant triggers existing wallet event/HUD gain popup; failed grants do not erase an uncollected buffer.
2. **Reusable production/buffer helper** under \`ECONOMY/Production/\` (suggested \`CreditProducer.cs\` or \`CreditBuffer.cs\`, plain C# helper, **not another Godot Node per instance**): instance-local \`Current\`, \`Capacity\`, \`Produce(amount)\` capped, \`TryCollect(wallet)\` atomically credits and clears only the granted amount, change notifications for display, reset/dispose. Use simple seconds-based accumulator for periodic producers and explicit kill event input for event-based producers. Inspector values live in the owning Citadel/tower config, not in a giant global manager.
3. **Citadel-specific component** under \`WORLD/OBJECTIVES/Citadel/\` (a single focused script/component, no duplicate wallet): owns its buffer, pause/wave gate and Citadel click collection. One independent instance per Citadel. Death disables new accrual; preserve/reset existing buffer on death only according to a clearly chosen rule (suggest preserve uncollected balance but block collection after destroyed until session ends).
4. **ARG tower** under \`TOWERS/Economy/AdaptableResourceGenerator/\`: inherits existing shared tower placement/construction/durability/Inspector patterns, owns its own producer buffer/settings, collects **tower-attributed kill events** inside configured radius. Does not itself attack; no per-tick scans of all towers/enemies.
5. **Passive Resource Tower** under \`TOWERS/Economy/ResourceTower/\`: future producer can use the same credit helper with timer inputs, without copying wallet logic.

## CP1 interaction decision — collect from world Citadel OR its top-centre HUD cell (2026-10-18, PLANNED)

**User-approved requirement:** Both click targets collect Credits from the **same individual Citadel buffer**; neither view owns or duplicates Credits. This supersedes CP1 wording that only the world-space Citadel is clickable.

- **World click:** left-click on the actual visible Citadel/footprint to attempt collecting its available Credits. Use the existing world input routing and respect placement/selection/gameplay UI consumption. One mouse press must never simultaneously place/select and collect; avoid global polling and invisible oversized click regions.
- **Top-centre HUD click:** each Citadel health panel/cell should incorporate a **small green Credit buffer indicator** (suggest compact slim bar plus `stored/capacity` or `+stored`), distinct from the existing red/health meter and labels. Make **that Citadel's HUD cell/collection region clickable**, so collecting works even when the physical Citadel is offscreen. The health meter remains visible/readable, and credit presentation must fit the existing single horizontal row with responsive widths.
- **Per-Citadel ownership:** HUD cell `CITADEL 1` collects only Citadel 1; `CITADEL 2` collects only Citadel 2, etc. Clicking a physical Citadel also collects only that one. **No pooled buffers, no mirrored reset, no collecting all Citadels at once**, unlike the old GameMaker function.
- **Single authoritative action:** Both UI paths should resolve stable Citadel identity and call the same `TryCollectCitadelCredits(citadelId)`-style command/service, which reads that instance's `CreditBuffer`, uses existing `EconomyManager.Grant`, and subtracts Credits **only after a successful grant**. Use shared buffer events/signals to refresh **both** world and HUD representations immediately after accrual/collection; no duplicate grant or duplicated counters.
- **Expected interaction:** if Citadel A has 60/80 and Citadel B has 20/80, clicking A's world sprite or A's top HUD meter grants **60 once** and sets A to 0/80; B remains 20/80. A subsequent click on the other representation of A with an empty buffer grants nothing. Clicking B awards only its 20. During no active wave the meter remains visible and previously accrued Credits remain collectible, but no new production occurs.
- **Click feedback:** normal wallet balance update/green `+Credits` HUD feedback happens **once** per successful collection, independently of which click source was used. Empty buffer interactions do nothing; while wallet full, preserve uncollected Credits and display a light nonintrusive cue where practical. Do not create new Currency wallets or trigger rewards on ordinary selection when there is nothing stored.
- **HUD integration:** reuse the **existing multi-Citadel horizontal top-centre health HUD** and its stable Citadel identity bindings, rather than building an overlapping independent top bar. One Citadel keeps familiar existing health layout; multiple Citadels each get their own small credit indicator within their corresponding cell. Make hover/cursor, keyboard focus/tooltips and input filtering sensible while not capturing clicks on adjacent wave/Credits UI or world outside the cell.
- **Rendering/performance:** model events or a cached dirty flag update buffer text/fill on changes only. Don't poll all Citadels each frame and don't add an extra scene tree node to each producer solely for a timer. Normal buffer values should be accessible via tooltip in narrow HUD cells.
- **Reset/unload safety:** restart, mission end and scene changes disconnect world/HUD collection callbacks, clear any pending input, and cannot grant twice from stale identity references; destroyed Citadel policy remains the v1 decision documented above.

**Codex tests required:** single Citadel world vs HUD collection; two/four independent Citadel balances and clicking the correct HUD cell; clicking world Citadel B vs HUD B; clicking both targets successively without double-grant; zero buffer, full wallet, inactive wave, paused game, destroyed/removed Citadel, scene restart; input conflict with tower placement/selection; HUD at 1920×1080 / 1366×768 / 960×540, ensuring existing multi-Citadel health labels/meter, wave panel and Credits widget retain their current layout.

## Proposed Citadel v1 (old hybrid-inspired)

- Inspector-tweakable starting values: \`BufferCapacity = 80\`, \`CreditsPerCycle = 10\`, \`CycleSeconds = 2.0\`; \`CollectOnClick = true\`. Use **seconds**, not 120-frame counters; these are old *coded* defaults, intentionally distinct from the childhood notes' earlier 40/15s concept.
- Accrue when wave is active (Spawning / SpawnBlocked / WaitingForClear), Citadel is alive, session is not paused / game-over and buffer not full. **Between waves, accumulation stops**; no timer catch-up or off-wave accrual. Recommend freezing the remaining elapsed-cycle time while inactive (do not reset it); document whatever policy Codex chooses.
- On left click on the **actual Citadel footprint**, attempt \`Grant\` for its collected amount; **only on success** decrement/clear that Citadel's buffer. A zero-buffer click is a no-op. Existing construction/selection input priority must be respected: never both collect and place/select towers on one click; don't add overlapping global polling.
- Display \`current / capacity\` with a compact green buffer meter attached to that Citadel, independently of its existing HP meter. Keep world-space text/bar legible, no new gameplay authority in HUD. Refresh on production/collection only, not every frame.
- Multi-Citadel: Citadel A and Citadel B maintain **separate** buffer values/timers and are collected independently. No mirrored clearing, shared storage, forced synchronization or bonus collection from the nearest Citadel.
- When wallet is at its max or any \`Grant\` fails, preserve buffer until there is room. Decide/report behavior for partially available wallet space: proposed **grant up to free wallet capacity atomically** and keep remainder in buffer, or use all-or-nothing existing \`Grant\` and leave full buffer unchanged for simpler v1. **Do not discard resources or show fake Credits.**
- Consider Citadel research \`New Interest\` later as a producer modifier, not a HUD or wallet multiplier. Its original childhood-notes value is **+25%**, not necessarily currently unlocked in Godot.

## Proposed ARG v1 (design, not verified old runtime implementation)

- Legacy description: extracts resources from **other towers within its range killing enemies**, with somewhat less yield than a normal Resource Generator; tier/radius/rate are **not specified in the childhood notes**. Thus the following are **new suggested playtest rules**, not historical facts.
- Subscribe to a **shared authoritative kill/attribution event** carrying enemy + actual credited killing tower/source. Inspect \`EnemyRegistry.EnemyKilled\`, combat \`DamageSource\` / kill attribution, tower parent/owner lifetime before choosing the hook. Never guess the killer by looking for nearby towers *after* a kill; never credit tower hits without a confirmed kill.
- On one real kill, pay only if the **credited offensive tower's world position** is inside this ARG's configured radius (not merely the dead enemy's position). Guard against missing/dead/destroyed sources, friendly/non-tower sources, repeated death notifications and invalid state. No kill-origin double rewards; existing global \`KillRewardCredits\` stays independent and unchanged.
- Suggested simple balancing: an Inspector-defined **flat credits per eligible kill** to start (e.g. 1–2 Credits, *provisional*), optional max stored buffer, and separate collection policy. Recommend **buffered manual collection using the shared helper** so both Citadel and ARG share the same gameplay interaction; automatic immediate \`Grant\` can be an Inspector mode later if desired. Don't hardwire the childhood \`Generator.2\` passive upgrade yet.
- Multiple ARGs: choose an explicit design. **Proposed default:** every eligible ARG inside radius of the *killer tower* earns independently, permitting positioning incentives; enforce authored ARG build caps separately as needed and test reward multiplication. If balancing prefers only one, specify deterministic winner and test it instead. No silent ambiguous overlap.
- Pause, construction incomplete, disabled/destroyed tower, removed source, mission end: avoid accrual/collection according to the normal tower lifecycle/disable rules. Do not introduce unbounded subscriptions, per-frame spatial scans or retaining disposed enemy/tower references.
- UI: show buffered amount/capacity + in-range relationship through existing Inspector/range indicator. Use normal shared collection input pathway when clicking/selecting tower; adapt to existing tower click-select precedence (perhaps Inspector **Collect** button for ARG rather than click-to-collect directly to avoid interfering with selecting/upgrading the tower).
- Future \`Generator.2\` research adds **20 every 10 seconds** to ARG through this same producer helper; whether it also requires an active wave should be configurable, not assumed.

## Suggested phases for Codex

**CP1 — Shared economy production + Citadel buffer:**
- [ ] Inspect exact wallet \`Grant\` overflow semantics, GameplayHUD reward feedback, WaveManager \`Changed\`/state lifecycle, pause handling, existing Citadel hit-test, multi-Citadel identity and any current click-interaction precedence.
- [ ] Implement/test reusable **plain C# buffer/producer** and event-driven state changes under \`ECONOMY/Production/\`; integrate **one per Citadel** with the previous coded legacy 80-cap / +10 per 2s defaults, active-wave-only generation and click-to-collect.
- [ ] Independent Citadel collection and world-space compact meter; no duplicate Credits popup, no unintended selection/construction click.
- [ ] Tests: no wave, active wave, spawn-blocked, waiting clear, end wave, pause/restart/time scale, full buffer, collection, wallet max/failure, two separate Citadels, destroyed Citadel, no double click payout.

**CP2 — ARG tower:**
- [ ] Inspect live attributed-kill source contract and shared tower catalog/construction/rank/health/upgrade standards before editing.
- [ ] New tower \`TOWERS/Economy/AdaptableResourceGenerator/\` with Inspector configuration, 1×1 footprint unless designer chooses otherwise, range visual/upgrade tiers, existing purchase/construction/durability flow and no bespoke currency grant.
- [ ] Reuse producer buffer; only credited kills by nearby eligible **other towers** accrue. Prefer explicit collect via existing tower Inspector. Do not emit synthetic kills or modify standard kill rewards.
- [ ] Tests: inside/outside range of **killing tower**, credited projectile/damage-over-time kills, no credited killer, two ARGs, same kill once, paused/disabled/destroyed/incomplete ARG, end-of-wave, regular global kill reward unaffected, claim/grant at wallet limit, placement/upgrade and regression tests.

**CP3 — Existing Resource Tower and later research (optional):**
- [ ] Wire \`ResourceTower/\` to the same helper for passive income if/when desired; decide wave gate/auto-grant/collection policy explicitly.
- [ ] Add optional \`Generator.2\` +20/10s ARG passive research and \`New Interest\` +25% Citadel production through producer configuration/modifiers; research availability is separate work.
- [ ] Update \`ECONOMY/README.md\`, both tower readmes, Citadel README, \`NOTES/WORK.md\` and \`TESTS/Economy/\` with actual implementation findings/results.

## Performance / acceptance

- The wallet is always authoritative; collection is idempotent and single-source.
- No added per-enemy producer logic or scanning every tower each frame; prefer subscribe/unsubscribe to relevant events and one active timer per producer (or a central staggered producer scheduler if profiling warrants it).
- Pause/wave gates are centrally interpreted, not reimplemented inconsistently by every generator. Beware that \`WaveManager\` disables its own physics processing while WaitingForClear: the producer clock must be independently scheduled/gated.
- Align with \`NOTES/CodePreferences.md\`: simple small helpers, comment header, \`// =========================================================\` above methods, reasonable \`#region\` and Inspector tweaks. **One commit per job/pass**.
- Existing kill rewards, Citadel HP/multi-Citadel targeting, Level001/002/020 wave content, and current wallet/HUD/UI must remain intact unless a deliberate separate pass changes them.
