// Shared human-readable fleet role labels for editor/debug menus; gameplay IDs and serialized family values stay stable.
public static class EnemyNaming
{
    // =========================================================
    // Keep labels independent of existing integer enum assignments and scene folder names.
    public static string Role(EnemyFamily family) => family switch
    {
        EnemyFamily.Drone => "Drone",
        EnemyFamily.Interceptor => "Interceptor",
        EnemyFamily.Bulwark => "Bulwark",
        EnemyFamily.ShieldShip => "Shield Ship",
        EnemyFamily.Carrier => "Carrier",
        EnemyFamily.Support => "Support",
        EnemyFamily.CapitalShip => "Capital Ship",
        EnemyFamily.Assault => "Assault",
        _ => "Other"
    };

    // =========================================================
    // Show role + display name + stable wave ID without changing anything persisted to waves/saves.
    public static string EditorLabel(EnemyDefinition definition) =>
        definition == null ? "Unregistered enemy" :
        Role(definition.Family) + " · " + definition.DisplayName + " (" + definition.EnemyId + ")";
}
