// Reusable producer storage: instance-local Credits, bounded accrual and atomic wallet collection.
using System;

public sealed class CreditBuffer
{
    #region Balance and Notifications
    public int Capacity { get; }
    public int Current { get; private set; }
    public bool IsFull => Current == Capacity;
    public event Action<int, int> Changed;
    private bool _collecting;
    #endregion

    // =========================================================
    // A producer owns its buffer; the session wallet remains the only spendable balance.
    public CreditBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    // =========================================================
    // Capped accumulation never writes directly to the wallet or produces extra HUD rewards.
    public int Produce(int amount)
    {
        if (amount <= 0) return 0;
        int added = Math.Min(amount, Capacity - Current);
        if (added == 0) return 0;
        Current += added; Changed?.Invoke(Current, Capacity);
        return added;
    }

    // =========================================================
    // Only a successful all-or-nothing wallet grant drains this one buffer, exactly once.
    public bool TryCollect(EconomyManager wallet, ResourceDefinition resource)
    {
        if (_collecting || Current == 0 || wallet == null || resource == null) return false;
        _collecting = true;
        try
        {
            int amount = Current;
            if (!wallet.Grant(resource, amount)) return false;
            Current -= amount;
            Changed?.Invoke(Current, Capacity);
            return true;
        }
        finally { _collecting = false; }
    }

    // =========================================================
    // Level/session resets clear only this producer's pending income.
    public void Reset()
    {
        if (_collecting) throw new InvalidOperationException("Cannot reset Credits while collecting.");
        if (Current == 0) return;
        Current = 0; Changed?.Invoke(Current, Capacity);
    }
}
