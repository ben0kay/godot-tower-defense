// One active-only world physics loop: retained hostile shot slots, spatial swept tower hits and shared Kinetic damage.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyBulletSystem : Node
{
    #region Configuration and State
    [Export] public EnemyProjectileMasterConfig Config { get; set; }
    private EnemyBulletData[] _shots = Array.Empty<EnemyBulletData>();
    private readonly List<TowerBase> _candidates = new();
    private TowerRegistry _towers;
    private EnemyBulletRenderer _renderer;
    private ProjectileVisualSnapshot _defaultVisual;
    private double _lifetime;
    private int _count;
    private bool _advancing;
    public int ActiveCount => _count;
    public int Capacity => _shots.Length;
    public long Spawned { get; private set; }
    public long Impacts { get; private set; }
    public long Expired { get; private set; }
    public long Invalidated { get; private set; }
    public long Cleared { get; private set; }
    public long Rejected { get; private set; }
    public long Overflow { get; private set; }
    public long CandidateChecks { get; private set; }
    // =========================================================
    // Snapshot the world budget/presentation once; empty battlefields have no bullet physics or render processing.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _towers = GetNode<TowerRegistry>("../TowerRegistry");
        _renderer = GetNode<EnemyBulletRenderer>("../EnemyBulletRenderer");
        _shots = new EnemyBulletData[Math.Clamp(Config?.MaximumActive ?? 4096, 1, 65536)];
        double lifetime = Config?.DefaultLifetimeSeconds ?? 2.5;
        _lifetime = double.IsFinite(lifetime) ? Math.Clamp(lifetime, .01, 30) : 2.5;
        _defaultVisual = Config?.DefaultVisual?.Capture() ?? default;
        _renderer.Bind(this);
    }
    // =========================================================
    // Renderer/test access returns a value, never a reference into the mutable live slot buffer.
    public EnemyBulletData Snapshot(int index)
    { if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index)); return _shots[index]; }
    #endregion

    #region Launch and Budget
    // =========================================================
    // Caller supplies actual muzzle direction. Reject new overflow; never cull a previously launched gameplay bullet.
    public bool TrySpawn(EnemyShotRequest request)
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused || _advancing || _shots.Length == 0
            || !GodotObject.IsInstanceValid(_towers) || !_towers.IsInsideTree() || _towers.IsQueuedForDeletion()
            || !request.Origin.IsFinite() || !request.Direction.IsFinite() || !float.IsFinite(request.Damage) || request.Damage < 0
            || !float.IsFinite(request.Speed) || request.Speed <= 0 || !double.IsFinite(request.LifetimeSeconds)
            || request.LifetimeSeconds < 0 || request.LifetimeSeconds > 30)
        { Rejected++; return false; }
        double length = Math.Sqrt((double)request.Direction.X * request.Direction.X + (double)request.Direction.Y * request.Direction.Y);
        if (length <= 1e-12) { Rejected++; return false; }
        if (_count == _shots.Length) { Overflow++; return false; }
        Vector2 direction = new((float)(request.Direction.X / length), (float)(request.Direction.Y / length));
        _shots[_count++] = new()
        {
            Position = request.Origin, Direction = direction, Speed = request.Speed,
            RemainingSeconds = request.LifetimeSeconds > 0 ? request.LifetimeSeconds : _lifetime,
            Hit = new DamageInfo(request.Damage, DamageType.Kinetic),
            Visual = Sanitize(request.Visual ?? _defaultVisual)
        };
        Spawned++; SetPhysicsProcess(true); Redraw(); return true;
    }
    // =========================================================
    // Even direct request callers cannot send NaN dimensions/colors or a disposed texture into native drawing.
    private static ProjectileVisualSnapshot Sanitize(ProjectileVisualSnapshot visual) => new(visual.Enabled,
        WeaponVisualMath.Safe(visual.Length, 1, 128), WeaponVisualMath.Safe(visual.Width, .1f, 12),
        WeaponVisualMath.Safe(visual.GlowWidth, 1, 4), WeaponVisualMath.Color(visual.Core),
        WeaponVisualMath.Color(visual.Body), WeaponVisualMath.Color(visual.Glow), visual.BrightTip,
        GodotObject.IsInstanceValid(visual.Texture) ? visual.Texture : null);
    // =========================================================
    // A world may explicitly reset bullets for a restart; source death/removal must never call this.
    public void Clear()
    {
        Cleared += _count; Array.Clear(_shots, 0, _count); _count = 0; _candidates.Clear(); SetPhysicsProcess(false); Redraw();
    }
    #endregion

    #region Swept Simulation
    // =========================================================
    // Physics owns travel; drawing/offscreen visibility has no authority to stop damage.
    public override void _PhysicsProcess(double delta) => Advance(delta);
    // =========================================================
    // Clip travel to remaining lifetime, resolve earliest footprint once, then swap-remove without allocating.
    public void Advance(double delta)
    {
        if (_advancing || _count == 0 || !double.IsFinite(delta) || delta <= 0 || !IsInsideTree()
            || IsQueuedForDeletion() || GetTree().Paused) return;
        if (!GodotObject.IsInstanceValid(_towers) || !_towers.IsInsideTree() || _towers.IsQueuedForDeletion()) { Clear(); return; }
        _advancing = true;
        try
        {
            for (int i = 0; i < _count;)
            {
                EnemyBulletData shot = _shots[i];
                double seconds = Math.Min(delta, shot.RemainingSeconds);
                Vector2 end = new((float)(shot.Position.X + (double)shot.Direction.X * shot.Speed * seconds),
                    (float)(shot.Position.Y + (double)shot.Direction.Y * shot.Speed * seconds));
                if (!end.IsFinite()) { Remove(i); Invalidated++; continue; }
                _towers.GetSegmentCandidates(shot.Position, end, _candidates);
                CandidateChecks += _towers.LastCandidates;
                TowerBase hit = null; double first = double.PositiveInfinity; long order = long.MaxValue;
                foreach (TowerBase tower in _candidates)
                {
                    if (!_towers.TryIntersectFootprint(tower, shot.Position, end, out double fraction)) continue;
                    long serial = _towers.RegistrationOrder(tower);
                    if (fraction < first || (fraction == first && serial < order))
                    { hit = tower; first = fraction; order = serial; }
                }
                _candidates.Clear();
                if (hit != null)
                {
                    Remove(i); Impacts++; // Consume before callbacks: a dying tower cannot be hit twice by this round.
                    hit.ApplyDamage(shot.Hit);
                    if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return;
                    if (!GodotObject.IsInstanceValid(_towers) || !_towers.IsInsideTree()) { Clear(); return; }
                    continue;
                }
                shot.Position = end; shot.RemainingSeconds -= seconds;
                if (shot.RemainingSeconds <= 0) { Remove(i); Expired++; continue; }
                _shots[i++] = shot;
            }
        }
        finally { _advancing = false; }
        if (_count == 0) SetPhysicsProcess(false);
        Redraw();
    }
    // =========================================================
    // Dense slots keep stepping/drawing bounded by actual bullets; clearing the tail releases optional texture references.
    private void Remove(int index) { _shots[index] = _shots[--_count]; _shots[_count] = default; }
    // =========================================================
    // Dirty-on-change rendering sleeps at rest; no Render/Process callback is needed.
    private void Redraw()
    { if (GodotObject.IsInstanceValid(_renderer) && _renderer.IsInsideTree()) _renderer.QueueRedraw(); }
    // =========================================================
    // The world owns every slot; teardown retains no enemy/tower nodes or pending hit buffers.
    public override void _ExitTree()
    {
        Clear();
        if (GodotObject.IsInstanceValid(_renderer)) _renderer.Bind(null);
        _shots = Array.Empty<EnemyBulletData>(); _towers = null; _renderer = null;
    }
    #endregion
}
