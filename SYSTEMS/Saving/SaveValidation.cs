// Bounded schema validation before disk publication or world preparation; gameplay owners validate capacities/references.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class SaveValidation
{
    // =========================================================
    // Finite nonnegative source values keep malformed saves out of clocks and arithmetic.
    internal static void Number(double value)
    { if (!double.IsFinite(value) || value < 0) throw new InvalidDataException("Invalid checkpoint number."); }
    internal static void Text(string value, int maximum = 128)
    { if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(char.IsControl)) throw new InvalidDataException("Invalid checkpoint identity."); }
    private static void Point(SavePoint p)
    { if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || Math.Abs(p.X) > 1e9 || Math.Abs(p.Y) > 1e9) throw new InvalidDataException("Invalid saved position."); }
    private static void List<T>(ICollection<T> values, int cap = 4096)
    { if (values == null || values.Count > cap || values.Any(v => v is null)) throw new InvalidDataException("Invalid checkpoint collection."); }
    private static void Unique(IEnumerable<string> values)
    { var seen = new HashSet<string>(StringComparer.Ordinal); foreach (string id in values) { Text(id); if (!seen.Add(id)) throw new InvalidDataException("Duplicate checkpoint identity."); } }

    // =========================================================
    // Missing ownership, inconsistent work, duplicate instances and impossible flight phases are hard errors.
    public static void Validate(LevelSaveData data, string profileId)
    {
        Text(data.LevelId, 64); Text(data.ContentHash, 64);
        if (data.ContentHash.Length != 64 || data.CompletedWaves < 0) throw new InvalidDataException("Invalid mission checkpoint.");
        List(data.Towers); List(data.Citadels, 64); List(data.Builders); List(data.Balances, 64);
        if (data.Citadels.Count == 0 || data.Research == null || data.Run == null || data.Logistics == null || data.RewardTicket == null)
            throw new InvalidDataException("Incomplete checkpoint.");
        var ticket = data.RewardTicket;
        if (ticket.ProfileId != profileId || ticket.LevelId != data.LevelId || ticket.Number <= 0 || ticket.Policy.VictoryXP < 0 || ticket.Policy.DefeatXP < 0 || data.Research.ProfileId != profileId)
            throw new InvalidDataException("Incorrect checkpoint/reward ownership.");
        List(data.Research.Towers); Number(data.Research.CitadelHealthPercent);
        foreach (var pair in data.Research.Towers) { Text(pair.Key); Number(pair.Value.DamagePercent); Number(pair.Value.ProjectileSpreadReductionFraction); if (pair.Value.ProjectileSpreadReductionFraction > 1) throw new InvalidDataException("Invalid research spread fraction."); }
        Number(data.BuilderRetry);
        Number(data.Run.Killed); Number(data.Run.Breached); Number(data.Run.Placed); Number(data.Run.Constructed);
        Number(data.Run.Upgrades); Number(data.Run.Seconds);
        if (data.Run.Constructed > data.Run.Placed) throw new InvalidDataException("Invalid construction telemetry.");
        foreach (var pair in data.Balances) Text(pair.Key);
        Unique(data.Towers.Select(t => t.Id)); Unique(data.Citadels.Select(c => c.Id));
        foreach (var citadel in data.Citadels)
        { if (citadel.Health < 1 || citadel.Credits < 0) throw new InvalidDataException("Invalid Citadel checkpoint."); Number(citadel.ProductionSeconds); }
        var towerIds = data.Towers.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var tower in data.Towers)
        {
            CampaignStore.ValidateId(tower.Id); Text(tower.TypeId);
            if (tower.Tier < 1 || !float.IsFinite(tower.Health) || tower.Health <= 0 || (tower.Priority.HasValue && !Enum.IsDefined(tower.Priority.Value)) ||
                !Enum.IsDefined(tower.Work.Phase) || Math.Abs((long)tower.X) > 1000000 || Math.Abs((long)tower.Y) > 1000000)
                throw new InvalidDataException("Invalid tower checkpoint.");
            Number(tower.XP); Number(tower.Heat); Number(tower.Work.ElapsedSeconds); Number(tower.Work.TotalSeconds);
            if (tower.Work.ElapsedSeconds > tower.Work.TotalSeconds ||
                (tower.Work.Phase != TowerWorkPhase.Operational && tower.Work.ElapsedSeconds >= tower.Work.TotalSeconds) ||
                (tower.Held && tower.Work.Phase != TowerWorkPhase.Constructing) ||
                (tower.Work.Phase != TowerWorkPhase.Upgrading && tower.Work.PendingUpgradeLevel != 0))
                throw new InvalidDataException("Invalid saved work state.");
            if (tower.Ammo < 0 || tower.StoredCredits < 0) throw new InvalidDataException("Invalid saved stock.");
            Number(tower.Consumed); Number(tower.Discarded); Number(tower.BeamCredit); Number(tower.Produced);
            if (tower.BeamCredit >= 1) throw new InvalidDataException("Invalid prepaid beam fraction.");
            Number(tower.CreditSeconds); Number(tower.AmmoSeconds);
            if (!double.IsFinite(tower.WeaponCooldown) || Math.Abs(tower.WeaponCooldown) > 1e9) throw new InvalidDataException("Invalid cooldown.");
            List(tower.Disables, 128);
            if (tower.Disables.Count(d => d.Reason == TowerDisableReason.Overheated) != (tower.Overheated ? 1 : 0)) throw new InvalidDataException("Invalid overheat latch.");
            foreach (var disable in tower.Disables)
            {
                if (!Enum.IsDefined(disable.Reason) || disable.Reason == TowerDisableReason.Upgrading || disable.Source == null || disable.Source.Length > 256) throw new InvalidDataException("Invalid disable source.");
                if (disable.Remaining.HasValue) { Number(disable.Remaining.Value); if (disable.Remaining == 0) throw new InvalidDataException("Expired disable in checkpoint."); }
            }
        }
        var logistics = data.Logistics;
        List(logistics.Jobs); List(logistics.Pending); List(logistics.Factories); List(logistics.Depots);
        Unique(logistics.Pending); Unique(logistics.Factories); Unique(logistics.Depots);
        foreach (string id in logistics.Pending.Concat(logistics.Factories).Concat(logistics.Depots))
            if (!towerIds.Contains(id)) throw new InvalidDataException("Unknown logistics endpoint.");
        Number(logistics.FactoryCursor); Number(logistics.RequestCursor); Number(logistics.Elapsed); Number(logistics.Lost); Number(logistics.Delivered);
        foreach (var job in logistics.Jobs)
        {
            if (!towerIds.Contains(job.Source) || job.Destination == null || (job.Destination.Length > 0 && !towerIds.Contains(job.Destination)) || job.Source == job.Destination ||
                job.Cargo < 0 || job.Reserved < 0 || (job.Returning ? job.Reserved != 0 : job.Reserved != job.Cargo || job.Cargo == 0 || job.Destination.Length == 0))
                throw new InvalidDataException("Invalid saved shipment.");
            Point(job.Position); Number(job.Speed); if (job.Speed == 0) throw new InvalidDataException("Invalid drone speed.");
        }
        if (data.Builders.Count(b => !b.Queued) > 64) throw new InvalidDataException("Too many active builders.");
        Unique(data.Builders.Where(b => !string.IsNullOrEmpty(b.Tower)).Select(b => b.Tower));
        foreach (var builder in data.Builders)
        {
            if (builder.Tower == null || builder.HomeId == null || !Enum.IsDefined(builder.Phase) || builder.Phase == ConstructionDronePhase.Cancelled ||
                (builder.Tower.Length > 0 && !towerIds.Contains(builder.Tower)) ||
                (builder.Phase == ConstructionDronePhase.Returning && builder.Tower.Length > 0) || (!builder.Queued && !data.Citadels.Any(c => c.Id == builder.HomeId)))
                throw new InvalidDataException("Invalid saved builder.");
            builder.Motion?.Validate();
            Point(builder.Home); Point(builder.Target); Point(builder.Position);
            Number(builder.HalfWidth); Number(builder.Hover); Number(builder.Height); Number(builder.Age); Number(builder.Deploy);
        }
        Point(data.CameraPosition); Number(data.CameraZoom); if (data.CameraZoom == 0) throw new InvalidDataException("Invalid camera zoom.");
    }
}
