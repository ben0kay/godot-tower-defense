// Explicit per-instance checkpoint seams; ordinary purchases and gameplay initialization remain unchanged.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public abstract partial class TowerBase
{
    public string SaveId { get; private set; } = Guid.NewGuid().ToString("N");
    internal virtual double CaptureWeaponCooldown() => 0;
    internal virtual void RestoreWeaponCooldown(double seconds) { }

    // =========================================================
    // Capture source inputs, real stock and fractional clocks; never serialize calculated final stats.
    internal TowerSaveData CaptureCheckpoint(Func<ulong, string> sourceKey)
    {
        var inventory = this is AmmoBuilding building ? building.Inventory : Ammunition?.Inventory;
        return new()
        {
            Id = SaveId, TypeId = TowerTypeId, X = GridCell.X, Y = GridCell.Y, Tier = UpgradeLevel,
            XP = RankProgress.TotalXp, Health = Health, Priority = TargetPriority, Work = CaptureWorkState(), Held = AwaitingConstructionDrone,
            Heat = CurrentHeat, Overheated = IsOverheated, Disables = _conditions?.CaptureCheckpoint(sourceKey) ?? new(),
            Ammo = inventory?.Amount, Consumed = Ammunition?.ConsumedRounds ?? 0, Discarded = Ammunition?.DiscardedRounds ?? 0,
            BeamCredit = Ammunition?.SavedBeamCredit ?? 0,
            StoredCredits = (this as CreditProducerTower)?.Buffer?.Current,
            CreditSeconds = (this as CreditProducerTower)?.SavedCreditSeconds ?? 0,
            AmmoSeconds = (this as AmmunitionFactory)?.Production.ProgressSeconds ?? 0,
            Produced = (this as AmmunitionFactory)?.ProducedRounds ?? 0, WeaponCooldown = CaptureWeaponCooldown()
        };
    }

    // =========================================================
    // Set progression/work before Ready so no wrong-tier activation or initial construction transaction can occur.
    internal void PrepareCheckpoint(TowerSaveData data, ConstructionWorkManager work, ConstructionDroneManager builders)
    {
        SaveId = data.Id; AuthoredConfig.InitialAmmoFraction = 0;
        if (data.Tier < 1 || (Levels.Count > 0 ? data.Tier > Levels.Count : data.Tier != 1)) throw new InvalidDataException("Saved tower tier is unavailable.");
        UpgradeLevel = data.Tier; RankProgress.Add(data.XP, AuthoredConfig.Ranks);
        if (data.Priority.HasValue)
        {
            if (AuthoredConfig is not ITowerTargetingPolicy policy) throw new InvalidDataException("Unexpected targeting policy.");
            policy.TargetPriority = data.Priority.Value;
        }
        else if (TargetPriority.HasValue) throw new InvalidDataException("Missing targeting policy.");
        WorkPhase = data.Work.Phase; ElapsedWorkSeconds = _lastWorkNotice = data.Work.ElapsedSeconds;
        WorkSeconds = data.Work.TotalSeconds; PendingUpgradeLevel = data.Work.PendingUpgradeLevel;
        if (WorkPhase == TowerWorkPhase.Upgrading && (PendingUpgradeLevel != UpgradeLevel + 1 || PendingUpgradeLevel > Levels.Count))
            throw new InvalidDataException("Invalid pending tower tier.");
        if (data.Held)
        {
            if (builders == null) throw new InvalidDataException("Saved construction needs a builder manager.");
            _constructionDroneOwner = builders; ConstructionDroneReservation = builders.Reserve();
        }
        if (IsBusy) { _construction = work; work.Register(this); }
    }

    // =========================================================
    // Replace creation stock exactly once after Ready; all restored worlds remain paused until references are rebuilt.
    internal void RestoreCheckpoint(TowerSaveData data, Func<string, ulong> sourceId)
    {
        if (data.Health > MaxHealth) throw new InvalidDataException("Saved tower health exceeds current capacity.");
        Health = data.Health; PublishHealthChanged();
        var inventory = this is AmmoBuilding building ? building.Inventory : Ammunition?.Inventory;
        if (data.Ammo.HasValue != (inventory != null)) throw new InvalidDataException("Saved ammunition capability differs.");
        if (inventory != null) inventory.RestoreAmount(data.Ammo.Value);
        Ammunition?.RestoreCounters(data.Consumed, data.Discarded, data.BeamCredit);
        if (this is CreditProducerTower producer)
        {
            if (!data.StoredCredits.HasValue) throw new InvalidDataException("Missing producer balance.");
            producer.RestoreProduction(data.StoredCredits.Value, data.CreditSeconds);
        }
        else if (data.StoredCredits.HasValue) throw new InvalidDataException("Unexpected producer balance.");
        if (this is AmmunitionFactory factory) factory.RestoreProduction(data.AmmoSeconds, data.Produced);
        if (data.Heat != 0 || data.Disables.Count > 0) Conditions().RestoreCheckpoint(data.Heat, data.Overheated, data.Disables, sourceId);
        RestoreWeaponCooldown(data.WeaponCooldown); NotifyRuntimeChanged();
    }
}
