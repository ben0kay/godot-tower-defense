// Physical leg transitions. A job's cargo is owned by exactly one source, drone or destination.
using Godot;
using System;
using System.Collections.Generic;

public partial class SalvageLogistics
{
    private void Launch(Job job, Leg leg, Vector2 from, Vector2 to)
    {
        if (job.Role == Role.Builder)
        {
            job.Drone = DroneScene.Instantiate<ConstructionDrone>();
            job.Body = job.Drone.GetNode<DroneBody>("Body");
            job.Beam = job.Drone.GetNode<ConstructionBeam>("Beam");
        }
        else
        {
            job.Body = DroneBodyScene.Instantiate<DroneBody>();
            job.Drone = job.Body;
        }
        job.Drone.ZIndex = 4; AddChild(job.Drone);
        job.Drone.GlobalPosition = from;
        Begin(job, leg, to);
    }

    private void Begin(Job job, Leg leg, Vector2 target)
    {
        Vector2 from = job.Drone.GlobalPosition;
        Vector2 direction = target - from;
        Vector2 departure = job.Motion?.Velocity.Vector() ?? direction.Rotated(.38f);
        job.Motion ??= new DroneFlightMotion { Heading = direction.Angle() };
        job.Motion.BeginFlight(from, target, departure, direction.Rotated(-.3f), Speed);
        job.Leg = leg; job.Hover = 0; job.Beam?.Hide();
    }

    private bool Tick(Job job, float delta)
    {
        if (job.Role == Role.Builder && job.Site != null && !Live(job.Site))
        {
            ReleaseStock(job); DetachSite(job);
            if (job.Cargo > 0) return RedirectMetal(job);
            if (job.Drone == null) return false;
            return Return(job);
        }
        if (job.Leg == Leg.Waiting) return true;
        if (job.Role != Role.Courier && !LiveHome(job.Home))
        {
            job.Home = _builders.NearestHome(job.Drone.GlobalPosition);
            if (!LiveHome(job.Home))
            {
                if (job.Leg == Leg.Assist && Live(job.Site)) return true;
                if (job.Role != Role.Builder || !Live(job.Site)) return false;
                ReleaseStock(job);
                _lostMetal += job.Cargo; job.Cargo = 0;
                ParkWaiting(job); return true;
            }
            if (job.Leg is Leg.Return or Leg.RawHold or Leg.Requeue)
                Begin(job, job.Leg, _builders.LaunchPoint(job.Home));
        }
        if ((job.Leg is Leg.Pickup or Leg.Grab or Leg.RawDelivery) && !Operational(job.Refinery))
        {
            ReleaseRawRoom(job);
            if (job.Cargo <= 0)
            {
                ReleaseDrop(job);
                return Return(job);
            }
            job.Refinery = FindRefinery(job.Drone.GlobalPosition);
            if (job.Refinery == null) { Begin(job, Leg.RawHold, _builders.LaunchPoint(job.Home)); return true; }
            job.Refinery.RawIncoming++; job.Refinery.NotifyRuntimeChanged();
            Begin(job, Leg.RawDelivery, job.Refinery.GlobalPosition);
        }
        if (job.Leg == Leg.MaterialPickup && !Operational(job.Storage))
        {
            ReleaseStock(job);
            Begin(job, Leg.Requeue, _builders.LaunchPoint(job.Home));
            job.Site.NotifyRuntimeChanged();
            return true;
        }
        if ((job.Leg == Leg.MetalDelivery || job.Leg == Leg.ReturnMetal && job.Storage != null) && !Operational(job.Storage))
            return RedirectMetal(job);
        if (job.Leg == Leg.ReturnMetal && job.Storage == null && !Live(job.Refinery))
            return RedirectMetal(job);
        if (job.Leg == Leg.Return && job.Role == Role.Courier && !Live(job.Refinery)) return false;
        if (delta == 0) return true; // Checkpoint reconciliation must not advance flight or transfer cargo.
        if (job.Leg is Leg.Grab or Leg.Assist)
        {
            job.Hover += delta;
            job.Body.Present(job.Motion.Heading, Vector2.Zero, Speed, false, (int)job.Leg,
                job.Cargo > 0, delta, lightWhileStationary: true);
            if (job.Leg == Leg.Assist)
            {
                if (!job.Site.IsBusy) return Return(job);
                job.Beam.Present(job.Site.GlobalPosition, 18, BuilderConfig.ShowVisuals ? 1 : 0, BuilderConfig, (float)job.Hover);
                return true;
            }
            float grab = float.IsFinite(Config.GrabSeconds) ? Mathf.Clamp(Config.GrabSeconds, 0, 2) : .25f;
            if (job.Hover < grab) return true;
            _drops.Remove(job.Drop); job.Drop = null; QueueRedraw();
            job.Cargo = 1; Begin(job, Leg.RawDelivery, job.Refinery.GlobalPosition); return true;
        }

        Vector2 position = job.Drone.GlobalPosition;
        bool arrived = job.Motion.Fly(delta, ref position, 240);
        job.Drone.GlobalPosition = position;
        job.Body.Present(job.Motion.Heading, job.Motion.Velocity.Vector(), Speed, true, (int)job.Leg, job.Cargo > 0, delta);
        return !arrived || Arrive(job);
    }

    private bool Arrive(Job job)
    {
        switch (job.Leg)
        {
            case Leg.Requeue:
                ParkWaiting(job); return true;
            case Leg.Pickup: job.Leg = Leg.Grab; job.Hover = 0; return true;
            case Leg.RawDelivery:
                var refinery = job.Refinery;
                refinery.Scrap += job.Cargo; job.Cargo = 0;
                ReleaseRawRoom(job);
                return Return(job);
            case Leg.RawHold:
                if (_holding.GetValueOrDefault(job.Home) < HoldingCapacity)
                { ChangeHolding(job.Home, job.Cargo); job.Cargo = 0; }
                return false;
            case Leg.MaterialPickup:
                var source = job.Storage;
                source.Metal -= job.Required; job.Cargo = job.Required;
                ReleaseStock(job);
                Vector2 approach = (job.Site.GlobalPosition - job.Drone.GlobalPosition).Normalized();
                if (approach.IsZeroApprox()) approach = Vector2.Right;
                Begin(job, Leg.SiteDelivery, job.Site.GlobalPosition + approach.Orthogonal() * 42);
                job.Site.NotifyRuntimeChanged(); return true;
            case Leg.SiteDelivery:
                job.Site.DeliverMetal(); job.Cargo = 0; job.Leg = Leg.Assist; job.Hover = 0;
                job.Site.NotifyRuntimeChanged(); return true;
            case Leg.MetalDelivery:
            case Leg.ReturnMetal:
                int amount = job.Cargo;
                if (job.Storage != null)
                {
                    var destination = job.Storage;
                    destination.Metal += amount;
                    ReleaseIncoming(job);
                    NotifyStock();
                }
                else if (Live(job.Refinery)) job.Refinery.Metal += amount;
                else return false;
                ReleaseRefineryReturn(job); job.Cargo = 0;
                if (job.Role == Role.Courier && job.Leg == Leg.ReturnMetal && Live(job.Refinery)
                    && job.Drone.GlobalPosition.IsEqualApprox(job.Refinery.GlobalPosition)) return false;
                return Return(job);
            default: return false;
        }
    }

    private static void ParkWaiting(Job job)
    {
        job.Drone.QueueFree(); job.Drone = null; job.Body = null; job.Beam = null; job.Motion = null;
        job.Home = null; job.Leg = Leg.Waiting; job.Hover = 0;
        job.Site.NotifyRuntimeChanged();
    }

    private bool Return(Job job)
    {
        DetachSite(job);
        if (job.Role == Role.Courier)
        {
            if (!Live(job.Refinery)) { job.Leg = Leg.Return; return false; }
            Begin(job, Leg.Return, job.Refinery.GlobalPosition); return true;
        }
        if (!LiveHome(job.Home)) { job.Leg = Leg.Return; return false; }
        Begin(job, Leg.Return, _builders.LaunchPoint(job.Home)); return true;
    }

    private bool RedirectMetal(Job job)
    {
        ReleaseIncoming(job);
        var storage = FindStorage(job.Drone.GlobalPosition, job.Cargo, supply: false);
        if (storage != null)
        {
            job.Storage = storage; storage.Incoming += job.Cargo; storage.NotifyRuntimeChanged();
            Begin(job, Leg.ReturnMetal, storage.GlobalPosition); return true;
        }
        if (job.Role == Role.Courier && Live(job.Refinery))
        {
            Begin(job, Leg.ReturnMetal, job.Refinery.GlobalPosition); return true;
        }
        return false;
    }

    private void ReleaseDrop(Job job) { if (job.Drop != null) job.Drop.Reserved = false; job.Drop = null; }
    private void ReleaseRawRoom(Job job)
    {
        if (GodotObject.IsInstanceValid(job.Refinery))
        { job.Refinery.RawIncoming--; job.Refinery.NotifyRuntimeChanged(); }
        job.Refinery = null;
    }
    private void ReleaseStock(Job job)
    {
        if (GodotObject.IsInstanceValid(job.Storage))
        { job.Storage.Reserved -= job.Required; job.Storage.NotifyRuntimeChanged(); }
        job.Storage = null; NotifyStock();
    }
    private void ReleaseIncoming(Job job)
    {
        if (GodotObject.IsInstanceValid(job.Storage))
        { job.Storage.Incoming -= job.Cargo; job.Storage.NotifyRuntimeChanged(); }
        job.Storage = null;
    }
    private void ReleaseRefineryReturn(Job job)
    {
        if (job.Role != Role.Courier || !GodotObject.IsInstanceValid(job.Refinery)) return;
        job.Refinery.ReturningMetal -= job.Cargo;
        job.Refinery.Outbound = 0; job.Refinery.NotifyRuntimeChanged();
    }

    private void Retire(int index)
    {
        Job job = _jobs[index];
        ReleaseDrop(job);
        if (job.Role == Role.Salvage)
        {
            if (job.Refinery != null) ReleaseRawRoom(job);
            _lostScrap += job.Cargo;
        }
        else
        {
            if (job.Leg is Leg.Waiting or Leg.MaterialPickup) ReleaseStock(job);
            else ReleaseIncoming(job);
            ReleaseRefineryReturn(job); _lostMetal += job.Cargo;
        }
        DetachSite(job);
        if (GodotObject.IsInstanceValid(job.Drone) && !job.Drone.IsQueuedForDeletion()) job.Drone.QueueFree();
        _jobs.RemoveAt(index);
    }

    internal void StopForOutcome()
    {
        _stopped = true; SetPhysicsProcess(false);
        foreach (var job in _jobs) { job.Beam?.Hide(); job.Body?.Reset(); }
    }

    public override void _ExitTree()
    {
        _stopped = true;
        if (GodotObject.IsInstanceValid(_enemies)) _enemies.EnemyKilled -= OnKill;
        if (GodotObject.IsInstanceValid(_towers))
        { _towers.ChildEnteredTree -= Track; _towers.ChildExitingTree -= Untrack; }
        foreach (var storage in _stores)
            if (GodotObject.IsInstanceValid(storage)) { storage.DisableStateChanged -= NotifyStock; storage.WorkCompleted -= NotifyStock; }
        foreach (var job in _jobs) DetachSite(job);
        _jobs.Clear(); _drops.Clear(); _stores.Clear(); _refineries.Clear(); _holding.Clear();
    }
}
