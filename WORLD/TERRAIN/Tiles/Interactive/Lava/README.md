# Lava artwork

`lava.svg` supplies the original 64px lava artwork. Only the lava entry in `InteractiveTerrain.tres` uses `LavaSurface.tres`. Its shader uses the original texture as a mask for self-lit world-space flow and intermittent bubble pops so neighbouring cells share the same motion field. Tune **Enabled**, **Flow Speed**, **Flow Strength**, **Brightness Variation** and **Bubble Density / Scale / Intensity** in that ShaderMaterial. Disabling it restores the original texture.

Merged edge glow and sparse GPU steam settings live in the [lighting configuration](../../../../../SYSTEMS/Lighting/README.md). Damage is configured separately through `LavaTileConfig`.
