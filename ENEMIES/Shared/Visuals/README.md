# Shared static enemy hull-damage artwork

This is a **cosmetic, opt-in, event-driven** feature for existing static `Sprite2D` enemies. No animation tree, extra enemy child node, recurring timer or per-frame traversal.

- `EnemyDamageAppearanceProfile.cs`: Inspector-editable resource containing `Stages`, an ordered `Godot.Collections.Array<Texture2D>` with index 0 as pristine and the final index as most damaged.
- `EnemyDamageAppearance.cs`: pure `StageForHealth(health, maxHealth, stageCount)` calculation. For `N` textures it divides the normalized **hull** health range into equal `1/N` bands, using a floor/clamp and returning `-1` for an empty profile. Three textures yield three equal thirds; four give quarters; arbitrary positive counts work without adding any code.
- `EnemyDefinition.DamageAppearance`: optional exported resource reference. The default is null. There are no special case enemy IDs or scene lookups.
- `Enemy.cs`: on initialization, caches the optional resource and existing `Sprite` node, resets its stage, and assigns the texture for current HP. On a successful damage commit it refreshes the art **only when hull HP actually changes** and only writes `Sprite.Texture` when the stage changes. Shield damage, shield restoration and movement do not cause visual updates. No profile means no extra scene node, profile scans or resource loads. The instance clears its references when freed.

**Authoring:** create a profile `.tres` for a type, fill `Stages` with same-sized right-facing artwork (healthy first), and set `Damage Appearance` on that type's `EnemyDefinition.tres`. Keep all textures registered to the same pivot/footprint; changing texture dimensions can affect defense-bar/shield visual framing. If no profile or its stage is empty, the existing scene sprite remains unchanged. Clearing the reference disables the feature. Use `ENEMIES/Types/Enemy001/Visuals/Enemy001DamageAppearance.tres` as the first example.

**Behaviour:** health values are clamped to 0–100% when selecting art, 100% is pristine, and a zero-HP enemy is assigned the last stage before its death/removal. There is currently no hull-healing system, but routing future healing through the same hull change path would update stages in either direction. Shield-only hits do not count as hull damage. Sprite tint (Slow/Freeze/Stealth) and any weapon effects remain owned by their existing systems.

**Scope:** only `Enemy001` is configured. This initial pass does not add particle sparks, debris nodes, new defence modifiers or animation; owner checks artwork readability in Godot at gameplay zoom. Source check only.
