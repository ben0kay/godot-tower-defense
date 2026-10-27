// Builder checkpoints retain FIFO holds, active phases and empty return flights without completing work.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class ConstructionDroneManager
{
    internal double SavedRetry => Math.Max(0, _retry);
    // =========================================================
    // Queue order and active order remain separate. Cancelled tombstones have no durable effect.
    internal List<BuilderSaveData> CaptureCheckpoint()
    {
        var result = new List<BuilderSaveData>();
        void Save(Assignment j, bool queued)
        {
            if (j.Cancelled) return;
            result.Add(new() { Tower = j.Tower?.SaveId ?? "", HomeId = j.Source?.Id ?? "", Queued = queued, Phase = queued ? ConstructionDronePhase.Flying : j.Phase,
                Motion = queued ? null : j.Motion.Copy(),
                Home = SavePoint.From(j.Home), Target = SavePoint.From(j.Target), Position = SavePoint.From(j.Position),
                HalfWidth = j.HalfWidth, Hover = j.Hover, Height = j.Height, Age = j.Age, Deploy = j.Deploy });
        }
        foreach (var j in _pending) Save(j, true);
        foreach (var j in _active) Save(j, false);
        return result;
    }

    // =========================================================
    // Reissue instance-local tokens and event handlers; the paid work state was restored before these assignments.
    internal void RestoreCheckpoint(List<BuilderSaveData> data, IReadOnlyDictionary<string, TowerBase> towers, double retry)
    {
        if (_jobs.Count != 0 || _active.Count != 0 || _pending.Count != 0) throw new InvalidOperationException("Builder restore needs a fresh service.");
        foreach (var saved in data)
        {
            TowerBase tower = saved.Tower.Length == 0 ? null : towers.GetValueOrDefault(saved.Tower);
            if (saved.Tower.Length > 0 && tower == null) throw new InvalidDataException("Saved builder site is missing.");
            var job = new Assignment { Tower = tower, Token = tower?.ConstructionDroneReservation ?? 0,
                Source = _citadels.Objectives.FirstOrDefault(o => o.Id == saved.HomeId), Phase = saved.Phase,
                Home = saved.Home.Vector(), Target = saved.Target.Vector(), Position = saved.Position.Vector(),
                HalfWidth = saved.HalfWidth, Hover = saved.Hover, Height = saved.Height, Age = saved.Age, Deploy = saved.Deploy };
            if (saved.HomeId.Length > 0 && job.Source == null) throw new InvalidDataException("Saved builder home is missing.");
            if (tower != null)
            {
                if (!tower.IsBusy || tower.AwaitingConstructionDrone != (saved.Queued || saved.Phase != ConstructionDronePhase.Beaming))
                    throw new InvalidDataException("Saved builder/work phase mismatch.");
                job.Completed = () => Completed(job); job.Removed = () => Cancel(job);
                _jobs.Add(tower, job); tower.WorkCompleted += job.Completed; tower.TreeExiting += job.Removed; tower.Destroyed += job.Removed;
            }
            else if (saved.Queued || saved.Phase != ConstructionDronePhase.Returning) throw new InvalidDataException("Builder has no site.");
            if (saved.Queued) _pending.Enqueue(job);
            else
            {
                job.Drone = Rent() ?? throw new InvalidDataException("Saved builder visual could not be created.");
                if (saved.Motion != null) job.Motion = saved.Motion.Copy();
                else
                {
                    // Older checkpoints keep their current pose and blend into the new movement model.
                    InitializeMotion(job);
                    job.Motion.Heading = (job.Home - job.Position).Angle();
                    if (job.Phase == ConstructionDronePhase.Returning) BeginReturn(job);
                    else job.Motion.BeginFlight(job.Position, job.Motion.OrbitPoint(job.Target + Vector2.Up * job.Height), Vector2.Zero, job.Motion.Tangent(), Config);
                }
                _active.Add(job);
                float duration = ConstructionDroneMasterConfig.Safe(Config.BeamDeploymentSeconds, .25f, 0, 1);
                float strength = saved.Phase == ConstructionDronePhase.Beaming ? 1 : saved.Phase == ConstructionDronePhase.Deploying ? (duration <= 0 ? 1 : Mathf.Min(1, job.Deploy / duration)) : 0;
                Present(job, strength);
            }
        }
        foreach (var tower in towers.Values)
            if (tower.AwaitingConstructionDrone && !_jobs.ContainsKey(tower)) throw new InvalidDataException("Saved paid work hold has no builder.");
        _retry = retry; SetPhysicsProcess(_active.Count > 0 || _pending.Count > 0);
    }
}
