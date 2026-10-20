# Stealth presentation

DefaultStealthVisualStyle.tres controls HiddenAlpha=0.30, RevealedAlpha=1.0 and DimDefenseFeedback=true. The ship remains visible to the player even though ordinary towers cannot target it. By default shield aura and defense meters receive the same opacity factor, preventing bright full-opacity feedback from defeating the visual treatment. Disable DimDefenseFeedback to keep those indicators normal.

StealthVisual is a lazy plain C# helper, not a Node or per-frame updater. It captures original Modulate values and multiplies alpha only on reveal transitions. Enemy Slow/Freeze continue using Sprite.SelfModulate, so Cryo/Stasis RGB and expiry compose with stealth rather than overwrite it. Root Modulate/rotation and DefenseBar alignment are unchanged. Shield auras added after spawn inherit the current opacity, while their own child hit/break animation still operates. Reinitialization/unload restores captured colors and clears references.

These are appearance settings only. Changing alpha does not change detection, health, shield math, navigation or reveal range. No shader/particle/animation system is added. Desktop visual/readability tuning is still advisable at different zoom levels.
