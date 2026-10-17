# Battle Tower construction art

All three stages use the existing64×64 canvas, center and upward-facing design. Final artwork remains `../battle_tower.svg` unchanged. `construction_preview.svg` is a contact sheet of the actual assets for browser review, not a runtime dependency or Godot screenshot.

Edit `BattleConstructionArt.tres` through DefaultBattleTowerConfig → Construction Artwork → ConstructionArt:

| Build progress | Image |
| --- | --- |
| 0 | stage0_foundation.svg |
| .35 | stage1_frame.svg |
| .70 | stage2_assembly.svg |
| Complete | original battle_tower.svg |

The list supports any count. Null textures or invalid thresholds are ignored. Keep matching canvas/anchor/facing when replacing SVGs.

LevelArtwork has empty Level2 and Level3 slots. To give an upgrade one quick visual step, assign UpgradeTexture and its UpgradeProgress threshold, plus FinishedTexture for the completed tier. Without assignments, upgrades preserve the old image. If a later finished image is missing, retain the previous completed tier's image. These fields are artwork only; all combat values, Credits and work seconds remain in BattleTowerLevels.cs. No sparks/VFX are included.
