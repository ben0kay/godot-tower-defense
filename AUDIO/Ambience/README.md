# Ambience

Global/non-positional space atmosphere belongs here (for example a quiet background bed). Use `Category = Ambience` and a Loop definition. Localized portal, Citadel, enemy or tower ambience lives under the **owning feature's** `Sounds/` folder and can use a positional Loop or Intermittent definition.

The central audio manager handles distance, offscreen rules, loop budgets, fades and stale owner cleanup.

## Atmospheric Space Background 1

The shared battlefield starts this non-positional loop on load and releases it on exit. The existing Ambience bus, pause setting and fade handling apply. Adjust `VolumeDb` on `AtmosphericSpaceBackground1.tres` to tune the level mix.

The original MP3 is preserved beside the runtime Ogg. The loop uses 3.4–9.8 seconds of the source with a 0.8-second overlap, yielding a 5.6-second bed. Godot's Ogg import enables native looping.
