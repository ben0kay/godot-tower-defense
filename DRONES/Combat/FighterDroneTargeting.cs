// One world-owned, bounded threat cache shared by all deployed fighters; leases spread comparable threats.
using Godot;
using System.Collections.Generic;
public partial class FighterDroneTargeting : Node
{
    private EnemyRegistry _registry;
    private readonly List<(Enemy Enemy, float Distance)> _building = new(), _ranked = new();
    private readonly Dictionary<FighterDrone, Enemy> _leases = new();
    private ulong _frame = ulong.MaxValue;
    private int _cursor;
    private float _wait;
    private bool _scanning;
    private const int CandidateLimit = 64, ScanBudget = 96;
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    internal static FighterDroneTargeting For(EnemyRegistry registry)
    {
        if (registry == null) return null;
        var service = registry.GetNodeOrNull<FighterDroneTargeting>("FighterThreats");
        if (service != null) return service;
        service = new FighterDroneTargeting { Name = "FighterThreats", _registry = registry };
        registry.AddChild(service); return service;
    }
    internal bool Valid(Enemy enemy) => TowerDetection.CanDetect(enemy) && _registry.Contains(enemy)
        && enemy.AssignedCitadel?.Available == true && float.IsFinite(enemy.RemainingJourneyDistance);
    // =========================================================
    // Called by active station managers, at most once per world physics frame; no enemy work between waves.
    internal void Tick(float dt)
    {
        ulong frame = Engine.GetPhysicsFrames(); if (_frame == frame) return; _frame = frame;
        if (_registry.Living.Count == 0) { _ranked.Clear(); _building.Clear(); _cursor = 0; _scanning = false; _wait = 0; return; }
        if (!_scanning)
        {
            _wait -= dt; if (_wait > 0) return;
            _building.Clear(); _cursor = 0; _scanning = true;
        }
        int end = System.Math.Min(_cursor + ScanBudget, _registry.Living.Count);
        for (; _cursor < end; _cursor++)
        {
            Enemy enemy = _registry.Living[_cursor]; if (!Valid(enemy)) continue;
            int at = 0; while (at < _building.Count && _building[at].Distance <= enemy.RemainingJourneyDistance) at++;
            if (at >= CandidateLimit) continue;
            _building.Insert(at, (enemy, enemy.RemainingJourneyDistance)); if (_building.Count > CandidateLimit) _building.RemoveAt(CandidateLimit);
        }
        if (_cursor < _registry.Living.Count) return;
        _ranked.Clear(); _ranked.AddRange(_building); _scanning = false; _wait = .25f;
    }
    internal void Release(FighterDrone fighter) => _leases.Remove(fighter);
    internal Enemy Acquire(FighterDrone fighter, Enemy current)
    {
        _leases.Remove(fighter);
        Enemy best = Valid(current) ? current : null;
        float score = best == null ? float.PositiveInfinity : Score(best) * .8f;
        foreach (var entry in _ranked)
        {
            Enemy enemy = entry.Enemy;
            if (!Valid(enemy)) continue;
            float candidate = Score(enemy);
            if (candidate < score) { best = enemy; score = candidate; }
        }
        if (best != null) _leases[fighter] = best;
        return best;
    }
    private float Score(Enemy enemy)
    {
        int assigned = 0; foreach (Enemy target in _leases.Values) if (target == enemy) assigned++;
        float journey = enemy.RemainingJourneyDistance;
        return journey <= 128 ? journey : journey * (1 + assigned * .35f);
    }
}
