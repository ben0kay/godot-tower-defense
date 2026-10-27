// One active-only dispatcher for paid builds and upgrades: FIFO holds, Citadel flight, pooled visuals and return.
// Work time remains exclusively in ConstructionWorkManager; no ground routes/collisions or ammunition dependencies.
using Godot;
using System;
using System.Collections.Generic;

public enum ConstructionDronePhase { Flying, Deploying, Beaming, Returning, Cancelled }
public partial class ConstructionDroneManager : Node2D
{
    #region Configuration and Assignments
    [Export] public ConstructionDroneMasterConfig Config { get; set; }
    [Export] public PackedScene DroneScene { get; set; }
    private sealed class Assignment
    {
        public TowerBase Tower;
        public ulong Token;
        public CitadelDestination Source;
        public Vector2 Home, Target, Position;
        public float HalfWidth, Hover, Height, Age, Deploy;
        public ConstructionDrone Drone;
        public ConstructionDronePhase Phase;
        public Action Completed, Removed;
        public bool Cancelled;
        public ConstructionDroneMotion Motion = new();
    }
    private readonly Dictionary<TowerBase, Assignment> _jobs = new();
    private readonly Queue<Assignment> _pending = new();
    private readonly List<Assignment> _active = new(64);
    private readonly List<ConstructionDrone> _pool = new(64);
    private TowerPlacementController _placement;
    private CitadelSession _citadels;
    private LevelSessionController _session;
    private WorldGrid _grid;
    private ulong _reservation;
    private bool _shutdown, _ticking, _warned;
    private ConstructionCompletionFlash _completionFlashes;
    private double _retry;
    public bool CanGate => !_shutdown && Config?.Enabled == true && IsInsideTree() && !IsQueuedForDeletion();
    public bool CanServiceUpgrades => CanGate && Config.ServiceUpgrades;
    public int ActiveCount => _active.Count;
    public int PendingCount => _pending.Count;
    public int CreatedVisuals { get; private set; }
    public int Releases { get; private set; }
    // =========================================================
    // O(1) read-only diagnostics for focused tests; no scene scanning during normal flight.
    public ConstructionDrone DroneFor(TowerBase tower) => _jobs.TryGetValue(tower, out var job) ? job.Drone : null;
    public CitadelDestination SourceFor(TowerBase tower) => _jobs.TryGetValue(tower, out var job) ? job.Source : null;
    public ConstructionDronePhase? PhaseFor(TowerBase tower) => _jobs.TryGetValue(tower, out var job) ? job.Phase : null;
    // =========================================================
    // Tokens are unique even if a site is cancelled and the same instance is reused by a fixture later.
    internal ulong Reserve() => ++_reservation;
    #endregion

    #region Binding and Success-only Reservations
    // =========================================================
    // The shared World owns one dispatcher; run after the work clock so arrival never grants earlier frame time.
    public override void _Ready()
    {
        ProcessPhysicsPriority = 100; SetProcess(false); SetPhysicsProcess(false);
        _completionFlashes = new ConstructionCompletionFlash { Name = "CompletionFlashes" };
        AddChild(_completionFlashes);
    }
    // =========================================================
    // Placement connects synchronously in Ready, before any funded site can be created (no deferred binding race).
    public void Bind(TowerPlacementController placement)
    {
        if (GodotObject.IsInstanceValid(_placement)) _placement.TowerPlaced -= OnPlaced;
        _placement = placement; _placement.TowerPlaced += OnPlaced;
        _grid = GetNode<WorldGrid>("../Grid");
        _citadels = GetNodeOrNull<CitadelSession>("../../CitadelSession");
        _session = GetNodeOrNull<LevelSessionController>("../../LevelSession");
    }
    // =========================================================
    // Only committed paid placements or upgrades enqueue work with an already-reserved hold.
    private void OnPlaced(TowerBase tower) => QueueHeldWork(tower);

    // =========================================================
    // The paid upgrade callback uses the same bounded dispatcher after wallet commit.
    internal bool QueueUpgrade(TowerBase tower) => CanServiceUpgrades && QueueHeldWork(tower);

    // =========================================================
    // A single assignment path for both paid initial builds and upgrades.
    private bool QueueHeldWork(TowerBase tower)
    {
        if (_shutdown || !GodotObject.IsInstanceValid(tower) || !tower.IsHeldBy(this) || _jobs.ContainsKey(tower)
            || !GodotObject.IsInstanceValid(_grid) || !IsInsideTree() || IsQueuedForDeletion()) return false;
        var job = new Assignment { Tower = tower, Token = tower.ConstructionDroneReservation, Target = tower.GlobalPosition };
        job.HalfWidth = Mathf.Clamp(tower.Footprint.X * _grid.Config.CellSize * .36f, 16, 160);
        job.Hover = Mathf.Min(ConstructionDroneMasterConfig.Safe(Config?.HoverRadius ?? 12, 12, 0, 20),
            Mathf.Min(tower.Footprint.X, tower.Footprint.Y) * _grid.Config.CellSize * .22f);
        job.Height = Mathf.Clamp(Mathf.Max(ConstructionDroneMasterConfig.Safe(Config?.HoverHeight ?? 48, 48, 16, 96),
            tower.Footprint.Y * _grid.Config.CellSize * .35f + 16), 16, 192);
        job.Completed = () => Completed(job); job.Removed = () => Cancel(job);
        _jobs.Add(tower, job); _pending.Enqueue(job);
        tower.WorkCompleted += job.Completed; tower.TreeExiting += job.Removed; tower.Destroyed += job.Removed;
        SetPhysicsProcess(true);
        return true;
    }
    // =========================================================
    // Detach before callbacks; returning visuals retain neither a tower nor any work subscription.
    private void Forget(Assignment job)
    {
        var tower = job.Tower; job.Tower = null;
        if (tower == null) return;
        _jobs.Remove(tower);
        if (!GodotObject.IsInstanceValid(tower)) return;
        tower.WorkCompleted -= job.Completed; tower.TreeExiting -= job.Removed; tower.Destroyed -= job.Removed;
        job.Completed = job.Removed = null;
    }
    // =========================================================
    // Removal immediately hides VFX; retained list tombstones are compacted safely by the central tick.
    private void Cancel(Assignment job)
    {
        if (job.Cancelled) return;
        job.Cancelled = true; job.Phase = ConstructionDronePhase.Cancelled;
        if (GodotObject.IsInstanceValid(job.Drone)) job.Drone.Park();
        Forget(job); SetPhysicsProcess(!_shutdown);
    }
    #endregion

    #region Citadel Policy and Pool
    // =========================================================
    // Stable objective order resolves equal distances; invalid/dead objectives never become a new home.
    public CitadelDestination NearestHome(Vector2 target)
    {
        CitadelDestination best = null; double distance = double.PositiveInfinity;
        if (!GodotObject.IsInstanceValid(_citadels) || _citadels.Objectives == null) return null;
        foreach (var candidate in _citadels.Objectives)
        {
            if (!ValidHome(candidate)) continue;
            Vector2 point = LaunchPoint(candidate); double next = point.DistanceSquaredTo(target);
            if (next < distance || (next == distance && (best == null || candidate.Number < best.Number)))
            { best = candidate; distance = next; }
        }
        return best;
    }
    // =========================================================
    // Cached destination identity is combined with authoritative destruction and queued-removal guards.
    private static bool ValidHome(CitadelDestination home) => home?.Available == true &&
        GodotObject.IsInstanceValid(home.Root) && !home.Root.IsQueuedForDeletion() &&
        GodotObject.IsInstanceValid(home.Health) && !home.Health.IsDestroyed;
    // =========================================================
    // An optional authored marker wins; otherwise derive the actual footprint center, not a hardcoded single-Citadel path.
    private Vector2 LaunchPoint(CitadelDestination home)
    {
        Node2D marker = home.Root.GetNodeOrNull<Node2D>("DroneLaunch");
        if (marker != null) return marker.GlobalPosition;
        Vector2I size = home.Root is WorldObjectFootprint footprint ? footprint.SizeCells : Vector2I.One;
        return home.Root.ToGlobal(((Vector2)size - Vector2.One) * _grid.Config.CellSize * .5f);
    }
    // =========================================================
    // Lazy bounded pooling: instantiate only when concurrency grows; no per-frame scenes/resources/arrays are allocated.
    private ConstructionDrone Rent()
    {
        while (_pool.Count > 0)
        {
            int last = _pool.Count - 1; var drone = _pool[last]; _pool.RemoveAt(last);
            if (GodotObject.IsInstanceValid(drone) && !drone.IsQueuedForDeletion()) return drone;
        }
        if (DroneScene == null) return null;
        Node node = null;
        try
        {
            node = DroneScene.Instantiate();
            if (node is not ConstructionDrone drone || node.GetNodeOrNull<ConstructionBeam>("Beam") == null)
            { node.Free(); return null; }
            AddChild(drone); CreatedVisuals++; return drone;
        }
        catch (Exception) { if (GodotObject.IsInstanceValid(node)) node.Free(); return null; }
    }
    // =========================================================
    // Freed/external visuals are never returned to the idle pool; assignment failures use the same safe release policy.
    private void Park(Assignment job)
    {
        if (GodotObject.IsInstanceValid(job.Drone) && !job.Drone.IsQueuedForDeletion())
        { job.Drone.Park(); _pool.Add(job.Drone); }
        job.Drone = null;
    }
    // =========================================================
    // Ordinary compatibility fails open; strict testing mode leaves a paid site held until a valid retry is possible.
    private void Fallback(Assignment job)
    {
        if (!_warned) { _warned = true; GD.PushWarning("Construction drone unavailable: normal mode releases site work; strict mode waits for a valid builder/home."); }
        if (Config?.RequireDroneToBuild == true)
        {
            Park(job); job.Source = null; _pending.Enqueue(job); return;
        }
        Release(job); Cancel(job);
    }
    // =========================================================
    // The tower validates both manager identity and exact token; repeated/stale arrival cannot resume another job.
    private void Release(Assignment job)
    {
        if (GodotObject.IsInstanceValid(job.Tower) && job.Tower.ReleaseConstructionDrone(this, job.Token)) Releases++;
    }
    #endregion

    #region Central Flight and Completion
    // =========================================================
    // Process only current assignments. FIFO dispatch has one small per-tick creation/attempt budget.
    public override void _PhysicsProcess(double delta)
    {
        if (_shutdown || _ticking || !IsInsideTree() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (GodotObject.IsInstanceValid(_session) && _session.State != LevelSessionState.Playing) return;
        using var pf2 = SubsystemProfiler.Measure(this, ProfileCategory.Towers);
        _ticking = true;
        try
        {
            if (Config?.Enabled != true) { DisableFeature(); return; }
            float dt = (float)Math.Min(delta, 3600);
            if (Config.ShowVisuals == false) _completionFlashes?.Clear();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var job = _active[i];
                if (!job.Cancelled) Advance(job, dt);
                if (!job.Cancelled && job.Phase != ConstructionDronePhase.Cancelled) continue;
                Park(job); _active.RemoveAt(i);
            }
            _retry -= delta;
            if (_retry <= 0) Dispatch();
        }
        finally { _ticking = false; SetPhysicsProcess(_active.Count > 0 || _pending.Count > 0); }
    }
    // =========================================================
    // Never drain an unbounded queue or allocate one drone for every queued build in a single frame.
    private void Dispatch()
    {
        int budget = Mathf.Clamp(Config.DispatchesPerTick, 1, 16), cap = Mathf.Clamp(Config.MaxActiveDrones, 1, 64);
        while (budget-- > 0 && _pending.Count > 0 && _active.Count < cap)
        {
            var job = _pending.Dequeue();
            if (job.Cancelled || !GodotObject.IsInstanceValid(job.Tower) || !job.Tower.IsHeldBy(this)) { Cancel(job); continue; }
            job.Source = NearestHome(job.Target);
            job.Drone = job.Source != null ? Rent() : null;
            if (job.Source == null || job.Drone == null)
            { Fallback(job); _retry = .25; break; }
            job.Home = LaunchPoint(job.Source); job.Position = job.Home;
            InitializeMotion(job);
            Vector2 center = job.Target + Vector2.Up * job.Height;
            job.Motion.Heading = (job.Motion.OrbitPoint(center) - job.Home).Angle();
            job.Motion.BeginFlight(job.Home, job.Motion.OrbitPoint(center), Vector2.Zero, job.Motion.Tangent(), Config);
            job.Phase = ConstructionDronePhase.Flying; job.Age = job.Deploy = 0;
            _active.Add(job); Present(job, 0);
        }
    }
    // =========================================================
    // Set up one bounded movement state per assignment; random choices do not touch global combat/wave RNG.
    private void InitializeMotion(Assignment job)
    {
        job.Motion.RandomState = unchecked((uint)(job.Token * 747796405UL + 2891336453UL));
        float radius = ConstructionDroneMasterConfig.Safe(Config.OrbitRadius, 28, 8, 160);
        job.Motion.Radii = new(Mathf.Max(radius, job.HalfWidth * 1.15f), Mathf.Max(radius * .55f, job.Height * .3f));
        job.Motion.Angle = -Mathf.Pi / 2 + Mathf.Lerp(-.8f, .8f, job.Motion.Random());
        job.Motion.HoldSeconds = job.Motion.NextHold(Config);
        job.Motion.BobPhase = job.Motion.Random() * Mathf.Tau;
        job.Motion.HoverStart = SavePoint.From(job.Position); job.Motion.HoverBlend = 0;
    }
    // =========================================================
    // Begin a curved return along the current motion before bending toward the Citadel; work is already finished.
    private void BeginReturn(Assignment job)
    {
        Vector2 direction = job.Motion.Velocity.Vector();
        if (direction.LengthSquared() < 1) direction = Vector2.FromAngle(job.Motion.Heading);
        job.Motion.BeginFlight(job.Position, job.Home, direction, job.Home - job.Position, Config);
    }
    // =========================================================
    // Movement changes visuals/state only. The normal work clock is the only owner of elapsed build time.
    private void Advance(Assignment job, float dt)
    {
        if (!GodotObject.IsInstanceValid(job.Drone) || job.Drone.IsQueuedForDeletion())
        { Fallback(job); job.Phase = ConstructionDronePhase.Cancelled; return; }
        if (job.Phase == ConstructionDronePhase.Returning)
        {
            if (!ValidHome(job.Source))
            {
                job.Source = NearestHome(job.Position);
                if (job.Source == null) { job.Cancelled = true; return; }
                job.Home = LaunchPoint(job.Source); BeginReturn(job);
            }
            if (job.Motion.Fly(dt, ref job.Position, Config)) job.Cancelled = true;
            Present(job, 0); return;
        }
        if (!GodotObject.IsInstanceValid(job.Tower) || job.Tower.IsDestroyed || job.Tower.IsQueuedForDeletion()) { Cancel(job); return; }
        if (job.Phase != ConstructionDronePhase.Beaming && !ValidHome(job.Source))
        { Fallback(job); job.Phase = ConstructionDronePhase.Cancelled; return; }
        Vector2 hover = job.Target + Vector2.Up * job.Height;
        if (job.Phase == ConstructionDronePhase.Flying)
        {
            if (job.Motion.Fly(dt, ref job.Position, Config))
            {
                job.Phase = ConstructionDronePhase.Deploying;
                job.Motion.HoverStart = SavePoint.From(job.Position); job.Motion.HoverBlend = 0;
            }
            Present(job, 0); return;
        }
        job.Age += dt;
        job.Motion.Hover(hover, job.Age, job.Hover, dt, ref job.Position, Config);
        float deployment = ConstructionDroneMasterConfig.Safe(Config.BeamDeploymentSeconds, .25f, 0, 1);
        if (job.Phase == ConstructionDronePhase.Deploying)
        {
            job.Deploy += dt; float amount = deployment <= 0 ? 1 : Mathf.Min(1, job.Deploy / deployment);
            Present(job, amount);
            if (amount >= 1) { job.Phase = ConstructionDronePhase.Beaming; Release(job); }
        }
        else Present(job, 1);
    }
    // =========================================================
    // WorkCompleted is authoritative; hide the beam immediately and leave the now operational tower alone.
    private void Completed(Assignment job)
    {
        if (job.Cancelled || job.Tower?.WorkPhase != TowerWorkPhase.Operational) return;
        _completionFlashes?.Spawn(job.Target, job.HalfWidth, Config);
        Forget(job); job.Phase = ConstructionDronePhase.Returning; BeginReturn(job);
        if (GodotObject.IsInstanceValid(job.Drone)) Present(job, 0); else job.Cancelled = true;
    }
    // =========================================================
    // A fixed site center and bounded footprint-derived hover/beam make large factories use the same implementation.
    private void Present(Assignment job, float strength) => job.Drone.Present(job.Position, job.Target, job.HalfWidth, strength, Config, job.Age, job.Motion.Heading);
    #endregion

    #region Disable and Teardown
    // =========================================================
    // Feature OFF releases holds without creating/completing work; visual OFF alone never enters this path.
    private void DisableFeature()
    {
        _completionFlashes?.Clear();
        while (_pending.TryDequeue(out var job)) { Release(job); Cancel(job); Park(job); }
        for (int i = _active.Count - 1; i >= 0; i--) { var job = _active[i]; Release(job); Cancel(job); Park(job); }
        _active.Clear();
    }
    // =========================================================
    // Remove subscriptions and release orphan holds if this service alone is removed; world teardown still cannot advance work.
    public override void _ExitTree()
    {
        _shutdown = true;
        if (GodotObject.IsInstanceValid(_placement)) _placement.TowerPlaced -= OnPlaced;
        DisableFeature(); _jobs.Clear(); _pool.Clear(); _placement = null; _citadels = null; _session = null; _grid = null;
        SetPhysicsProcess(false);
    }
    #endregion
}
