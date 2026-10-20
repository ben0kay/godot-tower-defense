// Instance-only stealth capability; overlapping sources reveal without removing the authored capability.
using System;
using System.Collections.Generic;

public sealed class EnemyStealthState
{
    private readonly HashSet<object> _sources = new();
    public bool IsRevealed => _sources.Count > 0;
    public int RevealCount => _sources.Count;
    public event Action Changed;
    // =========================================================
    // Stable source identity makes repeated range events idempotent and overlapping beacons independent.
    public void AddReveal(object source)
    { if (source != null && _sources.Add(source) && _sources.Count == 1) Changed?.Invoke(); }
    // =========================================================
    // Only the last departing source restores effective stealth.
    public void RemoveReveal(object source)
    { if (source != null && _sources.Remove(source) && _sources.Count == 0) Changed?.Invoke(); }
    // =========================================================
    // Retired instance state releases even non-beacon handles; no presentation callback survives teardown.
    internal void Clear() => _sources.Clear();
}

