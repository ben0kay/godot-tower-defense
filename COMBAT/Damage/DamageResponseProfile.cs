// Canonical Inspector defense coefficients, separate from tower and enemy source stats.
using Godot;
using System;
[GlobalClass]
public partial class DamageResponseProfile : Resource
{
    [ExportGroup("Kinetic")]
    [Export] public float KineticShield { get; set; } = 0.5f;
    [Export] public float KineticHull { get; set; } = 1f;
    [ExportGroup("Energy")]
    [Export] public float EnergyShield { get; set; } = 2f;
    [Export] public float EnergyHull { get; set; } = 0.85f;
    [ExportGroup("Explosive")]
    [Export] public float ExplosiveShield { get; set; } = 0.25f;
    [Export] public float ExplosiveHull { get; set; } = 1.25f;
    // =========================================================
    // Reject unknown categories and invalid coefficients without touching target state.
    public (float Shield, float Hull) Coefficients(DamageType type)
    {
        var result = type switch {
            DamageType.Kinetic => (KineticShield, KineticHull),
            DamageType.Energy => (EnergyShield, EnergyHull),
            DamageType.Explosive => (ExplosiveShield, ExplosiveHull),
            _ => throw new ArgumentOutOfRangeException(nameof(type)) };
        if (!float.IsFinite(result.Item1) || !float.IsFinite(result.Item2) || result.Item1 < 0 || result.Item2 < 0)
            throw new InvalidOperationException("Damage multipliers must be finite and nonnegative.");
        return result;
    }
    // =========================================================
    // Validate the complete authored table at initial load or explicit configuration checks.
    public void Validate() { foreach (DamageType type in Enum.GetValues<DamageType>()) Coefficients(type); }
}
