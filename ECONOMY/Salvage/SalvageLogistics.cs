// One mission-owned clock for physical scrap, refinery stock and shared-body logistics flights.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SalvageLogistics : Node2D
{
    public const string MetalId = "refined_metal";
    [Export] public SalvageConfig Config { get; set; }
    [Export] public PackedScene DroneScene { get; set; }
    [Export] public PackedScene DroneBodyScene { get; set; }
    private enum Leg { Waiting, Pickup, Grab, RawDelivery, RawHold, MetalDelivery, MaterialPickup, SiteDelivery, Assist, Return, ReturnMetal, Requeue }
    private enum Role { Salvage, Courier, Builder }
    private sealed class Drop
    {
        public long Id;
        public Vector2 Position;
        public double Remaining;
        public bool Reserved;
    }
    private sealed class Job
    {
        public Role Role;
        public Leg Leg;
        public CitadelDestination Home;
        public ScrapRefinery Refinery;
        public ResourceStorage Storage;
        public TowerBase Site;
        public Drop Drop;
        public int Cargo, Required;
        public double Hover;
        public DroneFlightMotion Motion;
        public Node2D Drone;
        public DroneBody Body;
        public ConstructionBeam Beam;
        public Action Completed;
    }

    private readonly List<Drop> _drops = new();
    private readonly List<Job> _jobs = new();
    private readonly List<ScrapRefinery> _refineries = new();
    private readonly List<ResourceStorage> _stores = new();
    private readonly Dictionary<CitadelDestination, int> _holding = new();
    private readonly RandomNumberGenerator _random = new();
    private EnemyRegistry _enemies;
    private ConstructionDroneManager _builders;
    private CitadelSession _citadels;
    private Node2D _towers;
    private double _retry;
    private long _nextDrop, _lostScrap, _lostMetal;
    private bool _stopped;
    public event Action StockChanged;
    public long StoredMetal => _stores.Where(Live).Sum(s => (long)s.Metal);
    private int FlightLimit => Math.Clamp(Config.FlightLimit, 1, 64);
    private int HoldingCapacity => Math.Clamp(Config.CitadelScrapCapacity, 1, 100);
    private float Speed => float.IsFinite(Config.FlightSpeed) ? Mathf.Clamp(Config.FlightSpeed, 32, 2000) : 280;
    private ConstructionDroneMasterConfig BuilderConfig => _builders.Config;

    public override void _Ready()
    {
        Config ??= new SalvageConfig();
        SetPhysicsProcess(false);
        _enemies = GetNode<EnemyRegistry>("../EnemySystems/Registry");
        _builders = GetNode<ConstructionDroneManager>("../ConstructionDrones");
        _citadels = GetNodeOrNull<CitadelSession>("../../CitadelSession");
        _towers = GetNode<Node2D>("../Towers");
        _towers.ChildEnteredTree += Track;
        _towers.ChildExitingTree += Untrack;
        foreach (Node child in _towers.GetChildren()) Track(child);
        _random.Randomize();
        _enemies.EnemyKilled += OnKill;
    }

    private static bool Live(TowerBase tower) => GodotObject.IsInstanceValid(tower) && tower.IsInsideTree() &&
        !tower.IsQueuedForDeletion() && !tower.IsDestroyed;
    private static bool Operational(TowerBase tower) => Live(tower) && tower.CanOperate;
    private static bool LiveHome(CitadelDestination home) => home?.Available == true &&
        GodotObject.IsInstanceValid(home.Root) && !home.Root.IsQueuedForDeletion() && !home.Health.IsDestroyed;

    private void Track(Node child)
    {
        if (child is ScrapRefinery refinery) _refineries.Add(refinery);
        if (child is ResourceStorage storage) { _stores.Add(storage); storage.DisableStateChanged += NotifyStock; storage.WorkCompleted += NotifyStock; }
    }
    private void Untrack(Node child)
    {
        if (child is ScrapRefinery refinery && _refineries.Remove(refinery) && !_stopped)
        { _lostScrap += refinery.Scrap + (refinery.Processing ? 1 : 0); _lostMetal += refinery.Metal; }
        if (child is ResourceStorage storage)
        {
            if (_stores.Remove(storage) && !_stopped) _lostMetal += storage.Metal;
            storage.DisableStateChanged -= NotifyStock; storage.WorkCompleted -= NotifyStock;
            NotifyStock();
        }
    }
    private void NotifyStock()
    {
        if (!_stopped) StockChanged?.Invoke();
    }

    private void OnKill(Enemy enemy)
    {
        if (_stopped || GetTree().Paused) return;
        float chance = float.IsFinite(Config.ScrapChance) ? Mathf.Clamp(Config.ScrapChance, 0, 1) : .01f;
        if (_random.Randf() >= chance && chance < 1f) return;
        if (_drops.Count >= Math.Clamp(Config.DropLimit, 1, 64)) return;
        _drops.Add(new Drop { Id = ++_nextDrop, Position = enemy.GlobalPosition,
            Remaining = float.IsFinite(Config.DropLifetime) ? Mathf.Clamp(Config.DropLifetime, 1, 600) : 120 });
        QueueRedraw(); SetPhysicsProcess(true);
    }

    public override void _Draw()
    {
        foreach (Drop drop in _drops)
        {
            Vector2 p = ToLocal(drop.Position);
            DrawCircle(p, 8, new Color(1, .64f, .24f, .18f));
            DrawLine(p - new Vector2(3, 2), p + new Vector2(3, 2), new Color("#ffce7e"), 3, true);
        }
    }

    // =========================================================
    // Construction reserves one storage's stock inside the paid-placement transaction.
    internal static bool IsMetal(ResourceCost cost) => cost?.Resource?.ResourceId == MetalId;
    internal string PlacementFailure(int amount) => amount <= 0 ? "" :
        amount > Math.Clamp(Config.BuilderCapacity, 1, 100) ? "Material delivery exceeds drone capacity." :
        !_builders.CanGate ? "Construction drones are unavailable." :
        !_stores.Any(s => Operational(s) && s.Available >= amount) ? "Insufficient Refined Metal in storage." : "";

    internal bool ReserveConstruction(TowerBase site, int amount, Vector2 worldCenter)
    {
        if (_stopped || PlacementFailure(amount).Length != 0) return false;
        var storage = FindStorage(worldCenter, amount, supply: true);
        if (storage == null) return false;
        var job = new Job { Role = Role.Builder, Leg = Leg.Waiting, Site = site, Storage = storage, Required = amount };
        site.RequireMetal(amount, float.IsFinite(Config.PreparationFraction) ? Math.Clamp(Config.PreparationFraction, 0, .95f) : .25);
        storage.Reserved += amount;
        AttachSite(job); _jobs.Add(job); SetPhysicsProcess(true);
        return true;
    }

    internal void PublishReservation(TowerBase site)
    {
        var job = _jobs.Find(j => j.Site == site);
        if (job == null) return;
        job.Storage?.NotifyRuntimeChanged(); NotifyStock();
    }

    internal void RollbackConstruction(TowerBase site)
    {
        var job = _jobs.Find(j => j.Site == site);
        if (job == null) return;
        ReleaseStock(job); DetachSite(job); _jobs.Remove(job);
    }

    internal string DescribeConstruction(TowerBase site)
    {
        var job = _jobs.Find(j => j.Site == site);
        return job?.Leg switch
        {
            Leg.MaterialPickup => "MATERIAL DRONE EN ROUTE TO STORAGE",
            Leg.Requeue => "WAITING FOR METAL · DRONE RETURNING",
            Leg.SiteDelivery => $"MATERIAL DRONE EN ROUTE · {job.Cargo}/{job.Required} METAL",
            Leg.Assist => site.AwaitingConstructionDrone ? $"METAL {job.Required}/{job.Required} DELIVERED" : "BOTH DRONES CONSTRUCTING",
            _ => "WAITING FOR REFINED METAL"
        };
    }

    private void AttachSite(Job job)
    {
        job.Completed = () => { if (!_stopped && job.Leg == Leg.Assist) Return(job); };
        job.Site.WorkCompleted += job.Completed;
    }
    private void DetachSite(Job job)
    {
        if (job.Completed != null && GodotObject.IsInstanceValid(job.Site)) job.Site.WorkCompleted -= job.Completed;
        job.Completed = null; job.Site = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_stopped || GetTree().Paused || delta <= 0 || !double.IsFinite(delta)) return;
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            Drop drop = _drops[i];
            if (drop.Reserved) continue;
            drop.Remaining -= delta;
            if (drop.Remaining > 0) continue;
            _drops.RemoveAt(i); QueueRedraw();
        }
        if (_holding.Count > 0 && _citadels?.Objectives != null)
            foreach (var home in _citadels.Objectives)
                if (!LiveHome(home) && _holding.Remove(home, out int lost)) _lostScrap += lost;
        foreach (ScrapRefinery refinery in _refineries)
        {
            if (!Operational(refinery) || refinery.MetalRoom < refinery.Yield || (!refinery.Processing && refinery.Scrap == 0)) continue;
            if (!refinery.Processing)
            {
                refinery.Scrap--; refinery.Processing = true; refinery.BatchElapsed = 0;
                refinery.NotifyRuntimeChanged();
            }
            double prior = refinery.BatchElapsed;
            refinery.BatchElapsed += delta;
            if (refinery.BatchElapsed + 1e-9 >= refinery.Duration)
            {
                refinery.Metal += refinery.Yield; refinery.Processing = false; refinery.BatchElapsed = 0;
                refinery.NotifyRuntimeChanged();
            }
            else if ((int)(prior * 5) != (int)(refinery.BatchElapsed * 5)) refinery.NotifyRuntimeChanged();
        }
        for (int i = _jobs.Count - 1; i >= 0; i--)
            if (!Tick(_jobs[i], (float)delta)) Retire(i);
        _retry -= delta;
        if (_retry <= 0)
        {
            _retry = .25;
            Dispatch();
        }
        SetPhysicsProcess(_drops.Count > 0 || _jobs.Count > 0 || _holding.Count > 0 ||
            _refineries.Any(r => Live(r) && r.HasWork));
    }

    private void Dispatch()
    {
        int active = _jobs.Count(j => j.Drone != null);
        foreach (Job order in _jobs)
        {
            if (active >= FlightLimit) break;
            if (order.Leg != Leg.Waiting || !Live(order.Site)) continue;
            if (!Operational(order.Storage))
            {
                ReleaseStock(order);
                order.Storage = FindStorage(order.Site.GlobalPosition, order.Required, supply: true);
                if (order.Storage == null) continue;
                order.Storage.Reserved += order.Required;
                order.Storage.NotifyRuntimeChanged(); NotifyStock();
            }
            order.Home = _builders.NearestHome(order.Site.GlobalPosition);
            if (!LiveHome(order.Home)) continue;
            Launch(order, Leg.MaterialPickup, _builders.LaunchPoint(order.Home), order.Storage.GlobalPosition);
            order.Site.NotifyRuntimeChanged(); active++;
        }
        foreach (ScrapRefinery refinery in _refineries)
        {
            if (active >= FlightLimit) break;
            if (!Operational(refinery) || refinery.Metal <= 0 || _jobs.Any(j => j.Role == Role.Courier && j.Refinery == refinery)) continue;
            var storage = FindStorage(refinery.GlobalPosition, 1, supply: false);
            if (storage == null) continue;
            int amount = Math.Min(Math.Clamp(Config.CourierCapacity, 1, 100), Math.Min(refinery.Metal, storage.Room));
            var job = new Job { Role = Role.Courier, Refinery = refinery, Storage = storage, Cargo = amount };
            refinery.Metal -= amount; refinery.ReturningMetal += amount; refinery.Outbound = amount; storage.Incoming += amount;
            Launch(job, Leg.MetalDelivery, refinery.GlobalPosition, storage.GlobalPosition);
            _jobs.Add(job); active++;
            refinery.NotifyRuntimeChanged(); storage.NotifyRuntimeChanged();
        }
        if (_citadels?.Objectives == null) return;
        foreach (var home in _citadels.Objectives)
        {
            if (active >= FlightLimit || !LiveHome(home)) continue;
            int occupied = _jobs.Count(j => j.Role == Role.Salvage && j.Home == home);
            while (active < FlightLimit && occupied < Math.Clamp(Config.SalvagePerCitadel, 1, 8))
            {
                Vector2 point = _builders.LaunchPoint(home);
                bool hasHeld = _holding.GetValueOrDefault(home) > 0;
                Drop drop = hasHeld ? null : _drops.Find(d => !d.Reserved);
                if (!hasHeld && drop == null) break;
                var refinery = FindRefinery(drop?.Position ?? point);
                if (refinery == null) break;
                refinery.RawIncoming++; refinery.NotifyRuntimeChanged();
                var job = new Job { Role = Role.Salvage, Home = home, Refinery = refinery, Drop = drop };
                if (drop != null)
                {
                    drop.Reserved = true; Launch(job, Leg.Pickup, point, drop.Position);
                }
                else
                {
                    ChangeHolding(home, -1); job.Cargo = 1;
                    Launch(job, Leg.RawDelivery, point, refinery.GlobalPosition);
                }
                _jobs.Add(job); active++; occupied++;
            }
        }
    }

    private ResourceStorage FindStorage(Vector2 from, int amount, bool supply)
    {
        ResourceStorage best = null; float distance = float.PositiveInfinity;
        foreach (var storage in _stores)
        {
            if (!Operational(storage) || (supply ? storage.Available : storage.Room) < amount) continue;
            float next = from.DistanceSquaredTo(storage.GlobalPosition);
            if (next < distance) { best = storage; distance = next; }
        }
        return best;
    }
    private ScrapRefinery FindRefinery(Vector2 from)
    {
        ScrapRefinery best = null; float distance = float.PositiveInfinity;
        foreach (var refinery in _refineries)
        {
            if (!Operational(refinery) || refinery.ScrapRoom <= 0) continue;
            float next = from.DistanceSquaredTo(refinery.GlobalPosition);
            if (next < distance) { best = refinery; distance = next; }
        }
        return best;
    }
    private void ChangeHolding(CitadelDestination home, int amount)
    {
        int next = _holding.GetValueOrDefault(home) + amount;
        if (next > 0) _holding[home] = next; else _holding.Remove(home);
    }
}
