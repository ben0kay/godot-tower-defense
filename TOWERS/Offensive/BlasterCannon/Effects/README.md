# Blaster impact presentation

BlasterShockwaveStyle.tres supplies the shared FEEDBACK/Shockwave component with orange plasma colours, ring/glow, .22-second expansion,55% initial radius and a short fade. Optional textures go in Artwork and can be assigned on the style. SplashProjectile snapshots the style at launch along with the immutable combat payload and uses final blast radius for the visual's maximum size.

The visual expands only after instantaneous AoE damage is finished. Changing its appearance or lifetime never delays, reapplies or enlarges combat damage. Each placed config, fired projectile and spawned effect isolates mutable style data.
