// Level001 playtest encounters: escalating standard, shielded, fast and tougher enemies.
// Shields are explicit per entry; Enemy001's 20-point capacity does not affect ordinary spawns.
using static WaveBuilder;

public static class Level001Waves
{
	// =========================================================
	// Three modest escalating waves; no Shield Generator here (no support shield restoration).
	public static WaveDefinition[] Create() =>
	[
		// Warm-up baseline, then a few shielded Standard Drones to verify the red aura.
		Wave(1,
			Batch(count: 10, interval: 0.1, after: 2, Enemy("basic_mk1")),
			Batch(count: 8, interval: 0.3, after: 4,
				Enemy("basic_mk1", weight: 75),
				Enemy("basic_mk1", weight: 25, shield: true)),
			Batch(count: 20, interval: 0.2, after: 0,
				Enemy("basic_mk1", weight: 75),
				Enemy("basic_mk1", weight: 25, shield: true))),

		// More pressure, a few fast Scouts, plus shielded Standard Drones and Scouts.
		Wave(2,
			Batch(count: 20, interval: 0.17, after: 3,
				Enemy("basic_mk1", weight: 60),
				Enemy("basic_mk1", weight: 25, shield: true),
				Enemy("scout_mk1", weight: 15)),
			Batch(count: 20, interval: 0.19, after: 0,
				Enemy("basic_mk1", weight: 50),
				Enemy("basic_mk1", weight: 25, shield: true),
				Enemy("scout_mk1", weight: 15),
				Enemy("scout_mk1", weight: 10, shield: true))),

		// Mixed finale to test varied targeting and damage types; a few slow Bulwarks.
		Wave(3,
			Batch(count: 30, interval: 0.14, after: 2,
				Enemy("basic_mk1", weight: 50),
				Enemy("basic_mk1", weight: 25, shield: true),
				Enemy("scout_mk1", weight: 25)),
			Batch(count: 25, interval: 0.18, after: 0,
				Enemy("basic_mk1", weight: 45),
				Enemy("basic_mk1", weight: 25, shield: true),
				Enemy("scout_mk1", weight: 15, shield: true),
				Enemy("medium_mk1", weight: 10),
				Enemy("scout_mk1", weight: 5)))
	];
}
