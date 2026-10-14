// Editable finite test encounters: only Enemy001 exists in this first wave pass.
using static WaveBuilder;

public static class Level001Waves
{
	// =========================================================
	// Edit wave composition/timing here rather than managing long Inspector arrays.
	public static WaveDefinition[] Create() =>
	[
		Wave(1,
			Batch(count: 10, interval: 0.1, after: 2, Enemy("enemy001")),
			Batch(count: 5, interval: 0.3, after: 5, Enemy("enemy001")),
			Batch(count: 20, interval: 0.2, after: 0, Enemy("enemy001"))),
		Wave(2,
			Batch(count: 20, interval: 0.15, after: 3, Enemy("enemy001")),
			Batch(count: 15, interval: 0.2, after: 0, Enemy("enemy001"))),
		Wave(3,
			Batch(count: 30, interval: 0.12, after: 2, Enemy("enemy001")),
			Batch(count: 20, interval: 0.15, after: 0, Enemy("enemy001")))
	];
}
