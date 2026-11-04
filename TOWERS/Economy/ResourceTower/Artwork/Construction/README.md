# Resource Tower — initial construction stages

The Resource Tower remains a **single stationary root Sprite2D** with its original 112×112 SVG canvas (`viewBox="0 0 64 64"`) and 2×2 tower footprint. The shared event-driven `TowerConstructionVisuals` uses only `SpritePath=../Sprite`: no base/gun split, turret rotation, custom animation or extra update loop.

- **0%** — foundation slab with empty production machinery mount, `stage0_foundation.svg`
- **35%** — exposed structural frame, `stage1_frame.svg`
- **70%** — partly enclosed generator with scaffold, `stage2_assembly.svg`
- **100%** — original complete Level 1 `../resource_tower.svg`

The same `ResourceConstructionArt.tres` owns Level 2 and 3 finished and temporary upgrade visuals. Construction remains **2 seconds** for all purchased tiers. Credit production, stored Credits, payout collection, worker drones, capacity, work timing and economy processing are unchanged. [Upgrade artwork](../Upgrades/README.md). Source checks only; owner Godot visual review pending.
