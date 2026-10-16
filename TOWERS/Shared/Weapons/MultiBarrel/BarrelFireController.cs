// Compact, allocation-free selector for one, sequential or simultaneous muzzle fire; no combat/target ownership.
using System;

public enum BarrelFireMode { Single, Sequential, Simultaneous }

public sealed class BarrelFireController
{
    public int MuzzleCount { get; }
    public int NextMuzzle { get; private set; }

    // =========================================================
    // Each turret owns one selector; no extra Godot nodes or per-barrel physics loops.
    public BarrelFireController(int muzzleCount)
    {
        if (muzzleCount < 1) throw new ArgumentOutOfRangeException(nameof(muzzleCount));
        MuzzleCount = muzzleCount;
    }

    // =========================================================
    // A volley is one bullet in Single/Sequential, or all muzzles together in Simultaneous.
    public int ShotsPerVolley(BarrelFireMode mode) => mode == BarrelFireMode.Simultaneous ? MuzzleCount : 1;

    // =========================================================
    // Return a stable index; call Commit only after a successfully funded launch.
    public int MuzzleIndex(BarrelFireMode mode, int volleyIndex)
    {
        if (!Enum.IsDefined(mode) || volleyIndex < 0 || volleyIndex >= ShotsPerVolley(mode))
            throw new ArgumentOutOfRangeException(nameof(volleyIndex));
        return mode switch
        {
            BarrelFireMode.Sequential => NextMuzzle,
            BarrelFireMode.Simultaneous => volleyIndex,
            _ => 0
        };
    }

    // =========================================================
    // Failed ammo/target/alignment attempts never advance the alternating sequence.
    public void Commit(BarrelFireMode mode)
    {
        if (mode == BarrelFireMode.Sequential) NextMuzzle = (NextMuzzle + 1) % MuzzleCount;
    }
}
