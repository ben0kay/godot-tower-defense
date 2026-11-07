# Lighting — VE5 planned

The isolated VE5C Cinematic Blue bloom experiment was removed by request. No bloom / HDR 2D integration is enabled; retain original VE lighting proposals without committing to bloom. VE2 energy structure glows are localized transparent shader overlays, not screen-space bloom.

## New LGT0 real lighting handoff — 2026-11-06

Primary controller, configs, presets and proposed delivery belong under [`SYSTEMS/Lighting/`](../../../SYSTEMS/Lighting/IMPLEMENTATION_PLAN.md). This plan uses native Godot CanvasModulate/PointLight2D and a debug Shift+F3 ON/OFF comparison, with shadows/bloom disabled. Existing WORLD/VISUALS shader glows remain cosmetic. **No lighting gameplay code is delivered by this plan.**
