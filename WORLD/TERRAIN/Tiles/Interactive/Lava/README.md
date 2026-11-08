# Lava artwork

`lava.svg` supplies the original 64px lava artwork. Only the lava entry in `InteractiveTerrain.tres` uses `LavaSurface.tres`. Its shader blends the original artwork with world-space flow and intermittent bubbles so neighbouring cells share the same motion field. Tune **Enabled**, **Flow Speed**, **Flow Strength**, **Brightness Variation** and **Bubble Density / Scale / Intensity** in that ShaderMaterial. Disabling it restores the original texture.

Regional light and sparse GPU steam settings live in the [lighting configuration](../../../../../SYSTEMS/Lighting/README.md). Damage is configured separately through `LavaTileConfig`.
