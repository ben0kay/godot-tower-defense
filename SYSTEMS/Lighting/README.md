# Native battlefield lighting

`Runtime/` owns the world controller, pooled lights, drone attachment and lava grouping. `Config/` holds typed settings and the default world resource. `Presets/` contains source profiles and `Textures/` holds the shared falloff.

Tune ambient, startup state, budgets, viewport culling and construction multipliers in `Config/DefaultLightingConfig.tres`. The default is `#4a5562` dusk with up to 24 active lights, including up to 8 drone lights. Visible Citadels and portals take priority, followed by one visible tower when available, the capped drone share, then remaining towers. F6 in a running debug battlefield compares lighting with original colors. The override resets on level entry. Reopen the level after editing resources.

Placed towers inherit a 576px cyan radius from `Presets/DefaultTowerLight.tres`. In a tower definition's **Lighting** group, zero radius inherits the default. Each definition can also adjust the offset or opt out. Construction uses half the resolved radius and 55% energy until completion. Citadel reactors keep their 320px radius and (64,64) local offset. Deployed drone bodies get a 62px following light, including builders hovering at worksites. Parked and docked drones release their slots. Lava uses cached connected patches inside 4×4-cell sections, centred on a real lava cell. Shadows are reserved for LGT0-B.

Nearby tower lamps of similar size share a capped glow. Neighbouring groups soften each other, and visible groups near the camera centre get priority if the pool fills. Reveal still uses each eligible tower's own footprint.

Disabled towers release their cyan light and reveal until they recover. The cyan passable foundation (source 1, atlas (3, 0), alternative 0) has a cell-contained cyan accent and each playable lava tile has a warm accent. Lava uses one merged additive coverage field with a uniform core and a soft halo around the outer silhouette. `Presets/LavaSpillLight.tres` controls tint, strength and reach (160px measured from a cell center, about 128px from a 64px cell edge). It uses no native light slots. The existing `LavaLight.tres` continues to define reveal. The **Lava Presentation** group in `DefaultLightingConfig.tres` tunes glow and steam, with up to four viewport-selected GPU steam emitters by default. F6 toggles the halo and steam along with lighting. The tile surface remains self-lit in both modes.

## Projectile lights

A.2 adds tiny lights for opt-in friendly and hostile rounds. The shared `Presets/ProjectileLight.tres` sets a 34px radius and 0.34 energy. Each round uses its tracer body or splash color. Enable `EmitsWorldLight` on a tracer style, or `ProjectileWorldLight` on Rocket/Blaster configs. `MaxProjectileLights` defaults to four spare slots within the world cap. With a cap above one, a single side can use up to the cap minus one. Already-flying rounds keep their normal visuals if they lose their slot. Set the category cap to zero to disable this pass.

## Light-based fog of war

Towers retain their local cyan base lights. In darkness, a subtle red radar contact replaces the enemy hull and health bar. The two fade across lamp edges. Citadels, forming/open portals, flying drones and lava also reveal entities. Ground towers can fire and guide rounds at dark contacts with normal range, stealth and terrain rules. Fighters can also target dark contacts. Enemy direct targeting still requires light. Projectile lights illuminate the floor without granting reveal.

Use `RevealEnabled` to opt out. Reopen the level after editing this setting. F6 disables the visible mask and contacts for comparison. `SetSourceSuppressed(owner, suppressed)` controls a registered light's blackout state without changing damage, navigation or overlapping sources. Native portal lights have priority after Citadels. Gameplay coverage is independent of the cosmetic light cap.

## Removal

Set the master config's `Enabled` to false and reopen the level for a configuration rollback. Revert A.3 independently to remove gameplay reveal and its shader bindings. Revert A.2 independently to remove projectile lights. Its integration points are labeled in the shared friendly projectile classes, hostile bullet system and splash tower configs. Revert A.1 to return to the original pilot. For selective removal, detach `Lighting` and its resources from `WORLD/World.tscn`, remove `FlightLight` and its script reference from `DRONES/Shared/DroneBody.tscn`, and remove the labeled lighting integrations in `DroneBody.cs`, `TowerDefinition.cs`, `InteractiveTileManager.cs` and `RoutePreviewOverlay.cs`. The latter keeps the F3 route colors independent. Lighting scripts and profiles live in this folder.

Dark radar contacts share one SVG texture. The unlit overlay draws one quad per on-screen contact and preserves the light-edge opacity fade. Camera changes refresh culling even for stationary contacts.
