// Restore stock and job ownership before the paused battlefield is published.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class SalvageLogistics
{
    internal void SettleCheckpointOwnership()
    {
        for (int i = _jobs.Count - 1; i >= 0; i--)
            if (!Tick(_jobs[i], 0)) Retire(i);
    }

    internal SalvageSaveData CaptureCheckpoint() => new()
    {
        RandomState = _random.State, NextDrop = _nextDrop, LostScrap = _lostScrap, LostMetal = _lostMetal,
        Drops = _drops.Select(d => new ScrapDropSaveData(d.Id, SavePoint.From(d.Position), d.Remaining)).ToList(),
        Stocks = _refineries.Where(Live).Select(r => new MaterialStockSaveData(r.SaveId, r.Scrap, r.Metal, r.Processing, r.BatchElapsed))
            .Concat(_stores.Where(Live).Select(s => new MaterialStockSaveData(s.SaveId, 0, s.Metal, false, 0))).ToList(),
        Jobs = _jobs.Select(j => new MaterialJobSaveData((int)j.Role, (int)j.Leg, j.Home?.Id ?? "",
            Live(j.Refinery) ? j.Refinery.SaveId : "", Live(j.Storage) ? j.Storage.SaveId : "",
            Live(j.Site) ? j.Site.SaveId : "", j.Drop?.Id ?? 0, j.Cargo, j.Required, j.Hover,
            SavePoint.From(j.Drone?.GlobalPosition ?? Vector2.Zero), j.Motion?.CopyFlight())).ToList(),
        Holding = _holding.ToDictionary(pair => pair.Key.Id, pair => pair.Value, StringComparer.Ordinal)
    };

    internal void RestoreCheckpoint(SalvageSaveData data, IReadOnlyDictionary<string, TowerBase> towers)
    {
        if (data == null)
        {
            Require(!towers.Values.Any(t => t.RequiredMetal > 0), "Missing material orders.");
            return;
        }
        Require(data.Drops != null && data.Stocks != null && data.Jobs != null && data.Holding != null &&
            data.Drops.Count <= Math.Clamp(Config.DropLimit, 1, 64) && data.Stocks.Count <= towers.Count &&
            data.Jobs.Count <= towers.Count + FlightLimit && data.Holding.Count <= 64 &&
            data.NextDrop >= 0 && data.LostMetal >= 0 && data.LostScrap >= 0, "Invalid salvage totals.");
        _random.State = data.RandomState; _nextDrop = data.NextDrop; _lostScrap = data.LostScrap; _lostMetal = data.LostMetal;
        var homes = _citadels.Objectives.ToDictionary(h => h.Id, StringComparer.Ordinal);
        var drops = new Dictionary<long, Drop>();
        foreach (var saved in data.Drops)
        {
            Require(saved != null && saved.Id > 0 && saved.Id <= _nextDrop && saved.Position.Vector().IsFinite() &&
                double.IsFinite(saved.Remaining) && saved.Remaining > 0 && saved.Remaining <= 600, "Invalid scrap pickup.");
            var drop = new Drop { Id = saved.Id, Position = saved.Position.Vector(), Remaining = saved.Remaining };
            Require(drops.TryAdd(drop.Id, drop), "Duplicate scrap pickup.");
            _drops.Add(drop);
        }
        foreach (var pair in data.Holding)
        {
            Require(homes.TryGetValue(pair.Key, out var home) && LiveHome(home) && pair.Value > 0 &&
                pair.Value <= HoldingCapacity, "Invalid Citadel scrap holding.");
            _holding.Add(home, pair.Value);
        }

        var stocked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var saved in data.Stocks)
        {
            Require(saved != null && saved.Id != null && stocked.Add(saved.Id) && towers.ContainsKey(saved.Id) &&
                saved.Metal >= 0 && saved.Scrap >= 0 && double.IsFinite(saved.BatchElapsed) && saved.BatchElapsed >= 0,
                "Invalid or duplicate material inventory.");
            switch (towers[saved.Id])
            {
                case ScrapRefinery refinery:
                    Require(saved.Scrap <= refinery.ScrapRoom && saved.Metal <= refinery.MetalRoom &&
                        (saved.Processing ? saved.BatchElapsed < refinery.Duration && refinery.IsOperational &&
                            saved.Metal <= refinery.MetalRoom - refinery.Yield : saved.BatchElapsed == 0), "Invalid refinery stock.");
                    refinery.Scrap = saved.Scrap; refinery.Metal = saved.Metal;
                    refinery.Processing = saved.Processing; refinery.BatchElapsed = saved.BatchElapsed;
                    refinery.NotifyRuntimeChanged();
                    break;
                case ResourceStorage storage:
                    Require(saved.Metal <= storage.Capacity && saved.Scrap == 0 && !saved.Processing &&
                        saved.BatchElapsed == 0, "Invalid Resource Storage stock.");
                    storage.Metal = saved.Metal; storage.NotifyRuntimeChanged();
                    break;
                default: throw new InvalidDataException("Material stock belongs to an incompatible building.");
            }
        }
        Require(stocked.Count == _stores.Count + _refineries.Count, "Missing material inventory.");

        var sites = new HashSet<TowerBase>();
        var couriers = new HashSet<ScrapRefinery>();
        var salvageCounts = new Dictionary<CitadelDestination, int>();
        int active = 0;
        foreach (var saved in data.Jobs)
        {
            Require(saved != null && saved.Home != null && saved.Refinery != null && saved.Storage != null && saved.Site != null &&
                Enum.IsDefined((Role)saved.Role) && Enum.IsDefined((Leg)saved.Leg) && saved.Cargo >= 0 && saved.Cargo <= 100 &&
                saved.Required >= 0 && saved.Required <= 100 && saved.Position.Vector().IsFinite() &&
                double.IsFinite(saved.Hover) && saved.Hover >= 0, "Invalid material flight.");
            var job = new Job { Role = (Role)saved.Role, Leg = (Leg)saved.Leg, Cargo = saved.Cargo, Required = saved.Required,
                Hover = saved.Hover, Home = homes.GetValueOrDefault(saved.Home),
                Refinery = Resolve<ScrapRefinery>(saved.Refinery, towers), Storage = Resolve<ResourceStorage>(saved.Storage, towers),
                Site = Resolve<TowerBase>(saved.Site, towers), Drop = drops.GetValueOrDefault(saved.Drop) };
            Require(saved.Home.Length == 0 || LiveHome(job.Home), "Missing material drone home.");
            Require((saved.Drop == 0) == (job.Drop == null), "Missing scrap pickup.");
            ValidateJob(job);
            Require(job.Role != Role.Builder || job.Required <= Math.Clamp(Config.BuilderCapacity, 1, 100), "Material drone capacity exceeded.");
            Require(job.Role != Role.Courier || job.Cargo <= Math.Clamp(Config.CourierCapacity, 1, 100), "Refinery courier capacity exceeded.");
            if (job.Role == Role.Courier && job.Refinery != null)
                Require(couriers.Add(job.Refinery), "Duplicate refinery courier.");
            if (job.Role == Role.Salvage)
            {
                int count = salvageCounts.GetValueOrDefault(job.Home) + 1;
                Require(count <= Math.Clamp(Config.SalvagePerCitadel, 1, 8), "Too many salvage flights at Citadel.");
                salvageCounts[job.Home] = count;
            }
            if (job.Site != null)
            {
                Require(sites.Add(job.Site) && job.Site.RequiredMetal == job.Required &&
                    job.Site.MaterialsDelivered == (job.Leg == Leg.Assist), "Duplicate or inconsistent construction order.");
                AttachSite(job);
            }
            RestoreReservation(job);
            if (job.Leg != Leg.Waiting)
            {
                Require(++active <= FlightLimit && saved.Motion != null, "Missing flight motion or flight cap exceeded.");
                saved.Motion.ValidateFlight();
                Require(ValidEndpoint(job, saved.Motion.End.Vector()), "Material flight endpoint changed.");
                Vector2 position = saved.Position.Vector();
                Launch(job, job.Leg, position, saved.Motion.End.Vector());
                job.Motion = saved.Motion.CopyFlight(); job.Hover = saved.Hover;
                bool flying = job.Leg is not (Leg.Grab or Leg.Assist);
                job.Body.Present(job.Motion.Heading, job.Motion.Velocity.Vector(), Speed, flying, (int)job.Leg, job.Cargo > 0, 0, ignite: false);
            }
            else Require(saved.Motion == null, "Waiting order contains flight motion.");
            _jobs.Add(job);
        }
        Require(towers.Values.All(t => t.RequiredMetal == 0 || sites.Contains(t)), "Missing construction material order.");
        foreach (var refinery in _refineries)
            Require(refinery.ScrapRoom >= 0 && refinery.MetalRoom >= (refinery.Processing ? refinery.Yield : 0), "Refinery reservation overflow.");
        foreach (var storage in _stores)
        {
            Require(storage.Room >= 0 && storage.Available >= 0, "Storage reservation overflow.");
            storage.NotifyRuntimeChanged();
        }
        foreach (var refinery in _refineries) refinery.NotifyRuntimeChanged();
        QueueRedraw(); NotifyStock();
        SetPhysicsProcess(_drops.Count > 0 || _jobs.Count > 0 || _holding.Count > 0 || _refineries.Any(r => r.HasWork));
    }

    private static T Resolve<T>(string id, IReadOnlyDictionary<string, TowerBase> towers) where T : TowerBase
    {
        if (id.Length == 0) return null;
        return towers.GetValueOrDefault(id) as T ?? throw new InvalidDataException("Invalid material job building.");
    }

    private static void ValidateJob(Job job)
    {
        bool valid = job.Role switch
        {
            Role.Salvage => job.Home != null && job.Site == null && job.Storage == null && job.Required == 0 &&
                (job.Leg switch
                {
                    Leg.Pickup or Leg.Grab => job.Drop != null && job.Refinery != null && job.Cargo == 0,
                    Leg.RawDelivery => job.Drop == null && job.Refinery != null && job.Cargo == 1,
                    Leg.RawHold => job.Drop == null && job.Refinery == null && job.Cargo == 1,
                    Leg.Return => job.Drop == null && job.Refinery == null && job.Cargo == 0,
                    _ => false
                }),
            Role.Courier => job.Home == null && job.Drop == null && job.Site == null && job.Required == 0 &&
                (job.Leg switch
                {
                    Leg.MetalDelivery => job.Storage != null && job.Cargo > 0,
                    Leg.ReturnMetal => (job.Storage != null || job.Refinery != null) && job.Cargo > 0,
                    Leg.Return => job.Refinery != null && job.Storage == null && job.Cargo == 0,
                    _ => false
                }),
            Role.Builder => job.Drop == null && job.Refinery == null && job.Required > 0 &&
                (job.Leg switch
                {
                    Leg.Waiting => job.Site != null && job.Cargo == 0,
                    Leg.MaterialPickup => job.Site != null && job.Home != null && job.Storage != null && job.Cargo == 0,
                    Leg.SiteDelivery => job.Site != null && job.Home != null && job.Storage == null && job.Cargo == job.Required,
                    Leg.Requeue => job.Site != null && job.Home != null && job.Storage == null && job.Cargo == 0,
                    Leg.Assist => job.Site != null && job.Home != null && job.Storage == null && job.Cargo == 0,
                    Leg.ReturnMetal => job.Site == null && job.Home != null && job.Storage != null && job.Cargo == job.Required,
                    Leg.Return => job.Site == null && job.Home != null && job.Storage == null && job.Cargo == 0,
                    _ => false
                }),
            _ => false
        };
        Require(valid, "Inconsistent material cargo or phase.");
    }

    private bool ValidEndpoint(Job job, Vector2 end)
    {
        Vector2? expected = job.Leg switch
        {
            Leg.Pickup or Leg.Grab => job.Drop.Position,
            Leg.RawDelivery => job.Refinery.GlobalPosition,
            Leg.RawHold or Leg.Requeue => _builders.LaunchPoint(job.Home),
            Leg.MaterialPickup or Leg.MetalDelivery => job.Storage.GlobalPosition,
            Leg.ReturnMetal => job.Storage?.GlobalPosition ?? job.Refinery.GlobalPosition,
            Leg.Return => job.Role == Role.Courier ? job.Refinery.GlobalPosition : _builders.LaunchPoint(job.Home),
            _ => null
        };
        return expected.HasValue ? expected.Value.DistanceSquaredTo(end) < .01f
            : job.Site.GlobalPosition.DistanceSquaredTo(end) <= 43 * 43;
    }

    private static void RestoreReservation(Job job)
    {
        if (job.Drop != null)
        { Require(!job.Drop.Reserved, "Duplicate scrap reservation."); job.Drop.Reserved = true; }
        if (job.Role == Role.Salvage && job.Refinery != null) job.Refinery.RawIncoming++;
        if (job.Role == Role.Courier && job.Refinery != null)
        { job.Refinery.ReturningMetal += job.Cargo; job.Refinery.Outbound = job.Cargo; }
        if (job.Storage == null) return;
        if (job.Leg is Leg.Waiting or Leg.MaterialPickup) job.Storage.Reserved += job.Required;
        else job.Storage.Incoming += job.Cargo;
    }

    private static void Require(bool valid, string message)
    { if (!valid) throw new InvalidDataException(message); }
}
