# Gunner Drone artwork

Two independent SVGs, using the game's existing right-facing hull/up-facing gun conventions. gunner_drone.svg is64×48, matching medium Enemy004's canvas with a unique angular hull. gunner_minigun.svg is24×40; its origin is the canvas center(12,20), barrel tip(12,2), matching muzzle(0,-18). Hull and gun are independently replaceable; preserve this registration or update the scene marker/pivot together. Socket sits at hull-local(-2,0). No per-frame SVG generation or extra enemy-specific damage code exists. Godot import and desktop visual acceptance remain pending.
