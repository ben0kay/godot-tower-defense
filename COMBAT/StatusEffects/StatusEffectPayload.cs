// Immutable launch snapshot: Slow strength is a fraction; Freeze duration is pause-aware seconds.
using System;
public readonly record struct StatusEffectPayload
{
    #region Payload
    public StatusEffectId Id { get; }
    public float Strength { get; }
    public float DurationSeconds { get; }
    public DamageSource Source { get; }
    public StatusEffectPayload WithSource(DamageSource source) => IsValid ? new(Id, Strength, DurationSeconds, source) : this;
    public bool IsValid => Id switch
    {
        StatusEffectId.Slow => float.IsFinite(Strength) && Strength > 0 && Strength < 1,
        StatusEffectId.Freeze => float.IsFinite(DurationSeconds) && DurationSeconds > 0,
        _ => StatusEffectRules.IsDamageOverTime(Id) && float.IsFinite(Strength) && Strength > 0
            && float.IsFinite(DurationSeconds) && DurationSeconds > 0
    };
    // =========================================================
    // Keep the existing Slow constructor; Freeze uses an explicit seconds argument.
    public StatusEffectPayload(StatusEffectId id, float strength, float durationSeconds = 0, DamageSource source = null)
    {
        Id = id; Strength = strength; DurationSeconds = durationSeconds; Source = source;
        if (!IsValid) throw new ArgumentOutOfRangeException(nameof(strength));
    }
    #endregion
}
