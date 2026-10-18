// Immutable launch snapshot: Slow strength is a fraction; Freeze duration is pause-aware seconds.
using System;
public readonly record struct StatusEffectPayload
{
    #region Payload
    public StatusEffectId Id { get; }
    public float Strength { get; }
    public float DurationSeconds { get; }
    public bool IsValid => Enum.IsDefined(Id) && (Id == StatusEffectId.Slow
        ? float.IsFinite(Strength) && Strength > 0 && Strength < 1
        : float.IsFinite(DurationSeconds) && DurationSeconds > 0);
    // =========================================================
    // Keep the existing Slow constructor; Freeze uses an explicit seconds argument.
    public StatusEffectPayload(StatusEffectId id, float strength, float durationSeconds = 0)
    {
        Id = id; Strength = strength; DurationSeconds = durationSeconds;
        if (!IsValid) throw new ArgumentOutOfRangeException(nameof(strength));
    }
    #endregion
}
