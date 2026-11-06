// Persistent station-owned fighter; finite launch/dock legs and continuous autopilot share DroneBody presentation.
using Godot;
using System;

public enum FighterDroneState { Docked, Launching, Holding, Returning, Intercepting, Manoeuvring }
public partial class FighterDrone : Node2D
{
    [Export] public FighterDroneConfig Config { get; set; }
    public string SaveId { get; private set; }
    public int Slot { get; private set; }
    public float Health { get; private set; }
    public int Ammo { get; private set; }
    public float MaxHealth => FighterDroneConfig.Safe(Config.MaxHealth, 300, 1, 10000);
    public int AmmoCapacity => Mathf.Clamp(Config.AmmoCapacity, 1, 10000);
    public FighterDroneState State { get; private set; } = FighterDroneState.Docked;
    public bool IsFlying => State != FighterDroneState.Docked;
    private bool InTransit => State is FighterDroneState.Launching or FighterDroneState.Returning;
    private readonly FighterDroneMovement _steering = new();
    private Enemy _target;
    private int _targetGeneration;
    private float _search, _retargetHold;
    internal void ClearTarget(FighterDroneTargeting targeting) { targeting?.Release(this); _target = null; }
    private DroneFlightMotion _motion = new() { Heading = -Mathf.Pi / 2 };
    private DroneBody _body;
    private FighterDroneWeapon _weapon;
    internal void BindWeapon(WorldGrid grid, Node2D projectiles) => _weapon?.Bind(this, grid, projectiles);
    // Ammunition is spent only after a real friendly projectile successfully enters the world.
    internal void RecordFiredRound() { if (Ammo > 0) Ammo--; }
    private Vector2 _initialPosition;

    internal void Initialize(FighterDroneConfig config, int slot, Vector2 position, string id = null)
    {
        Config = config; Slot = slot; SaveId = id ?? Guid.NewGuid().ToString("N");
        Health = MaxHealth; Ammo = AmmoCapacity; _initialPosition = position;
        _steering.Phase = slot * Mathf.Pi / 2; _search = slot * .08f;
    }
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _body = GetNode<DroneBody>("Body");
        _weapon = GetNodeOrNull<FighterDroneWeapon>("Weapon");
        GlobalPosition = _initialPosition; Present(0, false);
    }

    // =========================================================
    // A new finite leg retains real HP/ammo. No launch/landing event replenishes either reservoir.
    internal void BeginTransit(Vector2 destination, bool returning)
    {
        Vector2 direction = destination - GlobalPosition;
        Vector2 departure = InTransit ? _motion.Velocity.Vector() : _steering.Velocity;
        if (departure.LengthSquared() < 1) departure = direction;
        if (!InTransit) _motion.Heading = _steering.Heading;
        _motion.BeginFlight(GlobalPosition, destination, departure, direction,
            FighterDroneConfig.Safe(Config.FlightSpeed, 340, 32, 2000), 100,
            Config.Acceleration, Config.Deceleration);
        State = returning ? FighterDroneState.Returning : FighterDroneState.Launching;
        _steering.ResetPass(); _weapon?.Suspend();
        Present(0, true);
    }
    internal bool Step(float dt)
    {
        if (!InTransit) return false;
        Vector2 point = GlobalPosition;
        bool arrived = _motion.Fly(dt, ref point, Config.TurnDegreesPerSecond); GlobalPosition = point;
        if (arrived)
        {
            State = State == FighterDroneState.Returning ? FighterDroneState.Docked : FighterDroneState.Holding;
            _steering.Heading = _motion.Heading; _steering.Velocity = _motion.Velocity.Vector(); _search = Slot * .08f;
        }
        Present(dt, true); return arrived;
    }
    internal bool StepAutoPilot(float dt, FighterDroneTargeting targeting, Vector2 home, Func<Vector2, Vector2> clamp)
    {
        if (!IsFlying || InTransit) return false;
        if (targeting == null || !targeting.Valid(_target) || _target.SpawnGeneration != _targetGeneration) ClearTarget(targeting);
        _search -= dt; _retargetHold = Mathf.Max(0, _retargetHold - dt);
        if (_search <= 0)
        {
            if (_target == null || _retargetHold <= 0)
            {
                Enemy previous = _target;
                _target = targeting?.Acquire(this, _target);
                if (_target != previous)
                    _retargetHold = FighterDroneConfig.Safe(Config.TargetHoldSeconds, 1.5f, 0, 5);
            }
            _targetGeneration = _target?.SpawnGeneration ?? 0;
            _search = FighterDroneConfig.Safe(Config.TargetSearchSeconds, .3f, .1f, 2);
        }
        FighterDroneState before = State;
        float engagement = FighterDroneConfig.Safe(Config.AttackDistance, 160, 32, 1200)
            + FighterDroneConfig.Safe(Config.OrbitRadius, 100, 24, 600) * 1.8f;
        State = _target == null ? FighterDroneState.Holding :
            GlobalPosition.DistanceTo(_target.GlobalPosition) > engagement ? FighterDroneState.Intercepting : FighterDroneState.Manoeuvring;
        _steering.Step(this, _target, home, dt, clamp); Present(dt, true);
        bool fired = _weapon?.Step(dt, _target) == true;
        return before != State || fired;
    }
    private void Present(float dt, bool ignite) => _body?.Present(InTransit ? _motion.Heading : _steering.Heading,
        InTransit ? _motion.Velocity.Vector() : IsFlying ? _steering.Velocity : Vector2.Zero,
        FighterDroneConfig.Safe(Config.FlightSpeed, 340, 32, 2000),
        IsFlying, State == FighterDroneState.Returning ? 1 : 0, null, dt, ignite);

    // =========================================================
    // Only real source state is serialized; the arc-length table remains a derived, lazily rebuilt cache.
    internal FighterDroneSaveData Capture() => new()
    {
        Id = SaveId, Slot = Slot, Health = Health, Ammo = Ammo, State = State,
        Position = SavePoint.From(GlobalPosition), Heading = InTransit ? _motion.Heading : _steering.Heading,
        Motion = InTransit ? _motion.CopyFlight() : null,
        Velocity = SavePoint.From(_steering.Velocity), ManoeuvrePhase = _steering.Phase,
        WeaponCooldown = _weapon?.CaptureCooldown() ?? 0
    };
    internal void Restore(FighterDroneSaveData data)
    {
        if (data.Health > MaxHealth || data.Ammo > AmmoCapacity) throw new System.IO.InvalidDataException("Fighter exceeds current capacity.");
        Health = data.Health; Ammo = data.Ammo; State = data.State; GlobalPosition = data.Position.Vector();
        _motion = data.Motion?.CopyFlight() ?? new DroneFlightMotion { Heading = data.Heading };
        _steering.Heading = data.Heading; _steering.Velocity = data.Velocity.Vector(); _steering.Phase = data.ManoeuvrePhase;
        _target = null; _steering.ResetPass(); _search = Slot * .08f;
        _weapon?.RestoreCooldown(data.WeaponCooldown);
        _body.Reset(); Present(0, false);
    }
}
