# Shared weapon recoil — CW1
**Source Implemented / Awaiting Owner Validation.** `WeaponRecoilController` owns one bounded smoothstep kick/return in local +Y, captures/restores the exact visual target position and sleeps at rest. No per-shot tween, timer, node or resource allocation. `WeaponRecoilStyle` exposes enable, distance, kick/return duration and maximum accumulated displacement.

Hosts bind an explicit gun visual and call `Kick()` only after an accepted shot and muzzle/flash snapshots. They own lifecycle cancellation. Resource edits, target replacement/exit, hidden targets and unload reset the pose; scene pause freezes motion. There is no new onscreen policy or gameplay clock.

Existing tower controller/style scripts, resource classes/UIDs, snapshots and presets remain at their original paths as adapters. Their motion is the same shared implementation; tower scenes, recoil settings and weapon hooks are unchanged.

Gunner, Heavy Gunner and all four Obsidian Dreadnought cannons opt in through their EnemyMinigunConfig.Recoil presets (0.8/1.5/2px). Each initialized opted-in mount retains one controller. Only GunSprite moves; its fixed Muzzle is a sibling under TurretPivot at the same original rest barrel-tip coordinate. Accepted bullets/flash use that fixed physical origin before kick. Reinitialize, mount/config disable, freeze/lost aim, host death/arrival and removal cancel safely. Bullet cadence, aim, damage and existing world bullet ownership are unchanged.

Source/resource review only; .NET unavailable. Existing Gunner checks updated for the new physical marker path and focused visual-only/reset assertions, not executed. Owner to check tower recoil, all six enemy mounts, pauses/reuse and muzzle/flash alignment in Godot. CW2 splash is not implemented.
