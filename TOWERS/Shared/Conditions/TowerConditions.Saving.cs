// Save disable identities as durable keys; runtime handles are rebuilt for this new tower instance.
using System;
using System.Collections.Generic;
using System.IO;

public partial class TowerConditions
{
    // =========================================================
    // Unknown sustained sources cannot be silently dropped. Timed detached sources retain a distinct durable key.
    internal List<DisableSaveData> CaptureCheckpoint(Func<ulong, string> sourceKey)
    {
        var saved = new List<DisableSaveData>();
        foreach (var entry in _entries)
        {
            string key = entry.Source == 0 ? "" : sourceKey(entry.Source);
            if (key == null && !entry.Remaining.HasValue) throw new InvalidOperationException("A sustained disable source cannot be saved.");
            saved.Add(new(entry.Reason, entry.Remaining, key ?? "detached:" + entry.Source));
        }
        return saved;
    }

    // =========================================================
    // Preserve the overheat latch even below full capacity, until the normal restart threshold is reached.
    internal void RestoreCheckpoint(float heat, bool overheated, List<DisableSaveData> entries, Func<string, ulong> sourceId)
    {
        if (heat > (_stats?.Capacity ?? 0)) throw new InvalidDataException("Saved heat exceeds configured capacity.");
        _entries.Clear(); _overheat = default; Heat = heat;
        foreach (var entry in entries)
        {
            if (entry.Reason == TowerDisableReason.Upgrading) throw new InvalidDataException("Upgrade disabling is derived from saved work, not a stored condition.");
            long token = ++_next;
            _entries.Add(new() { Token = token, Reason = entry.Reason, Remaining = entry.Remaining, Source = sourceId(entry.Source) });
            if (entry.Reason == TowerDisableReason.Overheated && overheated) _overheat = new(_owner.GetInstanceId(), token);
        }
        Wake(); _owner.PublishDisableChanged(); _owner.PublishHeatChanged();
    }
}
