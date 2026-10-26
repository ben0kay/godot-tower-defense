// Reusable Citadel durability: level-provided maximum, damage, and one-time destruction event.
// No enemy spawning, game-over screens or HUD references belong in this component.
using Godot;
using System;

public partial class CitadelHealth : Node
{
    #region State and Events
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDestroyed => MaxHealth > 0 && CurrentHealth == 0;

    public event Action<int, int> HealthChanged;
    public event Action Destroyed;
    #endregion

    #region Lifecycle and Damage
    // =========================================================
    // The active level defines Citadel health; initialize at full HP on load/reset.
    public void Initialize(int maxHealth)
    {
        if (maxHealth < 1) throw new ArgumentOutOfRangeException(nameof(maxHealth), "Citadel maximum health must be positive.");
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    // =========================================================
    // Apply positive arrival damage once and report destruction only on the 1->0 transition.
    public void TakeDamage(int amount)
    {
        if (MaxHealth <= 0) throw new InvalidOperationException("Initialize Citadel health before applying damage.");
        if (amount <= 0 || CurrentHealth == 0) return;
        CurrentHealth = amount >= CurrentHealth ? 0 : CurrentHealth - amount;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (CurrentHealth == 0) Destroyed?.Invoke();
    }
    #endregion

    // =========================================================
    // Checkpoint restore sets source state directly; callers validate before publishing the battlefield.
    internal void RestoreCurrent(int current)
    {
        if (current < 1 || current > MaxHealth) throw new ArgumentOutOfRangeException(nameof(current));
        CurrentHealth = current; HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
