// Level002: four mixed test waves for a sprawling five-portal maze.
using static WaveBuilder;

public static class Level002Waves
{
    // =========================================================
    // All portals are eligible for each unpinned spawn; customize counts/timing freely.
    public static WaveDefinition[] Create() =>
    [
        Wave(1,
            Batch(count: 20, interval: 0.24, after: 3, Enemy("basic_mk1")),
            Batch(count: 14, interval: 0.32, after: 0, Enemy("basic_mk1", 75), Enemy("scout_mk1", 25))),
        Wave(2,
            Batch(count: 28, interval: 0.22, after: 4, Enemy("basic_mk1", 65), Enemy("scout_mk1", 35)),
            Batch(count: 9, interval: 0.50, after: 0, Enemy("medium_mk1"))),
        Wave(3,
            Batch(count: 40, interval: 0.18, after: 3, Enemy("basic_mk1", 55), Enemy("scout_mk1", 30), Enemy("medium_mk1", 15)),
            Batch(count: 10, interval: 0.55, after: 0, Enemy("medium_mk1", 85), Enemy("shield_emitter_mk1", 15))),
        Wave(4,
            Batch(count: 50, interval: 0.16, after: 3, Enemy("basic_mk1", 40), Enemy("scout_mk1", 40), Enemy("medium_mk1", 20)),
            Batch(count: 14, interval: 0.48, after: 0, Enemy("medium_mk1", 75), Enemy("shield_emitter_mk1", 25)))
    ];
}
