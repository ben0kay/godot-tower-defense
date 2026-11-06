// Station-owned FIFO dock service: real stock transfer, prepaid fractional repair credit and explicit return intent.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class FighterDroneManager
{
    private readonly FighterServiceSaveData[] _service = new FighterServiceSaveData[4];
    private readonly FighterServiceVisual[] _meters = new FighterServiceVisual[4];
    private readonly bool[] _activeBays = new bool[4];
    private double _serviceElapsed;
    private long _serviceSerial;
    private bool _servicing;

    internal void WakeService()
    {
        if (_shutdown || !IsInsideTree()) return;
        RefreshProcessing(); RefreshMeters();
    }
    private void WalletChanged(string resource, int amount) => WakeService();
    private void BindService()
    {
        _station.DeliveryInventory.Changed += WakeService;
        if (GodotObject.IsInstanceValid(_station.Wallet)) _station.Wallet.BalanceChanged += WalletChanged;
    }
    private void UnbindService()
    {
        if (GodotObject.IsInstanceValid(_station))
        {
            _station.DeliveryInventory.Changed -= WakeService;
            if (GodotObject.IsInstanceValid(_station.Wallet)) _station.Wallet.BalanceChanged -= WalletChanged;
        }
    }
    private void RegisterService(FighterDrone fighter)
    {
        _service[fighter.Slot] = new();
        var meter = new FighterServiceVisual { Name = "ServiceMeter" };
        fighter.AddChild(meter); _meters[fighter.Slot] = meter;
    }
    private void BeginDockService(FighterDrone fighter)
    {
        var job = _service[fighter.Slot];
        if (job.Reason == FighterReturnReason.None || job.VisitStarted) return;
        job.VisitStarted = true; job.MissingAtDock = fighter.Reserve.FreeSpace;
        job.AmmoTransferred = 0; job.AmmoFraction = 0; job.QueueOrder = ++_serviceSerial;
    }
    private bool WantsAutoReturn(FighterDrone fighter)
    {
        var job = _service[fighter.Slot];
        if (fighter.Ammo == 0) return true;
        if (job.ManualResourceOverride) return false;
        var config = fighter.Config;
        float ammo = FighterDroneConfig.Safe(config.AutoReturnAmmoFraction, .25f, 0, .9f);
        float hp = FighterDroneConfig.Safe(config.AutoReturnHealthFraction, .35f, 0, .9f);
        return ammo > 0 && fighter.Ammo <= fighter.AmmoCapacity * ammo ||
            hp > 0 && fighter.Health <= fighter.MaxHealth * hp;
    }
    private bool RepairAvailable(FighterDrone fighter, FighterServiceSaveData job)
    {
        var config = _station.Config;
        return fighter.Health < fighter.MaxHealth && config.RepairHpPerSecond > 0 && config.RepairCreditsPer10Hp > 0 &&
            (job.RepairCredit > .000001 || GodotObject.IsInstanceValid(_station.Wallet) &&
                _station.Wallet.GetBalance(_station.Wallet.CreditsResource) > 0);
    }
    private bool HasServiceWork()
    {
        if (!_station.CanOperate || _station.IsBusy || !_station.Config.ValidService) return false;
        for (int i = 0; i < 4; i++)
        {
            var fighter = _slots[i]; var job = _service[i];
            if (fighter == null || job == null || job.Reason == FighterReturnReason.None || fighter.State != FighterDroneState.Docked) continue;
            if (fighter.Reserve.FreeSpace == 0 && fighter.Health >= fighter.MaxHealth) return true;
            if (fighter.Reserve.FreeSpace > 0 && _station.DeliveryInventory.Amount > 0 || RepairAvailable(fighter, job)) return true;
        }
        return false;
    }

    // =========================================================
    // A bay is occupied only by actionable work; stock/Credits arrivals wake dormant waiters.
    private bool ServiceStep(double delta)
    {
        if (_servicing || !_station.CanOperate || _station.IsBusy || !_station.Config.ValidService) return false;
        if (!HasServiceWork()) return false;
        _serviceElapsed += delta;
        if (_serviceElapsed < .1) return false;
        double seconds = Math.Min(.25, _serviceElapsed); _serviceElapsed = 0;
        _servicing = true; Array.Clear(_activeBays);
        bool changed = false;
        try
        {
            for (int bay = 0; bay < Math.Clamp(_station.Config.ServiceBayCount, 1, 4); bay++)
            {
                int slot = -1; long order = long.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    var f = _slots[i]; var j = _service[i];
                    if (f == null || j == null || _activeBays[i] || f.State != FighterDroneState.Docked || j.Reason == FighterReturnReason.None) continue;
                    BeginDockService(f);
                    if (f.Reserve.FreeSpace == 0 && f.Health >= f.MaxHealth)
                    {
                        bool relaunch = j.Reason == FighterReturnReason.AutoService;
                        ResetVisit(i);
                        if (relaunch) Launch(i, true);
                        changed = true; continue;
                    }
                    bool actionable = f.Reserve.FreeSpace > 0 && _station.DeliveryInventory.Amount > 0 || RepairAvailable(f, j);
                    if (actionable && j.QueueOrder < order) { slot = i; order = j.QueueOrder; }
                }
                if (slot < 0) break;
                _activeBays[slot] = true;
                var fighter = _slots[slot]; var job = _service[slot];
                if (fighter.Reserve.FreeSpace > 0 && _station.DeliveryInventory.Amount > 0)
                {
                    double tokens = job.AmmoFraction + seconds * fighter.AmmoCapacity / _station.Config.FullFighterResupplySeconds;
                    int whole = (int)Math.Min(10000, Math.Floor(tokens));
                    int rounds = Math.Min(whole, Math.Min(fighter.Reserve.FreeSpace, _station.DeliveryInventory.Amount));
                    job.AmmoFraction = Math.Clamp(tokens - Math.Floor(tokens), 0, .999999999);
                    if (rounds > 0 && _station.DeliveryInventory.TryTransferTo(fighter.Reserve, rounds))
                    { job.AmmoTransferred += rounds; changed = true; }
                }
                if (RepairAvailable(fighter, job)) changed |= Repair(fighter, job, seconds);
            }
        }
        finally { _servicing = false; RefreshMeters(); }
        return changed;
    }
    // =========================================================
    // Charge whole Credits up front, retaining unused fractional credit on the fighter across every visit.
    private bool Repair(FighterDrone fighter, FighterServiceSaveData job, double seconds)
    {
        var wallet = _station.Wallet; var config = _station.Config;
        double price = config.RepairCreditsPer10Hp / 10.0;
        double funds = job.RepairCredit + (GodotObject.IsInstanceValid(wallet) ? Math.Max(0, wallet.GetBalance(wallet.CreditsResource)) : 0);
        float request = (float)Math.Min(config.RepairHpPerSecond * seconds, funds / price);
        float heal = Mathf.Max(0, Mathf.Min(fighter.MaxHealth, fighter.Health + request) - fighter.Health);
        if (heal <= 0) return false;
        double cost = heal * price;
        int credits = (int)Math.Ceiling(Math.Max(0, cost - job.RepairCredit) - 1e-9);
        bool Commit()
        {
            job.RepairCredit = Math.Clamp(job.RepairCredit + credits - cost, 0, .999999999);
            fighter.RepairHealth(heal); return true;
        }
        if (credits == 0) return Commit();
        return wallet.TryPurchase(new Godot.Collections.Array<ResourceCost>
            { new ResourceCost { Resource = wallet.CreditsResource, Amount = credits } }, Commit);
    }
    private void ResetVisit(int slot)
    {
        var old = _service[slot];
        _service[slot] = new() { RepairCredit = old?.RepairCredit ?? 0 };
        _activeBays[slot] = false;
        if (GodotObject.IsInstanceValid(_meters[slot])) _meters[slot].Present(false, false, 0);
    }
    private void RefreshMeters()
    {
        for (int i = 0; i < 4; i++)
        {
            var fighter = _slots[i]; var job = _service[i]; var meter = _meters[i];
            if (fighter == null || job == null || !GodotObject.IsInstanceValid(meter)) continue;
            bool docked = fighter.State == FighterDroneState.Docked && job.VisitStarted && job.Reason != FighterReturnReason.None;
            bool waiting = docked && fighter.Reserve.FreeSpace > 0 && _station.DeliveryInventory.Amount == 0;
            bool visible = docked && _station.CanOperate && fighter.Reserve.FreeSpace > 0 && (waiting || _activeBays[i]);
            float progress = job.MissingAtDock > 0 ? (float)job.AmmoTransferred / job.MissingAtDock : 1;
            meter.Present(visible, waiting, progress);
        }
    }
    public string DescribeService(int slot)
    {
        var f = At(slot);
        if (f == null) return "EMPTY";
        var j = _service[slot];
        string reason = j?.Reason == FighterReturnReason.AutoService ? "AUTO" :
            j?.Reason == FighterReturnReason.StationDisabled ? "STATION RECALL" : "MANUAL";
        if (f.State == FighterDroneState.Returning) return "RETURNING · " + reason;
        if (f.State != FighterDroneState.Docked) return "DEPLOYED";
        if (j?.Reason == FighterReturnReason.None) return f.Ammo > 0 ? "DOCKED READY" : "DOCKED · NO AMMO";
        if (!_station.CanOperate) return "SERVICE PAUSED";
        if (f.Reserve.FreeSpace > 0)
        {
            if (_station.DeliveryInventory.Amount == 0) return "WAITING FOR AMMO · " + reason;
            return _activeBays[slot] ? $"REARMING {(j.MissingAtDock > 0 ? 100.0 * j.AmmoTransferred / j.MissingAtDock : 100):0}% · {reason}" : "WAITING FOR SERVICE BAY";
        }
        if (f.Health < f.MaxHealth)
            return _station.Config.RepairHpPerSecond <= 0 || _station.Config.RepairCreditsPer10Hp <= 0 ? "REPAIR UNAVAILABLE" :
                RepairAvailable(f, j) ? (_activeBays[slot] ? "REPAIRING · " + reason : "WAITING FOR SERVICE BAY") : "WAITING FOR REPAIR CREDITS";
        return "DOCKED READY";
    }
    internal bool IsAutoReturning(int slot) => _service[slot]?.Reason == FighterReturnReason.AutoService;
    // =========================================================
    // Optional legacy fields restore no job; new jobs must reconcile with their actual physical reserve.
    private static void ValidateServiceSnapshot(FighterDroneSaveData fighter, long serial)
    {
        if (fighter.RoundsFired.HasValue && (fighter.RoundsFired < 0 || fighter.RoundsFired > 1000000000000000))
            throw new InvalidDataException("Invalid fighter shot parity.");
        var j = fighter.Service; if (j == null) return;
        if (!Enum.IsDefined(j.Reason) || !double.IsFinite(j.AmmoFraction) || j.AmmoFraction < 0 || j.AmmoFraction >= 1 ||
            !double.IsFinite(j.RepairCredit) || j.RepairCredit < 0 || j.RepairCredit >= 1 ||
            j.MissingAtDock < 0 || j.MissingAtDock > 10000 || j.AmmoTransferred < 0 || j.AmmoTransferred > j.MissingAtDock ||
            j.QueueOrder < 0 || j.QueueOrder > serial ||
            (j.VisitStarted != (j.QueueOrder > 0)) ||
            (j.Reason != FighterReturnReason.None && fighter.State is not (FighterDroneState.Docked or FighterDroneState.Returning)) ||
            (j.VisitStarted && (fighter.State != FighterDroneState.Docked || j.Reason == FighterReturnReason.None)) ||
            (!j.VisitStarted && (j.MissingAtDock != 0 || j.AmmoTransferred != 0 || j.AmmoFraction != 0)) ||
            (j.ManualResourceOverride && (j.Reason != FighterReturnReason.None || fighter.State == FighterDroneState.Returning)))
            throw new InvalidDataException("Invalid fighter service source state.");
    }
    private static void ValidateServiceCapacity(FighterDrone fighter, FighterServiceSaveData job)
    {
        if (job.MissingAtDock > fighter.AmmoCapacity || job.VisitStarted &&
            fighter.Reserve.FreeSpace + job.AmmoTransferred != job.MissingAtDock)
            throw new InvalidDataException("Saved service progress differs from actual fighter ammunition.");
    }

}
