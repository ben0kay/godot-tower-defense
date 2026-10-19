// One weak owner token per tower, carried by immutable hits; no tree searches or per-hit allocations.
using Godot;
using System;

public sealed class DamageSource
{
    private readonly WeakReference<TowerBase> _owner;

    // =========================================================
    // Projectiles/waves must not keep removed towers alive or transfer credit to a later replacement.
    public DamageSource(TowerBase owner) => _owner = new(owner);

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
    public bool TryGetTower(out TowerBase tower) => _owner.TryGetTarget(out tower)
        && GodotObject.IsInstanceValid(tower) && tower.IsInsideTree() && !tower.IsQueuedForDeletion();
}
