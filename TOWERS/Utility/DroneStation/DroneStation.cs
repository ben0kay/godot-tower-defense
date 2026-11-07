// Buildable hangar owns physical ammunition and paid fighters; the existing courier network supplies dock service.
using Godot;
using System.Collections.Generic;

public partial class DroneStation : TowerBase, IAmmoDeliveryConsumer
{
    [Export] public DroneStationConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;
    public FighterDroneManager Fighters { get; private set; }
    public EconomyManager Wallet { get; private set; }
    public AmmoInventory DeliveryInventory { get; private set; }
    public bool CanReceiveAmmo => CanOperate && !IsBusy;
    public bool AmmoRequested { get; private set; }
    public bool AmmoInFlight { get; private set; }
    private AmmunitionLogistics _logistics;
    private LevelSessionController _session;
    public void SetAmmoDeliveryStatus(bool requested, bool inFlight)
    {
        if (AmmoRequested == requested && AmmoInFlight == inFlight) return;
        AmmoRequested = requested; AmmoInFlight = inFlight; NotifyRuntimeChanged();
    }
    private void StockChanged() { if (GodotObject.IsInstanceValid(Fighters)) Fighters.WakeService(); NotifyRuntimeChanged(); }
    private void RefreshStationService()
    { if (GodotObject.IsInstanceValid(_logistics) && IsOperational) _logistics.RefreshConsumer(this); if (GodotObject.IsInstanceValid(Fighters)) Fighters.WakeService(); }
    protected override void OnBecameOperational() { base.OnBecameOperational(); RefreshStationService(); }

    public override bool AcceptsConfig(TowerConfig config) => config is DroneStationConfig station && station.ValidService;
    protected override void ApplyConfig(TowerConfig config) => Config = (DroneStationConfig)config;
    public override bool ValidateScene() => Config?.ValidService == true && Config.FighterConfig != null && Config.FighterScene != null &&
        GetNodeOrNull<Sprite2D>("Sprite") != null && GetNodeOrNull<Marker2D>("DockPoints/Dock3") != null;
    public override void _Ready()
    {
        SetProcess(false); SetOperationalPhysicsProcess(false);
        if (!Config.ValidService) throw new System.IO.InvalidDataException("Invalid station service configuration.");
        DeliveryInventory = new AmmoInventory(Config.StationAmmoCapacity); // Always empty; no construction/load gift.
        DeliveryInventory.Changed += StockChanged;
        _logistics = GetNodeOrNull<AmmunitionLogistics>("../../AmmunitionLogistics");
        Wallet = GetNodeOrNull<EconomyManager>("../../../Economy");
        _session = GetNodeOrNull<LevelSessionController>("../../../LevelSession");
        WorldGrid grid = GetNodeOrNull<WorldGrid>("../../Grid");
        if (grid == null) return; // Standalone station art preview has no gameplay world.
        Fighters = new FighterDroneManager { Name = "Fighters" };
        AddChild(Fighters); Fighters.Bind(this, grid); RefreshStationService();
    }
    internal bool CanCommand => IsInsideTree() && !IsQueuedForDeletion() && !IsDestroyed && !GetTree().Paused &&
        Fighters != null && (_session == null || _session.State == LevelSessionState.Playing);
    private Godot.Collections.Array<ResourceCost> Price() => new()
    {
        new ResourceCost { Resource = Wallet.CreditsResource, Amount = Mathf.Clamp(Config.FighterCredits, 0, 1000000) }
    };
    public bool CanBuyFighter => CanCommand && CanOperate && !IsBusy && Wallet != null &&
        Fighters.FreeSlot() >= 0 && Wallet.CanAfford(Price());

    // =========================================================
    // Actual wallet commit owns the purchase; failed/full/unready stations never spend Credits or create a duplicate.
    public bool TryBuyFighter(out FighterDrone fighter, out string error)
    {
        fighter = null; error = "";
        if (!CanBuyFighter) { error = "Station unavailable, slots full or insufficient Credits."; return false; }
        FighterDrone created = null;
        if (!Wallet.TryPurchase(Price(), () => Fighters.TryCreate(out created)))
        { error = "Fighter purchase could not be completed."; return false; }
        fighter = created; NotifyRuntimeChanged(); return true;
    }
    public bool LaunchFighter(int slot) => CanCommand && CanOperate && !IsBusy && Fighters.Launch(slot);
    public bool RecallFighter(int slot) => CanCommand && Fighters.Recall(slot);
    protected override void OnDisableChanged()
    { base.OnDisableChanged(); if (!CanOperate && GodotObject.IsInstanceValid(Fighters)) Fighters.RecallAll(); RefreshStationService(); }
    // Tint the building only: its owned fighters are independently presented.
    public override void SetSelected(bool selected)
    { if (GetNodeOrNull<Sprite2D>("Sprite") is Sprite2D sprite) sprite.Modulate = selected ? new Color(1, .9f, .68f) : Colors.White; }
    public override string DescribeRuntime() => IsBusy ? "Hangar under construction" :
        $"Fighters: {Fighters?.Count ?? 0}/{Mathf.Clamp(Config.FighterCapacity, 1, 4)} · Launch / Recall";
    public string DescribeAmmo() => $"Station ammo {DeliveryInventory?.Amount ?? 0}/{DeliveryInventory?.Capacity ?? Config.StationAmmoCapacity} · " +
        (AmmoInFlight ? "Courier in flight" : AmmoRequested ? "Awaiting depot delivery" : "Stock ready");
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_logistics)) _logistics.RemoveConsumer(this);
        if (DeliveryInventory != null) DeliveryInventory.Changed -= StockChanged;
        base._ExitTree();
    }
    internal FighterStationSaveData CaptureFighters() => Fighters?.Capture() ?? new();
    internal void RestoreFighters(FighterStationSaveData data)
    {
        if (Fighters == null && data?.Fighters.Count > 0) throw new System.IO.InvalidDataException("Fighter station has no world manager.");
        Fighters?.Restore(data);
    }
}
