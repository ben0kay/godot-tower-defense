# Tower Progress Popup

Shared cosmetic popup created lazily by `TowerBase` when a paid tower upgrade **finishes** or earned XP causes an actual rank promotion.

- Paid level completion: **aqua**, thick filled up-arrow, `LEVEL N`, to the right of the tower.
- Earned rank promotion: **gold**, same arrow, `RANK UP  VETERAN` (or the actual rank name).
- Floats upward 20 pixels over 1.35 seconds and fades near the end. No effects on gameplay stats, purchases, XP or construction timing.
- A tower owns at most one `TowerProgressPopup` node. It is allocated on first use, reuses the same node on later events and disables processing while hidden.
- The popup uses `CanvasItem.TopLevel` with a world-space right-hand origin, so selected-tower tint and weapon/tower rotation cannot recolour or rotate the text. Towers are stationary after placement; the popup is still freed with its owning tower.
- The arrow points are reused rather than rebuilt as new arrays each draw frame. Rendering uses `DrawSetTransform` for the short upward motion.
- Initial construction completion deliberately does **not** trigger a level-up. Paid upgrades with work time show the effect when work completes, not when purchased. Instant paid upgrades show it after successful payment.
- Multiple promotions in one XP award show the final attained rank. Simultaneous rank and level events on the same tower reuse the popup; the latest event wins.

**Review status (2026-11-03):** Checked the rank promotion, instant paid upgrade, timed paid completion and save restore paths in source. Removed temporary geometry allocations and isolated the popup from tower selection tint. Godot draw/font API signatures were cross-checked with Godot documentation. **QA pending:** Godot/.NET build and in-game visual verification (zoom, overlap, text size, colours and nearby towers). No runtime tests were run in this connector session.
