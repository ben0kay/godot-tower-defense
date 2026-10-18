// Lazy compact runtime storage; independent family policies resolve one cached movement multiplier.
using System;
using System.Collections.Generic;
public sealed class EnemyStatusEffects
{
    #region Active Effects
    private readonly record struct Active(StatusEffectPayload Payload, float Remaining);
    private readonly List<Active> _active = new(2);
    public float MovementMultiplier { get; private set; } = 1;
    public bool HasTimed => Has(StatusEffectId.Freeze);
    public int Count => _active.Count;
    // =========================================================
    // Most affected enemies have one or two entries; ordinary enemies allocate no holder.
    public bool Has(StatusEffectId id)
    { foreach (var effect in _active) if (effect.Payload.Id == id) return true; return false; }
    // =========================================================
    // Family refresh policy: strongest permanent Slow or maximum remaining/new Freeze duration.
    public bool Apply(StatusEffectPayload effect)
    {
        if (!effect.IsValid) return false;
        for (int i = 0; i < _active.Count; i++)
        {
            Active old = _active[i]; if (old.Payload.Id != effect.Id) continue;
            bool stronger = effect.Id == StatusEffectId.Slow ? effect.Strength > old.Payload.Strength : effect.DurationSeconds > old.Remaining;
            if (!stronger) return false;
            _active[i] = new(effect, effect.DurationSeconds); Resolve(); return true;
        }
        _active.Add(new(effect, effect.DurationSeconds)); Resolve(); return true;
    }
    // =========================================================
    // Permanent effects are never decremented; expiration leaves concurrent families intact.
    public bool Advance(float seconds)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Active item = _active[i]; if (item.Payload.Id != StatusEffectId.Freeze) continue;
            float remaining = Math.Max(0, item.Remaining - seconds);
            if (remaining == 0) _active.RemoveAt(i); else _active[i] = item with { Remaining = remaining };
        }
        Resolve(); return HasTimed;
    }
    // =========================================================
    // Freeze gates movement independently of the strongest Slow multiplier and authored base speed.
    private void Resolve()
    {
        float slow = 0; bool frozen = false;
        foreach (var item in _active)
            switch (item.Payload.Id)
            { case StatusEffectId.Slow: slow = Math.Max(slow, item.Payload.Strength); break; case StatusEffectId.Freeze: frozen = true; break; }
        MovementMultiplier = frozen ? 0 : 1 - slow;
    }
    #endregion
}
