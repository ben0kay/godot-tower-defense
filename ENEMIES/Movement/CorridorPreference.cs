// Pure deterministic per-spawn assignment/preferences; no wave RNG, Godot RNG or A* state.
using Godot;
public static class CorridorPreference
{
    // =========================================================
    // Separate world-local spawn ordinal stream, keyed by the existing route seed.
    public static uint Seed(uint ordinal, int routeSeed) => Hash(ordinal ^ unchecked((uint)routeSeed) ^ 0xa511e9b3u);
    // =========================================================
    // Independent probability, including exact off/zero/all boundary cases.
    public static bool Selected(uint seed, EnemyMasterConfig config) => config.EnableCorridorVariation &&
        config.CorridorVariationPercent > 0 && (config.CorridorVariationPercent >= 100 ||
            Hash(seed ^ 0x63d83595u) / 4294967296.0 < config.CorridorVariationPercent / 100.0);
    // =========================================================
    // Integer mixing is stable across game runs, without consuming any shared random stream.
    public static uint Hash(uint value)
    { unchecked { value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; return value ^ (value >> 16); } }
}
