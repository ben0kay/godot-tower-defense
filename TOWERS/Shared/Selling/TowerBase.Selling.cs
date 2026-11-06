// Per-instance actual paid Credits and a one-way sale latch, shared by every tower category.
using System;

public abstract partial class TowerBase
{
    public long? CreditsInvested { get; private set; } = 0;
    internal bool SaleCommitted { get; set; }

    // =========================================================
    // Called inside the successful wallet transaction before its public notifications.
    internal void RecordCreditsPaid(int credits)
    {
        if (credits < 0) throw new ArgumentOutOfRangeException(nameof(credits));
        if (CreditsInvested is long total) CreditsInvested = total > long.MaxValue - credits ? long.MaxValue : total + credits;
    }
}
