# World Callouts — shared visual feedback

**Source implemented on `dev`; owner visual QA pending.** `WorldCallout.cs` is a reusable floating world-space message for any gameplay owner, independent of towers, XP and heat. The caller supplies world position, text, colour and an optional `WorldCalloutSymbol.ThickUpArrow`; the default is plain text with no arrow.

TowerBase lazily creates one reusable child only on the first event. `ShowWorldCallout(message, tint, symbol)` anchors it to the right of the placed tower. On completed **paid** upgrade, it displays aqua `LEVEL 2` (or actual level), with a thick upward arrow. On actual earned rank promotion, it displays gold `RANK UP  Veteran` (or the attained rank), with the same arrow. Ordinary XP, initial build completion and checkpoint restoration never display false promotions. Timed upgrade completion integrates with `dev`'s current drone/disable/work flow, without changing that flow.

**Example future integration (not enabled in this pass):**
```csharp
tower.ShowWorldCallout("OVERHEATED", new Color("#ff864d"));
```
Other entities can create and reuse their own `WorldCallout` and call `Play(position, message, tint, symbol)`. The last event replaces the current display; concurrent queueing is intentionally not implemented.

**Inspector tuning:** `DefaultWorldCalloutStyle.tres` has duration 1.35s, fade 0.4s, upward rise 20px and font size 13. `FEEDBACK/DefaultFeedbackConfig.tres` has `WorldCalloutsEnabled` (on by default). Caller chooses colour and icon. TopLevel keeps the text horizontal and avoids parent selection tint. No processing or redraw when idle; static polygon vertices are reused; owner removal frees its callout. No gameplay stat or cost effects.

**Checks remaining:** Godot/.NET compile, in-game text/arrow placement at camera zoom, adjacent towers, pause/resume and selection tint. Source review only; no Godot gameplay validation was run.
