# Mission XP foundation

Current defaults are intentionally provisional: **victory 500 XP, defeat 0 XP**, authored independently in each `LEVELS/Level00N/Level00NConfig.tres` under **Player XP Rewards**. Every replay currently earns the same baseline. No tower-placement/kill/time farming bonuses, first-clear multiplier, replay reduction or penalty formula is enabled.

## Ownership and extension

- `MissionXP.cs`: immutable captured `MissionXPPolicy`, signed `MissionXPLine` rows, deterministic `MissionXPBreakdown`, ticket/receipt/result data. Add approved bonuses/deductions in `Calculate` using actual frozen `LevelResultSnapshot` fields. Results renders the rows generically; it contains no score formula. Negative rows can reduce a mission reward to zero, but never take previously earned XP/levels away. Do not change `PlayerProgression.AddXP` into an RP-spending or level-removal system.
- `MissionRewardLedger.cs`: reserve a monotonically increasing mission number at deployment; complete its reward once after outcome capture. `LastIssuedMissionNumber`, `LastAwardedMissionNumber`, latest receipt, XP and RP live in the **same atomic progression record**. No separate award-marker file or growing GUID array. New/retry deployments get new numbers; abandoned runs leave gaps. A duplicate of the latest awarded run returns the persisted receipt without saving/notifying; older retired requests are rejected. This assumes the existing one-active-battlefield application flow, not concurrent independent game processes sharing one profile file.
- `ProgressionService.Missions.cs`: validates the captured deployment profile against current selection, uses the existing store, notifies menu views only after a successful commit. Profile switches cannot redirect XP to another player.
- `LevelSessionController`: captures policy/profile/ticket at deployment, freezes authentic outcome statistics, attempts the award before Results presentation. Authoritative R1 victory/defeat arbitration is unchanged. An empty mission, abandoned scene or F2-only kill cannot grant victory XP. No per-enemy save calls or combat polling.

The selected profile's existing curve grants **one Research Point per crossed player level**, including multiple promotions. XP is distinct from tower rank XP and Credits. Stored curve parameters remain unchanged for existing players. The first deployment reservation can now establish a new profile's stored curve before it earns XP.

## Save format and errors

Progression schema **v3** adds two counters and the latest `MissionAwardReceipt` to v2's XP/RP/curve/research fields. v1/v2 migrate in memory without a write-on-read, reset or profile-index change. Next successful save writes v3. Validate watermarks, receipt/balance consistency and source fields before accepting or replacing data; existing bounded-file/backup behavior remains.

Save/overflow/profile errors leave the reward uncommitted and display **SAVE FAILED** with **RETRY XP SAVE**. Retry does not replay the mission, change frozen stats or duplicate a committed award. A preparation failure can reserve a ticket on explicit retry. Navigation remains available; because battlefield saves do not exist, leaving an unawarded failed mission does not preserve a pending reward for another launch. Profileless direct F6/preview runs explicitly say **NOT SAVED** and create no profile or XP award.

## Results and Campaign

Results shows the saved itemized reward, before/after level change, Research Points gained and final within-level XP bar. Existing reveal/Skip never grants rewards. Detailed count-up across level boundaries, sounds and additional animation remain R2 polish. **RETURN TO CAMPAIGN** opens the existing profile hub, where progression refreshes from storage and Research is available between deployments. No Research button/purchase path exists on Results or during gameplay. Profileless return follows Campaign's existing Boot redirect.

Remaining original P1/P2/R4 design: first-clear/replay history/policies, ETA/time rewards, performance bonuses/deductions, richer animation. P3's separate Levels area, mission unlock progression, campaign/battlefield saving and save slots remain separate work. See `TESTS/Progression/MissionXP.md`.
