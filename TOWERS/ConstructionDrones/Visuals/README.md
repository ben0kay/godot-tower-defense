# Construction drone visuals

`ConstructionDrone.svg` is the shared imported36×28 dark/cyan builder, separate from ammunition courier artwork. The scene reuses one texture; no runtime artwork generation or loading occurs during flight. Its nozzle is at local(0,7).

`ConstructionBeam.cs` draws the footprint-sized cyan hologram from the drone nozzle toward the site's fixed world center. A retained four-vertex polygon/color array and thin lattice/edge lines avoid particles, shaders, per-frame arrays and input/collision nodes. The manager supplies pose/deployment strength, and hides the beam immediately on work completion/cancellation. No construction time or combat logic lives here. Desktop appearance/GPU review remains pending; see ../README.md.
