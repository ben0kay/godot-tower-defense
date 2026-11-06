# Drone Station SVG artwork — 2 cells wide × 3 cells long

All station artwork lives here, native **128×192** pixels, and must remain centered (sprite local origin 64,96) for the `Vector2i(2,3)` `TowerDefinition.Footprint`.

- `drone_station.svg`: completed twin-bay graphite/steel top-down hangar with two landing/dock illustrations, central guide channel, cyan illuminated rails, orange/amber safety marks and protected maintenance housing.
- `stage0_foundation.svg`: heavy base pad, dimensional seams, conduits and blueprint landing outlines at 0% of construction.
- `stage1_frame.svg`: exposed metal gantry, hangar bay outlines and partly assembled maintenance modules from 35%.
- `stage2_assembly.svg`: fitted bay canopies and rail systems with incomplete electronics from 70%.
- `DroneStationConstructionArt.tres`: existing shared `TowerConstructionArt` for the three staged texture swaps; the completed SVG returns at 100%.

No SVG fonts, filters, external images, animation script or active GPU particles are needed. Keep all stages the same native size so placement ghost, bounds and hologram remain in one location. Colors follow existing friendly cyan drone/glow identity; they should not resemble the hostile fleet's red weapon lighting.

Functional dock markers are authored in the station scene; the pad silhouettes remain flat artwork. Preserve marker positions for checkpoint compatibility.
