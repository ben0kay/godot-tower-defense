// One level/session wallet; validated atomic payments and event-driven HUD updates.
using Godot;
using System;
using System.Collections.Generic;

public partial class EconomyManager : Node
{
    #region Configuration and State
    [Export] public EconomyMasterConfig Config { get; set; }
    [Export] public ResourceDefinition CreditsResource { get; set; }
    private readonly Dictionary<string, int> _balances = new();
    private GameplayHUD _hud;
    private EnemyRegistry _enemyRegistry;
    private bool _transactionActive;

    // =========================================================
    // Notify views after the complete transaction has committed.
    [Signal] public delegate void BalanceChangedEventHandler(string resourceId, int amount);
    #endregion

    #region Level Lifecycle
    // =========================================================
    // Main places this node after the HUD; each new scene gets its own fresh wallet.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD");
        _enemyRegistry = GetNode<EnemyRegistry>("../World/EnemySystems/Registry");
        BalanceChanged += UpdateCreditsDisplay;
        _enemyRegistry.EnemyKilled += GrantKillReward;
        Initialize(GetNode<WorldGrid>("../World/Grid").Level.Settings);
    }

    // =========================================================
    // Reset all resources for a new level, never retaining the previous map's balance.
    public void Initialize(LevelConfig level)
    {
        if (_transactionActive) throw new InvalidOperationException("Cannot reset a wallet during a purchase.");
        if (level == null || !ValidResource(CreditsResource))
            throw new InvalidOperationException("Economy requires level settings and a valid Credits resource.");
        string[] previous = new string[_balances.Count]; _balances.Keys.CopyTo(previous, 0);
        _balances.Clear();
        if (GodotObject.IsInstanceValid(_hud)) _hud.ResetCreditReward();
        _balances[CreditsResource.ResourceId] = Mathf.Clamp(level.StartingCredits, 0, MaximumBalance);
        foreach (string id in previous)
            if (id != CreditsResource.ResourceId) EmitSignal(SignalName.BalanceChanged, id, 0);
        EmitSignal(SignalName.BalanceChanged, CreditsResource.ResourceId, GetBalance(CreditsResource));
    }

    // =========================================================
    // Avoid retaining HUD subscriptions after this session exits.
    public override void _ExitTree()
    {
        BalanceChanged -= UpdateCreditsDisplay;
        if (GodotObject.IsInstanceValid(_enemyRegistry)) _enemyRegistry.EnemyKilled -= GrantKillReward;
    }

    // =========================================================
    // Credits are pushed on changes only; the HUD never owns the balance.
    private void UpdateCreditsDisplay(string id, int amount)
    {
        if (id == CreditsResource.ResourceId && GodotObject.IsInstanceValid(_hud)) _hud.SetCredits(amount);
    }
    // =========================================================
    // A killed ship grants its type's configured credits; Citadel arrivals do not emit kills.
    // Reuse Grant for wallet caps and event-driven HUD updates.
    private void GrantKillReward(Enemy enemy)
    {
        int reward = enemy?.Definition?.KillRewardCredits ?? 0;
        if (reward > 0) Grant(CreditsResource, reward);
    }
    #endregion

    #region Transactions
    private int MaximumBalance => Mathf.Max(0, Config?.MaxBalancePerResource ?? 1000000000);

    // =========================================================
    // Resource IDs are stable identity; separate instances with the same ID share a balance.
    private static bool ValidResource(ResourceDefinition resource) =>
        resource != null && !string.IsNullOrWhiteSpace(resource.ResourceId);

    // =========================================================
    // Missing balances are zero, including future secondary materials.
    public int GetBalance(ResourceDefinition resource) =>
        ValidResource(resource) && _balances.TryGetValue(resource.ResourceId, out int balance) ? balance : 0;

    // =========================================================
    // Reject malformed recipes; combine duplicate IDs before checking affordability.
    private bool Aggregate(Godot.Collections.Array<ResourceCost> costs, out Dictionary<string, int> totals)
    {
        totals = new();
        if (costs == null) return false;
        foreach (ResourceCost cost in costs)
        {
            if (cost == null || !ValidResource(cost.Resource) || cost.Amount < 0) return false;
            if (cost.Amount == 0) continue;
            totals.TryGetValue(cost.Resource.ResourceId, out int existing);
            long combined = (long)existing + cost.Amount;
            if (combined > int.MaxValue) return false;
            totals[cost.Resource.ResourceId] = (int)combined;
        }
        return true;
    }

    // =========================================================
    // Validate all deductions together; default policy never permits negative balances.
    private bool Affordable(Dictionary<string, int> totals)
    {
        foreach (var pair in totals)
        {
            _balances.TryGetValue(pair.Key, out int balance);
            long remaining = (long)balance - pair.Value;
            if (remaining < (Config?.AllowNegativeBalances == true ? -MaximumBalance : 0)) return false;
        }
        return true;
    }

    // =========================================================
    // A view can check a recipe without changing balances or emitting events.
    public bool CanAfford(Godot.Collections.Array<ResourceCost> costs) =>
        !_transactionActive && Aggregate(costs, out var totals) && Affordable(totals);

    // =========================================================
    // Reuse the same atomic payment path for future upgrades or other purchases.
    public bool TrySpend(Godot.Collections.Array<ResourceCost> costs) => TryPurchase(costs, () => true);

    // =========================================================
    // Validate payment, commit the build, then deduct once; failed builds never need a refund.
    // The synchronous callback must roll back its own world changes on failure.
    public bool TryPurchase(Godot.Collections.Array<ResourceCost> costs, Func<bool> commit)
    {
        if (_transactionActive || commit == null || !Aggregate(costs, out var totals) || !Affordable(totals))
            return false;
        bool committed = false;
        _transactionActive = true;
        try
        {
            if (!commit()) return false;
            foreach (var pair in totals)
            {
                _balances.TryGetValue(pair.Key, out int balance);
                _balances[pair.Key] = balance - pair.Value;
            }
            committed = true;
        }
        finally { _transactionActive = false; }
        if (committed)
            foreach (var pair in totals) EmitSignal(SignalName.BalanceChanged, pair.Key, _balances[pair.Key]);
        return committed;
    }

    // =========================================================
    // Grant all or nothing, rejecting invalid amounts and balance overflow.
    public bool Grant(ResourceDefinition resource, int amount)
    {
        if (_transactionActive || !ValidResource(resource) || amount < 0) return false;
        int before = GetBalance(resource);
        long after = (long)before + amount;
        if (after > MaximumBalance) return false;
        if (amount == 0) return true;
        _balances[resource.ResourceId] = (int)after;
        EmitSignal(SignalName.BalanceChanged, resource.ResourceId, (int)after);
        if (resource.ResourceId == CreditsResource?.ResourceId && GodotObject.IsInstanceValid(_hud))
            _hud.ShowCreditReward(amount);
        return true;
    }
    #endregion
}
