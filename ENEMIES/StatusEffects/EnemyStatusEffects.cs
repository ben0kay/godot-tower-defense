// Compact enemy-owned status state: independent movement policies plus one reusable periodic-damage family.
using Godot;
using System;
using System.Collections.Generic;
public sealed class EnemyStatusEffects
{
    private readonly record struct Active(StatusEffectPayload Payload, double Remaining, double Accumulated = 0);
    private readonly List<Active> _active = new(3);
    public float MovementMultiplier { get; private set; } = 1;
    public bool HasTimed
    { get { foreach (var item in _active) if (item.Payload.Id == StatusEffectId.Freeze || StatusEffectRules.IsDamageOverTime(item.Payload.Id)) return true; return false; } }
    public int Count => _active.Count;
    // =========================================================
    // Lookup cost scales with this enemy's few active identities, never with the enemy registry.
    public bool Has(StatusEffectId id)
    { foreach (var effect in _active) if (effect.Payload.Id == id) return true; return false; }
    // =========================================================
    // DoT keeps stronger DPS, refreshes to the NEW duration, preserves tick phase and transfers source.
    public bool Apply(StatusEffectPayload effect)
    {
        if (!effect.IsValid) return false;
        for (int i = 0; i < _active.Count; i++)
        {
            Active old = _active[i]; if (old.Payload.Id != effect.Id) continue;
            if (StatusEffectRules.IsDamageOverTime(effect.Id))
            {
                var payload = new StatusEffectPayload(effect.Id, Math.Max(old.Payload.Strength, effect.Strength), effect.DurationSeconds, effect.Source);
                _active[i] = new(payload, effect.DurationSeconds, old.Accumulated); return true;
            }
            bool stronger = effect.Id == StatusEffectId.Slow ? effect.Strength > old.Payload.Strength : effect.DurationSeconds > old.Remaining;
            if (!stronger) return false;
            _active[i] = new(effect, effect.DurationSeconds); Resolve(); return true;
        }
        _active.Add(new(effect, effect.DurationSeconds)); Resolve(); return true;
    }
    // =========================================================
    // Commit clocks before damage callbacks. Hitches batch whole seconds; expiry pays a final partial second.
    public bool Advance(double seconds, Enemy owner = null)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return HasTimed;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Active item = _active[i];
            bool dot = StatusEffectRules.IsDamageOverTime(item.Payload.Id);
            if (item.Payload.Id != StatusEffectId.Freeze && !dot) continue;
            double elapsed = Math.Min(seconds, item.Remaining);
            double remaining = Math.Max(0, item.Remaining - elapsed);
            if (remaining < 1e-9) remaining = 0;
            double accumulated = item.Accumulated + elapsed;
            double paid = dot ? (remaining == 0 ? accumulated
                : Math.Min(accumulated, Math.Floor((accumulated + 1e-9) / StatusEffectRules.DamageTickSeconds) * StatusEffectRules.DamageTickSeconds)) : 0;
            if (remaining == 0) _active.RemoveAt(i);
            else _active[i] = new(item.Payload, remaining, dot ? Math.Max(0, accumulated - paid) : 0);
            if (paid > 0 && owner != null)
            {
                if (!GodotObject.IsInstanceValid(owner) || !owner.IsAlive || !owner.IsInsideTree() || owner.IsQueuedForDeletion()) return false;
                float amount = (float)Math.Min(float.MaxValue, item.Payload.Strength * paid);
                owner.ApplyDamage(new DamageInfo(amount, DamageType.Energy, hullCoefficientOverride: 1,
                    shieldPenetration: 1, source: item.Payload.Source));
                // Death/removal/reinitialization replaces the holder; never advance that replacement here.
                if (!GodotObject.IsInstanceValid(owner) || !owner.OwnsStatuses(this)) return false;
            }
        }
        Resolve(); return HasTimed;
    }
    // =========================================================
    // DoT does not participate in movement. Freeze and permanent Slow retain their independent semantics.
    private void Resolve()
    {
        float slow = 0; bool frozen = false;
        foreach (var item in _active)
            switch (item.Payload.Id)
            { case StatusEffectId.Slow: slow = Math.Max(slow, item.Payload.Strength); break; case StatusEffectId.Freeze: frozen = true; break; }
        MovementMultiplier = frozen ? 0 : 1 - slow;
    }
}
