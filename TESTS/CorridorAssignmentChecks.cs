// N4 assignment checkpoint: reproducible probability boundaries without touching other RNG streams.
using Godot;
using System;
public static class CorridorAssignmentChecks
{
    // =========================================================
    // Test deterministic independent selection before any waypoint refinement is introduced.
    public static void Run()
    {
        EnemyMasterConfig config = new(); int selected = 0;
        for (uint i = 0; i < 10000; i++)
        {
            uint seed = CorridorPreference.Seed(i, 12345);
            Require(!CorridorPreference.Selected(seed, config), "Master-off must select none");
            config.EnableCorridorVariation = true; config.CorridorVariationPercent = 0;
            Require(!CorridorPreference.Selected(seed, config), "Zero percent must select none");
            config.CorridorVariationPercent = 100;
            Require(CorridorPreference.Selected(seed, config), "100 percent must select all");
            config.CorridorVariationPercent = 50;
            if (CorridorPreference.Selected(seed, config)) selected++;
            Require(seed == CorridorPreference.Seed(i, 12345), "Per-spawn ordinal seed is reproducible");
            config.EnableCorridorVariation = false;
        }
        Require(selected > 4700 && selected < 5300, "50 percent must approximately split a large deterministic population");
        config.CorridorVariationPercent = float.NaN; config.CorridorVariationStrength = float.PositiveInfinity; config.CorridorVariationSpacingCells = -1;
        Require(config.CorridorVariationPercent == 0 && config.CorridorVariationStrength == 0 && config.CorridorVariationSpacingCells == 2,
            "Malformed runtime controls clamp safely");
        var batch = WaveBuilder.Batch(20, 0, 0, WaveBuilder.Enemy("enemy001", weight: 80), WaveBuilder.Enemy("enemy002", weight: 20));
        RandomNumberGenerator before = new() { Seed = 123 }, after = new() { Seed = 123 };
        var expected = WaveCatalog.Compose(batch, before);
        for (uint i = 0; i < 100; i++) CorridorPreference.Selected(CorridorPreference.Seed(i, 12345), config);
        var actual = WaveCatalog.Compose(batch, after);
        for (int i = 0; i < actual.Length; i++) Require(actual[i] == expected[i], "N4 assignment must not perturb authored wave composition RNG");
        GD.Print($"PASS: N4 master/zero/all boundaries and deterministic50% selection ({selected}/10000), independent RNG stream");
    }
    // =========================================================
    // Stop on assignment/control contract regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
