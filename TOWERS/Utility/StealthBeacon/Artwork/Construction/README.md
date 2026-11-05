## Level 2/3 construction and upgrade visuals (2026-11-05)

The same `StealthConstructionArt.tres` now retains the original 0/35/70% **initial build stages** and additionally authors two `TowerLevelArtwork` entries. Purchased Level 2 and Level 3 each use a **35% / 70%** intermediate composite SVG with scaffold/calibration lines. Finished upgrade swaps back to the split base/head for that level; the correct powered head continues to rotate. This uses the existing event-driven `TowerConstructionVisuals` presenter and existing construction drone/work clock—no scene duplication or separate upgrade script. Source implemented; owner visual review pending.

---

# Stealth Beacon — staged build SVGs (2026-11-05)

- **0%** `stage0_foundation.svg`: fixed octagonal platform and empty bearing socket.
- **35%** `stage1_frame.svg`: supporting framework for the directional scanner and temporary scaffold.
- **70%** `stage2_assembly.svg`: installed scanner housing with a dim, unfinished optic and temporary scaffold.
- **100%**: shared presenter restores the split finished `../stealth_beacon_base.svg` and `../stealth_beacon_head.svg`, with composite `../stealth_beacon.svg` remaining the original overlay fallback/catalog icon.

Original **1×1** footprint, **250 Credits**, **2-second** construction and **1800 HP** remain unchanged. No purchased upgrade art is authored because the Stealth Beacon currently has only one paid tier.

All external resources precede any subresource declarations and `load_steps` includes the new resource, following the corrected Resource Tower pattern. This artwork pass adds no reveal mechanic or rotating process. Runtime visual acceptance pending.
