# Enemy001 damage-art trial

`Enemy001DamageAppearance.tres` is the **only enabled damage-artwork profile** in the initial pass. It references three existing-format 32×24 static SVG textures in `../Artwork/`:

1. `enemy001.svg` — original intact forked strike drone
2. `enemy001_damaged.svg` — scraped/broken armour and visible exposed machinery
3. `enemy001_critical.svg` — torn stabilizers, missing panels and more obvious internal damage

The shared selector automatically divides the enemy's hull health into equal bands. For these three assets that means full art above two-thirds health, moderate damage until one-third, then critical art below one-third. Additional textures make four/five/etc equally-sized stages without changing code or scene structure.

**Disable:** in `../Enemy001.tres`, clear `Damage Appearance`. No other enemy type is affected. The profile changes texture only: sprite size, collision, health, shields, movement, loot/rewards, rank XP and stealth/status tint follow their normal logic. Check the art after sustained small hull hits; a shielded spawn must first lose its shield before changing body texture.
