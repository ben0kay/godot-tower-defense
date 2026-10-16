// Optional absolute level capability. Discrete weapons charge per launch; beams charge rounds per firing second.
public readonly record struct AmmunitionStats(int Capacity, int PerShot, float PerSecond = 0)
{
    public string Describe() => $"Ammo capacity: {Capacity}\n" + (PerSecond > 0 ? $"Consumption: {PerSecond:0.##} ammo/s" : $"Consumption: {PerShot} ammo/attack");
    // =========================================================
    // Upgrade previews describe the same optional absolute snapshot applied after payment.
    public string ChangesFrom(AmmunitionStats? previous)
    {
        if (!previous.HasValue) return Describe() + "\n";
        AmmunitionStats a = previous.Value; string changes = "";
        if (a.Capacity != Capacity) changes += $"Ammo capacity: {a.Capacity} → {Capacity}\n";
        if (a.PerShot != PerShot) changes += $"Ammo/attack: {a.PerShot} → {PerShot}\n";
        if (a.PerSecond != PerSecond) changes += $"Ammo/s: {a.PerSecond:0.##} → {PerSecond:0.##}\n";
        return changes;
    }
}
