# CP2 / CP3 credit tower verification — 2026-10-19

Build with Godot .NET 4.7.2 / .NET8, then run `res://TESTS/Economy/CreditTowerChecks.tscn` headless.

Passed:

- C# build: zero warnings/errors. Both original SVGs inspected with vector rasterization; real scenes/configs load through the catalog.
- Both Economy build buttons, real wallet-paid placement and two-second construction. The fixture uses a private open buildable map, not changes to authored levels.
- Periodic active-wave-only production, exact interval, inactive/disabled/paused gates, capacity cap/no automatic wallet payout.
- Inspector collection, once-only transfer, empty/paused/mission-end rejection and full-wallet preservation.
- Resource tiers2/3 and ARG tier2 through real paid upgrades; busy upgrades suspend income, capacity grows without losing stored Credits, new payouts and range apply, config tiers are independent copies. Shared comparison has economy rows rather than fake weapon stats.
- Killer-position range vs victim position, independent overlapping ARG payouts, unattributed hits, disabled/destroyed ARG, stale queued source and the final owned wave kill. Ordinary kill Credits and exact finite damage/kill tower-XP budget remain unchanged.
- Existing `TESTS/Economy/CitadelCreditChecks.tscn` and `TESTS/UI/CitadelCollectionChecks.tscn` pass. User separately reported CP1 gameplay looked fine on2026-10-18.

Broader existing fixtures were not counted as passing: ProjectileVisualChecks fails in its authored-map placement/target setup (the unchanged base84eb2bc also fails its target sequence); TowerRankChecks fails the same travelling Shockwave falloff assertion on unchanged base84eb2bc and with this change (`2.392094802856445 != 2.125`). These baseline failures were reproduced in a separate worktree; no unrelated combat/map values were changed to satisfy them. The new focused fixture explicitly verifies ordinary credited-kill XP and wallet payout around ARG income.

Desktop Inspector layout/playfeel and final economy balance remain for review. No rendered FPS measurement, campaign battlefield saving, optional Generator.2/New Interest research or new DoT attack is claimed. New towers do not add per-enemy processing or scan every tower on each frame.
