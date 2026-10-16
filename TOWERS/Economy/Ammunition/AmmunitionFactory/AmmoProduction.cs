// Standalone batch-production state, deliberately not wired to a factory scene or the game loop yet.
using System;

public sealed class AmmoProduction
{
    #region Configuration and State
    public int BatchAmount { get; }
    public double SecondsPerBatch { get; }
    public AmmoInventory Buffer { get; }
    public double ProgressSeconds { get; private set; }
    public bool IsPaused => Buffer.FreeSpace < BatchAmount;
    #endregion

    // =========================================================
    // Values are supplied by a future factory config; a depot is never a production prerequisite.
    public AmmoProduction(int batchAmount, double secondsPerBatch, int bufferCapacity, int initialBufferedAmmo = 0)
    {
        if (batchAmount <= 0 || batchAmount > bufferCapacity) throw new ArgumentOutOfRangeException(nameof(batchAmount));
        if (!double.IsFinite(secondsPerBatch) || secondsPerBatch <= 0)
            throw new ArgumentOutOfRangeException(nameof(secondsPerBatch));
        BatchAmount = batchAmount; SecondsPerBatch = secondsPerBatch;
        Buffer = new AmmoInventory(bufferCapacity, initialBufferedAmmo);
    }

    // =========================================================
    // The future factory node calls Advance(delta); only THIS method manufactures rounds.
    // Finish whole batches, pause when another batch cannot fit and never bank paused time.
    // Returns newly produced ROUNDS (not number of batches).
    public int Advance(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (deltaSeconds == 0 || IsPaused) return 0;
        double total = ProgressSeconds + deltaSeconds;
        if (!double.IsFinite(total)) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        int availableSlots = Buffer.FreeSpace / BatchAmount;
        int batches = (int)Math.Min((double)availableSlots, Math.Floor(total / SecondsPerBatch));
        if (batches <= 0) { ProgressSeconds = total; return 0; }
        int produced = batches * BatchAmount; // <= Buffer.FreeSpace, so cannot overflow.
        if (!Buffer.TryAdd(produced)) throw new InvalidOperationException("Factory buffer changed during production.");
        ProgressSeconds = IsPaused ? 0 : Math.Max(0, total - batches * SecondsPerBatch);
        return produced;
    }
}
