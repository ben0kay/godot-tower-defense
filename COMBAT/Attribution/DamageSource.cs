// Weak attack owner: towers retain XP; friendly fighters identify their shots without impersonating a tower.
using Godot;
using System;

public sealed class DamageSource
{
    private readonly WeakReference<TowerBase> _owner;
    private readonly WeakReference<FighterDrone> _fighterOwner;

    // =========================================================
    // Projectiles/waves must not keep removed towers alive or transfer credit to a later replacement.
    public DamageSource(TowerBase owner) => _owner = new(owner);
    // Fighter kills remain ordinary enemy kills/credit rewards. Fighter XP is a future explicit policy.
    public DamageSource(FighterDrone owner) => _fighterOwner = new(owner);

    // =========================================================
    // A live source provides the session's shared Inspector rules; the enemy snapshots its XP budget once.
    public bool TryGetRules(out TowerMasterConfig rules)
    {
        rules = null;
        if (!TryGetTower(out TowerBase tower)) return false;
        rules = tower.ExperienceRules; return GodotObject.IsInstanceValid(rules);
    }

    // =========================================================
    // Recheck at publication: a kill callback may have removed the originating tower.
    public void Credit(double experience)
    { if (TryGetTower(out TowerBase tower)) TowerRankController.AwardExperience(tower, experience); }

    // =========================================================
    // Native lifetime, tree membership and deferred deletion all count; weak identity is never reassigned.
    public bool TryGetTower(out TowerBase tower)
    {
        tower = null;
        return _owner != null && _owner.TryGetTarget(out tower)
            && GodotObject.IsInstanceValid(tower) && tower.IsInsideTree() && !tower.IsQueuedForDeletion();
    }
    public bool TryGetFighter(out FighterDrone fighter)
    {
        fighter = null;
        return _fighterOwner != null && _fighterOwner.TryGetTarget(out fighter)
            && GodotObject.IsInstanceValid(fighter) && fighter.IsInsideTree() && !fighter.IsQueuedForDeletion();
    }
}
