// Optional resolution snapshots for debugging; normal combat does not allocate or collect traces.
using System.Collections.Generic;

public sealed class TowerStatTrace
{
    private readonly List<(string Stage, TowerStatBlock Stats)> _steps = new(8);
    public IReadOnlyList<(string Stage, TowerStatBlock Stats)> Steps => _steps.AsReadOnly();

    // =========================================================
    // Reuse an explicitly supplied trace rather than accumulating old resolution runs.
    internal void Clear() => _steps.Clear();

    // =========================================================
    // Record immutable values after each stage only when tracing has been requested.
    internal void Add(string stage, TowerStatBlock stats) => _steps.Add((stage, stats));
}
