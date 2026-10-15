# Solar Tower (Planned Hybrid)

Begins as an **economy tower** that generates energy, inspired by the older Solar Panel structure.

**Potential upgrade:** gain a weak defensive laser while continuing to produce energy. Remains in `Economy/` because that is its primary purpose. The later C# design should make attacks optional, not a mandatory feature of every placeable tower.

Initial prototype can use one SVG, with laser fire from the center if/when the upgrade is implemented. No implementation yet.

## Dedicated Solar Generator versus older hybrid idea

The [SolarGenerator](../SolarGenerator/README.md) folder is the planned **pure, non-offensive small power source** of the Solar / Industrial / Fusion set. This older SolarTower concept retains its potential defensive-laser hybrid identity, but it is not a second implementation of base solar generation. Decide if it is a separate tower or a future hybrid upgrade before registration. See [POWER](../../../POWER/README.md). Both remain unimplemented.
