// Finite, per-owner ammunition quantity with atomic full-quantity transfers; no Godot node or wallet dependency.
using System;

public sealed class AmmoInventory
{
    #region State
    public int Capacity { get; private set; }
    public int Amount { get; private set; }
    public int FreeSpace => Capacity - Amount;
    public event Action Changed;
    #endregion

    // =========================================================
    // Reject malformed starting state rather than silently minting/clamping ammunition.
    public AmmoInventory(int capacity, int initialAmount = 0)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (initialAmount < 0 || initialAmount > capacity) throw new ArgumentOutOfRangeException(nameof(initialAmount));
        Capacity = capacity; Amount = initialAmount;
    }

    // =========================================================
    // Allow future level changes only if they can preserve every stored round.
    public bool TrySetCapacity(int capacity)
    {
        if (capacity < Amount || capacity < 0) return false;
        if (capacity == Capacity) return true;
        Capacity = capacity; Changed?.Invoke();
        return true;
    }

    // =========================================================
    // Add an entire amount or none; only producers/initialization should create new ammo.
    public bool TryAdd(int rounds)
    {
        if (rounds <= 0 || rounds > FreeSpace) return false;
        Amount += rounds; Changed?.Invoke();
        return true;
    }

    // =========================================================
    // Remove an entire amount or none; for a weapon this is one valid firing event.
    public bool TryConsume(int rounds)
    {
        if (rounds <= 0 || rounds > Amount) return false;
        Amount -= rounds; Changed?.Invoke();
        return true;
    }

    // =========================================================
    // Atomically move physical stock (source -> cargo -> destination) without duplication.
    // This does NOT deliver instantly in gameplay: Codex invokes it on drone load/unload.
    public bool TryTransferTo(AmmoInventory destination, int rounds)
    {
        if (destination == null || ReferenceEquals(this, destination) || rounds <= 0
            || rounds > Amount || rounds > destination.FreeSpace) return false;
        Amount -= rounds; destination.Amount += rounds;
        Changed?.Invoke(); destination.Changed?.Invoke();
        return true;
    }
}
