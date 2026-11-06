# Status application feedback

**CTX1 Source Implemented / Awaiting Owner Validation.** `StatusEffectFeedback` maps a successful Corruption application to the existing cosmetic `ShockwaveEffect` and `CorruptionImpact.tres`: thin violet ring, 0.18s lifetime, bounded8–36px radius. No damage, status storage, timer/scheduler or permanent particle system lives here. `Enemy` composes the persistent purple tint with Slow/Freeze using existing sprite SelfModulate. Other identities retain their existing visuals. Godot visual acceptance pending.
