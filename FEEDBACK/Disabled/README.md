# Shared disabled tower visual

## Disabled tower feedback and stable NoPower presentation — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Placed disabled towers now show a small amber outlined exclamation triangle above the upper-right footprint corner and a subtle red art tint. `FEEDBACK/Disabled/DefaultTowerDisabledStyle.tres` tunes tint, badge color/size/offset. The shared TowerBase condition notification attaches one lazy `TowerDisabledVisual` only after native artwork initialization and only when needed. Initial construction sites hide the warning; completed disabled structures and paid upgrades show it. Work/disable/death events update presentation. No polling, bobbing animation, timer, active shader or per-frame redraw; active/never-disabled towers allocate no helper.

The helper caches Sprite2D descendants in scriptless art branches once, excluding scripted feedback/effect and GUI subtrees. Sprite SelfModulate multiplies a captured authored tint, leaving selection Modulate intact; split art, recoil and texture swaps keep their existing owners. All captured colors restore exactly on recovery/removal; overlapping reasons never compound the tint or clear it prematurely. Custom future art under a scripted container will need an explicit presenter extension rather than tinting arbitrary effects.

Inspector no longer emits a duplicate `NO POWER` entry in the Main warning row; other reasons retain their counts. The shared disabled header and ENERGY bar remain. PowerDetails keeps its three explicit lines at a fixed 48px minimum with wrapping disabled/clipping enabled, so changing connection/recovery text cannot add rows or grow the panel. Full power explanations remain in the existing tooltip. No power arithmetic, disable ownership, upgrade behavior, save data, gameplay balance or selection semantics change.

Focused HeatInspectorChecks expectations now omit duplicate NoPower and include dormant badge lifecycle. Source/scene review only; no Godot/.NET runtime or desktop layout test run. Owner: select an empty/recovering Battle, watch the panel during charge/depletion, inspect unselected disabled towers, EMP plus NoPower/heat overlaps, recovery, selection changes, upgrade art, 2×2/3×2 buildings and a fresh Save/Load. Verify that icons stay at the fixed upper-right corner and other gameplay effects retain their normal colors.

