## Alien robotic fleet artwork — 2026-10-18

Replaced the seven existing enemy SVG hulls with original vector interpretations of the supplied dark alien-spacecraft references. Emphasis is on silhouette and exposed machinery rather than repeating one arrow shape with different colors: forked strike drone, tiny hooked interceptor, broad tusked assault cruiser, large four-lance warship, curved talon-wing Elite, bulbous armored Transporter and rounded split-fork Shield Generator. Dark layered metal, restrained edge highlights and recessed reactors keep them readable against space. Reference screenshot borders/backgrounds are not part of the artwork; uploaded bitmaps are not embedded.

All SVG file paths, native width/height/viewBox and right-facing (+X) convention are retained. No scene transforms, definitions, IDs, collision/navigation radii, health, shields, movement, rewards, abilities, waves or shared shield/status visuals changed. The Shield Generator's circular center is physical reactor/projector machinery, not a baked shield aura. Small hulls remain small; the Transporter retains its existing 112×72 canvas and Big mk.1 its 112×120 canvas. Gradients and layered plates are rasterized at asset import; no blur filters, shaders, extra nodes or per-frame effect processing were added.

Validation: all seven assets parsed and rasterized successfully with Godot 4.7.2's native SVG renderer at their exact original dimensions; reviewed at enlarged and native-relative scale. No C# changes, so no unrelated gameplay suite/build was needed. Desktop in-game style/readability is still subjective and can be tuned after review. The review sheet enlarges each hull independently and lists native dimensions; it is documentation, not a gameplay texture reference. See `ENEMIES/Types/ARTWORK_PREVIEW.png` and `ENEMIES/Types/ART_DIRECTION.md`. This visual update supersedes earlier placeholder/color descriptions while retaining their historical gameplay notes.

![Fleet artwork review](ARTWORK_PREVIEW.png)

---

# Enemy types

**Elite mk.1:** `Enemy006/` adds an 80×60 fast, heavily armored enemy (`elite_mk1`) with 1,000 HP, 250 optional shields, 100px/s and 10 kill Credits. It inherits `GenericEnemy.tscn` and is in the default catalog; existing waves are unchanged. See [Enemy006/README.md](Enemy006/README.md).

**Heavy Big mk.1:** `Enemy005/` is a 112×120 plated heavyweight ship with ID `big_mk1`, 1,000 HP, 200 optional shields, 80 px/s speed, 5 Citadel damage and 10 Credits. Inherits `Shared/GenericEnemy.tscn` with no unique scripts or abilities. See [Enemy005](Enemy005/README.md). Its shields remain wave-opt-in, not permanently enabled.


`Enemy001/` is the first reusable basic test ship. Enemy002 and later variants can reuse Shared/Enemy.cs and Movement/EnemyMovement.cs, changing scenes, artwork and EnemyDefinition resources instead of duplicating scripts.

**New medium hull:** `Enemy004/Enemy004.tscn` is the larger 64×48px `medium_mk1` / **Bulwark Drone** with 160 HP, 75 speed and a 22px clearance radius; see [Enemy004/README.md](Enemy004/README.md). `Enemy003` is intentionally not required for this asset and its absence does not dictate wave order.

Level-specific composition and future waves belong elsewhere. **Naming contract:** keep numbered development assets such as `Enemy001.tscn` and `Enemy002.tscn`. Set each `.tres`'s **unique stable `EnemyId`** for gameplay/wave lookup (`basic_mk1`, `scout_mk1`, `shield_emitter_mk1`), and its **`DisplayName`** for players (`Standard Drone`, `Swift Interceptor`, `Shield Generator`). Neither ID nor filename dictates wave appearance/order. New `EnemyDefinition` instances intentionally start with an empty ID; explicitly author a unique ID before registering a scene. Do not rename IDs lightly once saved levels refer to them.
