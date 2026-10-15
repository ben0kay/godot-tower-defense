# Lava

Paint Terrain/Interactive source1 atlas(0,0). Lava is passable and nonbuildable, including over ordinary buildable floor; structural walls and reservations remain authoritative. The source64px SVG under Tiles/Interactive/Lava is molten orange with sparse tiny highlights, without large black crust or per-cell particles.

LavaTileConfig is Inspector-editable through World/InteractiveTiles: DamagePerTick5, DamageIntervalSeconds0.5, DamageOnEntry=true, DamageType=Energy. Amount must be finite/nonnegative, interval finite/positive and type supported. Valid live field edits affect future hits and reschedule timers without another entry.

One manager tracks only exposed enemies. Enter/re-enter hits once, adjacent tiles share the timer, stationary enemies continue ticking, exit cancels exposure. Actual swept movement accounts for sub-frame exposure, normal gaps and fast crossings; direct teleports only affect the destination. Every owed periodic hit is delivered through Enemy.ApplyDamage with no tower source/XP; canonical defenses/death/Credits remain intact. Entry hazards can kill immediately, including at spawn, without stranding wave completion.

See ../README.md for timing, repaint, lifecycle, tests and deferred scope. No Thermal damage type or lingering burn was added.
