// Persistent friendly fighter body/state. Its station manager alone drives finite launch/return legs in FD1-B.
using Godot;
using System;

public enum FighterDroneState { Docked, Launching, Holding, Returning }
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
    public bool IsFlying => State is FighterDroneState.Launching or FighterDroneState.Returning;
    private DroneFlightMotion _motion = new() { Heading = -Mathf.Pi / 2 };
    private DroneBody _body;
    private Vector2 _initialPosition;

    internal void Initialize(FighterDroneConfig config, int slot, Vector2 position, string id = null)
    {
        Config = config; Slot = slot; SaveId = id ?? Guid.NewGuid().ToString("N");
        Health = MaxHealth; Ammo = AmmoCapacity; _initialPosition = position;
    }
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _body = GetNode<DroneBody>("Body"); GlobalPosition = _initialPosition; Present(0, false);
    }

    // =========================================================
    // A new finite leg retains real HP/ammo. No launch/landing event replenishes either reservoir.
    internal void BeginTransit(Vector2 destination, bool returning)
    {
        Vector2 direction = destination - GlobalPosition;
        _motion.BeginFlight(GlobalPosition, destination, direction, direction,
            FighterDroneConfig.Safe(Config.FlightSpeed, 340, 32, 2000), 100,
            Config.Acceleration, Config.Deceleration);
        State = returning ? FighterDroneState.Returning : FighterDroneState.Launching;
        Present(0, true);
    }
    internal bool Step(float dt)
    {
        if (!IsFlying) return false;
        Vector2 point = GlobalPosition;
        bool arrived = _motion.Fly(dt, ref point, Config.TurnDegreesPerSecond); GlobalPosition = point;
        if (arrived) State = State == FighterDroneState.Returning ? FighterDroneState.Docked : FighterDroneState.Holding;
        Present(dt, true); return arrived;
    }
    private void Present(float dt, bool ignite) => _body?.Present(_motion.Heading,
        IsFlying ? _motion.Velocity.Vector() : Vector2.Zero, FighterDroneConfig.Safe(Config.FlightSpeed, 340, 32, 2000),
        IsFlying, State == FighterDroneState.Returning ? 1 : 0, null, dt, ignite);

    // =========================================================
    // Only real source state is serialized; the arc-length table remains a derived, lazily rebuilt cache.
    internal FighterDroneSaveData Capture() => new()
    {
        Id = SaveId, Slot = Slot, Health = Health, Ammo = Ammo, State = State,
        Position = SavePoint.From(GlobalPosition), Heading = _motion.Heading,
        Motion = IsFlying ? _motion.CopyFlight() : null
    };
    internal void Restore(FighterDroneSaveData data)
    {
        if (data.Health > MaxHealth || data.Ammo > AmmoCapacity) throw new System.IO.InvalidDataException("Fighter exceeds current capacity.");
        Health = data.Health; Ammo = data.Ammo; State = data.State; GlobalPosition = data.Position.Vector();
        _motion = data.Motion?.CopyFlight() ?? new DroneFlightMotion { Heading = data.Heading };
        _body.Reset(); Present(0, false);
    }
}
