# UI

Dedicated, C#-based interface area, following the organization of `ben0kay/isometric-new-2d/UI`.

- `UIMasterConfig.cs` — layout heights/widths, visual palette and tower tray animation settings.
- `DefaultUIConfig.tres` — Inspector-editable shared HUD settings.
- `GameplayHUD/` — active in-world HUD: permanent top strip, Ctrl-to-toggle build tray, bottom-right inspector.
- `Menus/` — reserved folders for Boot, Main and Pause menus; not implemented yet.
- `Shared/` — reserved for reusable UI styles/buttons and helpers when needed.

**UI uses the existing CanvasLayer in `Main.tscn`.** The root GameplayHUD ignores mouse events in unused screen space; its visible panels/buttons consume them to avoid interfering with camera/world interactions. No gameplay tower placement exists yet.
