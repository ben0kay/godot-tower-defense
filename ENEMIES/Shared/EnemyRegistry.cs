// Authoritative per-world living membership and shared spatial circle/swept enemy queries.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyRegistry : Node
{
    [ExportGroup("Spatial Queries")]
    [Export(PropertyHint.Range, "16,512,16")] public float SpatialCellSize { get; set; } = 128;
    private EnemySpatialIndex _spatial;
    private EnemySpatialIndex Spatial => _spatial ??= new(SpatialCellSize);
    private readonly Stack<List<EnemySpatialIndex.Neighbor>> _queryBuffers = new();
    public int LastQueryCandidates => Spatial.CandidatesExamined;
    private readonly List<Enemy> _living = new();
    private readonly HashSet<Enemy> _members = new();
    public IReadOnlyList<Enemy> Living => _living;
    public int KilledCount { get; private set; }
    public int ArrivedCount { get; private set; }
    public event Action<Enemy> EnemyRegistered;
    public event Action<Enemy> EnemyPositionChanged;
    public event Action<Enemy> EnemyKilled;
    public event Action<Enemy, DamageSource> AttributedKill;
    public event Action<Enemy> EnemyUnregistered;
    public event Action<Enemy> EnemyArrived;

    // =========================================================
    // Register once; enemies unregister immediately on death or arrival.
    public void Register(Enemy enemy) { if (_members.Add(enemy)) { _living.Add(enemy); Spatial.Add(enemy); EnemyRegistered?.Invoke(enemy); } }

    // =========================================================
    // Exit-tree cleanup is idempotent and does not count an outcome twice.
    public void Unregister(Enemy enemy) { if (_members.Remove(enemy)) { _living.Remove(enemy); Spatial.Remove(enemy); EnemyUnregistered?.Invoke(enemy); } }

    // =========================================================
    // Snapshot-based attacks recheck removal in constant time without rescanning the living list.
    public bool Contains(Enemy enemy) => _members.Contains(enemy);

    // =========================================================
    // GameMaker-style circle list: caller-owned storage, exact inclusive center distance, no result cap.
    public void GetEnemiesInRadius(Vector2 center, float radius, List<Enemy> results) => Spatial.Circle(center, radius, results);
    // =========================================================
    // Visit nearby candidates directly when the caller needs only a best result, not a snapshot list.
    internal void VisitEnemiesInRadius<T>(Vector2 center, float radius, ref T visitor) where T : struct, EnemySpatialIndex.INeighborVisitor
        => Spatial.VisitNeighbors(center, radius, ref visitor);
    // =========================================================
    // Snapshot data for combat queries; no additional native transform reads or hash lookups per candidate.
    internal void GetNeighborsInRadius(Vector2 center, float radius, List<EnemySpatialIndex.Neighbor> results) => Spatial.Neighbors(center, radius, results);
    // =========================================================
    // Transform notifications also cover explicit teleports and editor/debug repositioning.
    public void UpdatePosition(Enemy enemy)
    { _spatial?.Move(enemy, enemy.GlobalPosition); if (EnemyPositionChanged != null && _members.Contains(enemy)) EnemyPositionChanged.Invoke(enemy); }
    // =========================================================
    // Shared pooled buffers remain safe when a damage callback starts another query.
    internal List<EnemySpatialIndex.Neighbor> RentQueryBuffer() => _queryBuffers.Count > 0 ? _queryBuffers.Pop() : new();
    // =========================================================
    // Return only after dispatch so nested attacks cannot overwrite an outer snapshot.
    internal void ReturnQueryBuffer(List<EnemySpatialIndex.Neighbor> buffer) { buffer.Clear(); _queryBuffers.Push(buffer); }
    // =========================================================
    // Wave cursors retain only motion needed by active independent emissions; initialization is O(1).
    internal ulong BeginWaveQuery() => Spatial.BeginSweep();
    // =========================================================
    // Swept broad phase returns samples; only the weapon's existing solver decides actual contact.
    internal void GetWaveCandidates(ulong cursor, Vector2 center, float from, float to, List<EnemySpatialIndex.Sweep> results)
        => Spatial.Swept(cursor, center, from, to, results);
    // =========================================================
    // Completion/unload releases retained history, without touching other wave cursors.
    internal void EndWaveQuery(ulong cursor) => _spatial?.EndSweep(cursor);

    // =========================================================
    // World unload releases index history, membership and reusable query storage together.
    public override void _ExitTree() { _living.Clear(); _members.Clear(); _queryBuffers.Clear(); _spatial = null; EnemyUnregistered = null; EnemyRegistered = null; EnemyPositionChanged = null; AttributedKill = null; }

    // =========================================================
    // Emit kill-only notifications after an enemy leaves the living registry.
    // Enemy.Finish is guarded against repeated damage, so each ship pays at most once.
    public void RecordOutcome(bool arrived, Enemy enemy)
    {
        if (arrived) { ArrivedCount++; EnemyArrived?.Invoke(enemy); return; }
        KilledCount++;
        AttributedKill?.Invoke(enemy, enemy.KillingSource); // Before wave completion: the final kill remains eligible.
        EnemyKilled?.Invoke(enemy);
    }
}
