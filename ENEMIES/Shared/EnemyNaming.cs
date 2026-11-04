// Shared readable classification labels for editor/debug menus; stable IDs and gameplay remain independent.
public static class EnemyNaming
{
    // =========================================================
    // Centralize readable labels independently of serialized enum numbers.
    public static string Class(EnemyClass value) => value.ToString();
    public static string Rank(EnemyRank value) => value.ToString();
    public static string Role(EnemyRole value) => value.ToString();

    // =========================================================
    // A short picker label keeps long future ship names usable; details are displayed separately.
    public static string EditorLabel(EnemyDefinition definition) => definition == null ? "Unregistered enemy" :
        Class(definition.Class) + " · " + definition.DisplayName;

    // =========================================================
    // Used only on editor/debug selection changes, never by enemy simulation or combat rules.
    public static string ClassificationLabel(EnemyDefinition definition) => definition == null ? "Unregistered enemy" :
        "Class: " + Class(definition.Class) + " · Rank: " + Rank(definition.Rank) + " · Role: " + Role(definition.Role);
}
