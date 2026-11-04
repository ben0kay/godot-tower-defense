// Level002 tutorial: dual-portal mission introducing foundations/path blocking.
// Only Standard Drones and Swift Interceptors are used here; no Bulwarks yet.
using Godot;
using static WaveBuilder;

[GlobalClass]
public partial class Level002Waves : WaveSetSource
{
	// =========================================================
	// Assign this resource to LevelConfig without adding a wave-catalog switch entry.
	public override WaveDefinition[] Build() => Create();

	#region Tutorial Encounters
	// =========================================================
	// Manual wave starts leave unlimited building time; both authored portals remain eligible.
	public static WaveDefinition[] Create() =>
	[
		// Wave 1: first mixed wave, still fairly gentle.
		Wave(1,
			Batch(count: 14, interval: 0.8, after: 0,
				Enemy("basic_mk1", weight: 65),
				Enemy("scout_mk1", weight: 35))),

		// Wave 2: pure speed test.
		Wave(2,
			Batch(count: 14, interval: 0.7, after: 0,
				Enemy("scout_mk1"))),

		// Wave 3: mixed again, but noticeably more compact.
		Wave(3,
			Batch(count: 18, interval: 0.55, after: 0,
				Enemy("basic_mk1", weight: 55),
				Enemy("scout_mk1", weight: 45))),

		// Wave 4: flurry structure.
		// 1) quick burst of Standards
		// 2) pause
		// 3) quick burst of Scouts
		// 4) pause
		// 5) mixed finishing burst
		Wave(4,
			Batch(count: 8, interval: 0.35, after: 1.75,
				Enemy("basic_mk1")),
			Batch(count: 8, interval: 0.30, after: 1.75,
				Enemy("scout_mk1")),
			Batch(count: 10, interval: 0.40, after: 0,
				Enemy("basic_mk1", weight: 50),
				Enemy("scout_mk1", weight: 50)))
	];
	#endregion
}
