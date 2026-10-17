// Lazy compact instance storage. Only changed statuses resolve cached modifiers; no processing nodes/timers.
using System.Collections.Generic;

public sealed class EnemyStatusEffects
{
    private readonly List<StatusEffectPayload> _active = new(2);
    public float MovementMultiplier { get; private set; } = 1;
    public int Count => _active.Count;
    // =========================================================
    // Most affected enemies have one or two entries; no dictionary is needed on each enemy.
    public bool Has(StatusEffectId id)
    { foreach (var effect in _active) if (effect.Id == id) return true; return false; }
    // =========================================================
    // Slow is one effect family with strongest-wins semantics, never multiplicative stacking.
    public bool Apply(StatusEffectPayload effect)
    {
        if (effect.Id != StatusEffectId.Slow || effect.Strength <= 0 || effect.Strength >= 1) return false;
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].Id != effect.Id) continue;
            if (_active[i].Strength >= effect.Strength) return false;
            _active[i] = effect; MovementMultiplier = 1 - effect.Strength; return true;
        }
        _active.Add(effect); MovementMultiplier = 1 - effect.Strength; return true;
    }
}
