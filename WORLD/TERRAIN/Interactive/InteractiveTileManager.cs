// One world-scoped terrain dispatcher: cached hazard cells, swept exposure and only active-occupant timers.
using Godot;
using System;
using System.Collections.Generic;

public partial class InteractiveTileManager : Node
{
    [Export] public LavaTileConfig LavaConfig { get; set; }
    private sealed class Exposure { public double LastTime, NextHit; }
    private WorldGrid _grid;
    private EnemyRegistry _registry;
    private HashSet<Vector2I> _lava = new();
    private readonly Dictionary<Enemy, Exposure> _active = new();
    private readonly List<Enemy> _ticks = new(), _reconcile = new(), _nearby = new();
    private DamageInfo _hit;
    private double _interval, _clock;
    private bool _entry;
    public int ActiveCount => _active.Count;
    public int LavaCellCount => _lava.Count;
    public long TraversedCells { get; private set; }

    // =========================================================
    // Movement runs first; the late manager tick supplies only each occupant's unaccounted stationary time.
    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        _grid = GetNode<WorldGrid>("../Grid"); _registry = GetNode<EnemyRegistry>("../EnemySystems/Registry");
        OnConfigChanged(); LavaConfig.Changed += OnConfigChanged;
        _registry.EnemyUnregistered += Remove;
        _grid.InteractiveTiles = this; _grid.TerrainRebuilt += Rebuild;
        Rebuild();
    }

    // =========================================================
    // Edits affect subsequent hits; reschedule active timers from their last accounted instant, without entry hits.
    private void OnConfigChanged()
    {
        if (LavaConfig == null) throw new InvalidOperationException("Interactive terrain requires a LavaTileConfig.");
        LavaConfig.Validate(); _hit = new(LavaConfig.DamagePerTick, LavaConfig.DamageType);
        _interval = LavaConfig.DamageIntervalSeconds; _entry = LavaConfig.DamageOnEntry;
        foreach (Exposure exposure in _active.Values) exposure.NextHit = exposure.LastTime + _interval;
    }

    // =========================================================
    // Explicit edits rebuild cells only; discover newly affected occupants through existing spatial queries.
    public void Rebuild()
    {
        HashSet<Vector2I> next = new(); TileMapLayer layer = _grid.Level.Interactive;
        if (layer != null)
        {
            if (layer.TileSet.GetCustomDataLayerByName("interaction") < 0
                || layer.TileSet.GetCustomDataLayerType(layer.TileSet.GetCustomDataLayerByName("interaction")) != Variant.Type.String)
                throw new InvalidOperationException("Interactive TileSet requires String interaction metadata.");
            foreach (Vector2I painted in layer.GetUsedCells())
            {
                TileData tile = layer.GetCellTileData(painted);
                if (tile == null) continue;
                string kind = tile.GetCustomData("interaction").AsString();
                if (kind != "lava" && kind != "foundation") throw new InvalidOperationException("Unknown interactive terrain: " + kind);
                Vector2I cell = _grid.WorldToCell(layer.ToGlobal(layer.MapToLocal(painted)));
                if (kind == "lava" && _grid.IsInsideMap(cell) && !_grid.TerrainBlocksEnemies(cell)) next.Add(cell);
            }
        }
        HashSet<Vector2I> previous = _lava; _lava = next;
        _reconcile.Clear(); _reconcile.AddRange(_active.Keys);
        foreach (Enemy enemy in _reconcile) if (!Eligible(enemy) || !IsLava(enemy.GlobalPosition)) Remove(enemy);
        List<Enemy> arrivals = new(); // Explicit rebuild only; snapshot before any entry can invoke a kill callback.
        foreach (Vector2I cell in next)
        {
            if (previous.Contains(cell)) continue;
            _registry.GetEnemiesInRadius(_grid.CellToWorldCenter(cell), _grid.Config.CellSize * .708f, _nearby);
            foreach (Enemy enemy in _nearby) if (_grid.WorldToCell(enemy.GlobalPosition) == cell) arrivals.Add(enemy);
        }
        foreach (Enemy enemy in arrivals) if (Eligible(enemy) && IsLava(enemy.GlobalPosition)) Enter(enemy, _clock);
    }

    // =========================================================
    // Spawns/direct transforms are arrivals, not swept travel through intervening terrain.
    public void Reposition(Enemy enemy)
    {
        Remove(enemy);
        if (Eligible(enemy) && IsLava(enemy.GlobalPosition)) Enter(enemy, _clock);
    }

    // =========================================================
    // One dictionary removal handles exit, death, arrival and external frees; no registry scan/subscription fanout.
    public void Remove(Enemy enemy) => _active.Remove(enemy);
    // =========================================================
    // Cached cell lookup never infers behavior from artwork or touches the painted TileMap each frame.
    public bool IsLava(Vector2 position) => _lava.Count > 0 && position.IsFinite() && _lava.Contains(_grid.WorldToCell(position));

    // =========================================================
    // DDA traverses positive-duration cell interiors; simultaneous corner crossings cannot double-enter.
    // Offsets/durations are supplied by actual safe movement segments, never a guessed frame-wide exposure.
    public bool Traverse(Enemy enemy, Vector2 from, Vector2 to, double duration, double offset = 0)
    {
        if (!Eligible(enemy) || !from.IsFinite() || !to.IsFinite() || !double.IsFinite(duration) || duration < 0
            || !double.IsFinite(offset) || offset < 0) return false;
        double startTime = _clock + offset;
        if (_lava.Count == 0) return true;
        if (from == to) { Sync(enemy, from, startTime); return Advance(enemy, startTime + duration, from, to, startTime, duration); }
        Vector2 a = _grid.ToLocal(from) / _grid.Config.CellSize, b = _grid.ToLocal(to) / _grid.Config.CellSize;
        double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y;
        int x = Mathf.FloorToInt(a.X), y = Mathf.FloorToInt(a.Y), sx = Math.Sign(dx), sy = Math.Sign(dy);
        double tx = dx == 0 ? double.PositiveInfinity : ((sx > 0 ? x + 1 : x) - a.X) / dx;
        double ty = dy == 0 ? double.PositiveInfinity : ((sy > 0 ? y + 1 : y) - a.Y) / dy;
        double stepX = dx == 0 ? double.PositiveInfinity : 1 / Math.Abs(dx), stepY = dy == 0 ? double.PositiveInfinity : 1 / Math.Abs(dy);
        double t = 0;
        while (t < 1)
        {
            double end = Math.Min(1, Math.Min(tx, ty));
            if (end > t)
            {
                TraversedCells++;
                if (_lava.Contains(new(x, y))) Enter(enemy, startTime + duration * t, from.Lerp(to, (float)t));
                else Remove(enemy);
                if (!Eligible(enemy) || !Advance(enemy, startTime + duration * end, from, to, startTime, duration)) return false;
            }
            if (end >= 1) break;
            bool crossX = tx <= ty + 1e-12, crossY = ty <= tx + 1e-12;
            if (crossX) { x += sx; tx += stepX; }
            if (crossY) { y += sy; ty += stepY; }
            t = Math.Max(t, end);
        }
        Sync(enemy, to, startTime + duration);
        return Eligible(enemy);
    }

    // =========================================================
    // One continuous Lava exposure survives cell boundaries, with a fresh schedule only after actual exit.
    private void Enter(Enemy enemy, double time, Vector2? position = null)
    {
        if (_active.ContainsKey(enemy) || !Eligible(enemy)) return;
        _active.Add(enemy, new() { LastTime = time, NextHit = time + _interval });
        if (_entry) Hit(enemy, position ?? enemy.GlobalPosition);
    }
    // =========================================================
    // Half-open floor cell ownership supplies exact endpoint/spawn semantics without corner-only phantom cells.
    private void Sync(Enemy enemy, Vector2 position, double time)
    { if (IsLava(position)) Enter(enemy, time, position); else Remove(enemy); }

    // =========================================================
    // Deliver every owed tick, stopping immediately on removal. Death feedback uses the actual encounter point.
    private bool Advance(Enemy enemy, double time, Vector2 from, Vector2 to, double startTime, double duration)
    {
        if (!_active.TryGetValue(enemy, out Exposure exposure)) return Eligible(enemy);
        if (_hit.Amount == 0 && exposure.NextHit <= time)
        {
            double owed = Math.Floor((time - exposure.NextHit) / _interval) + 1;
            exposure.NextHit += owed * _interval;
            if (exposure.NextHit <= time) exposure.NextHit = Math.BitIncrement(time);
            exposure.LastTime = time; return Eligible(enemy); // Zero hits cannot change health, rewards or XP.
        }
        // Float position samples can shift a cell-edge timestamp by a fraction of a microsecond.
        double tolerance = Math.Min(1e-6, _interval * 1e-5);
        while (exposure.NextHit <= time + tolerance)
        {
            double hitTime = exposure.NextHit; exposure.NextHit += _interval;
            exposure.LastTime = hitTime;
            float fraction = duration > 0 ? (float)Math.Clamp((hitTime - startTime) / duration, 0, 1) : 1;
            Hit(enemy, from.Lerp(to, fraction));
            if (!Eligible(enemy) || !_active.TryGetValue(enemy, out Exposure current) || current != exposure) return Eligible(enemy);
        }
        exposure.LastTime = Math.Max(exposure.LastTime, time);
        return Eligible(enemy);
    }

    // =========================================================
    // Environmental damage has no DamageSource and uses the canonical shield/hull/death/credit pipeline.
    private void Hit(Enemy enemy, Vector2 position)
    { enemy.SetMovementPosition(position); enemy.ApplyDamage(_hit); }

    // =========================================================
    // Only current occupants are visited; no tile iteration or full-enemy-registry loop occurs here.
    public override void _PhysicsProcess(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0) return;
        double end = _clock + delta;
        _ticks.Clear(); _ticks.AddRange(_active.Keys);
        for (int i = 0; i < _ticks.Count; i++)
        {
            Enemy enemy = _ticks[i];
            if (!Eligible(enemy)) { Remove(enemy); continue; }
            Vector2 position = enemy.GlobalPosition;
            Advance(enemy, end, position, position, _clock, delta);
        }
        _clock = end;
    }

    // =========================================================
    // Membership is checked after every callback; a lethal tick cannot continue travel or earn duplicate rewards.
    private bool Eligible(Enemy enemy) => GodotObject.IsInstanceValid(enemy) && enemy.IsAlive && enemy.IsInsideTree()
        && !enemy.IsQueuedForDeletion() && GodotObject.IsInstanceValid(_registry) && _registry.Contains(enemy);

    // =========================================================
    // A level-scoped manager never retains occupants or resource subscriptions after unloading.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) _registry.EnemyUnregistered -= Remove;
        if (GodotObject.IsInstanceValid(LavaConfig)) LavaConfig.Changed -= OnConfigChanged;
        if (GodotObject.IsInstanceValid(_grid)) { _grid.TerrainRebuilt -= Rebuild; _grid.InteractiveTiles = null; }
        _active.Clear(); _ticks.Clear(); _reconcile.Clear(); _nearby.Clear(); _lava.Clear();
        _grid = null; _registry = null; LavaConfig = null;
    }
}
