// Editable Level001 test encounters: currently Enemy001-only; other enemy types are registered.
using static WaveBuilder;

public static class Level001Waves
{
	// =========================================================
	// Edit wave composition/timing here rather than managing long Inspector arrays.
	public static WaveDefinition[] Create() =>
	[
		Wave(1,
			Batch(count: 10, interval: 0.1, after: 2, Enemy("basic_mk1")),
			Batch(count: 5, interval: 0.3, after: 5, Enemy("basic_mk1")),
			Batch(count: 20, interval: 0.2, after: 0, Enemy("basic_mk1"))),
		Wave(2,
			Batch(count: 20, interval: 0.15, after: 3, Enemy("basic_mk1")),
			Batch(count: 15, interval: 0.2, after: 0, Enemy("basic_mk1"))),
		Wave(3,
			Batch(count: 30, interval: 0.12, after: 2, Enemy("basic_mk1")),
			Batch(count: 20, interval: 0.15, after: 0, Enemy("basic_mk1")))
	];
}
