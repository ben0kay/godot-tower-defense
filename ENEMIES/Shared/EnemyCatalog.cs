// Inspector-authored enemy scene registry; the scene's existing Definition owns identity/balance once.
using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class EnemyCatalog : Resource
{
    [Export] public Godot.Collections.Array<PackedScene> Scenes { get; set; } = new();
    private Dictionary<string, PackedScene> _scenes;
    private Dictionary<string, EnemyDefinition> _definitions;
    private static EnemyCatalog _default;
    public static EnemyCatalog Default => _default ??= LoadDefault();
    // =========================================================
    // Default registration is loaded/validated once, never per spawn or physics frame.
    private static EnemyCatalog LoadDefault()
    {
        EnemyCatalog catalog = GD.Load<EnemyCatalog>("res://ENEMIES/DefaultEnemyCatalog.tres");
        if (catalog == null) throw new InvalidOperationException("Enemy catalog is missing.");
        catalog.Rebuild(); return catalog;
    }
    // =========================================================
    // Publish a fresh lookup only after every scene/root/definition validates; no fallback IDs.
    public void Rebuild()
    {
        _scenes = null;
        if (Scenes == null) throw new ArgumentException("Enemy scenes are missing.");
        Dictionary<string, PackedScene> next = new(StringComparer.Ordinal);
        Dictionary<string, EnemyDefinition> definitions = new(StringComparer.Ordinal);
        foreach (PackedScene scene in Scenes)
        {
            if (scene == null) throw new ArgumentException("Null enemy scene.");
            Node root = scene.Instantiate();
            try
            {
                if (root is not Enemy enemy || enemy.Definition == null || enemy.GetNodeOrNull<EnemyMovement>("Movement") == null)
                    throw new ArgumentException("Enemy scene requires Enemy root, Definition and Movement.");
                ValidateDefinition(enemy.Definition);
                if (!next.TryAdd(enemy.Definition.EnemyId, scene)) throw new ArgumentException("Duplicate EnemyId: " + enemy.Definition.EnemyId);
                definitions.Add(enemy.Definition.EnemyId, enemy.Definition);
            }
            finally { root.Free(); }
        }
        _scenes = next; _definitions = definitions;
    }
    // =========================================================
    // World-independent balance/clearance validation; physical source fit stays with SpawnManager.
    public static void ValidateDefinition(EnemyDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.EnemyId) || definition.EnemyId != definition.EnemyId.Trim() ||
            !Positive(definition.MaxHealth) || !Positive(definition.MoveSpeed) || !Positive(definition.RadiusPixels) ||
            !Nonnegative(definition.MaxShield) || !Nonnegative(definition.TurnSpeed) || !Nonnegative(definition.DeathExplosionMultiplier) ||
            !Enum.IsDefined(definition.Family) || definition.CitadelDamage < 0 || definition.KillRewardCredits < 0 || definition.Armor < 0)
            throw new ArgumentException("Invalid enemy identity/balance: " + definition.EnemyId);
    }
    // =========================================================
    // Stable registered identity lookup; unknown IDs remain explicit setup errors.
    public PackedScene Find(string id)
    { if (_scenes == null) Rebuild(); return id != null && _scenes.TryGetValue(id, out var scene) ? scene : null; }
    // =========================================================
    // Cached authored clearance metadata avoids instantiating a temporary child for every position probe.
    public EnemyDefinition DefinitionFor(string id)
    { if (_scenes == null) Rebuild(); return id != null && _definitions.TryGetValue(id, out var definition) ? definition : null; }
    // =========================================================
    // Backwards-compatible wave adapter returns its own map without exposing mutable registry state.
    public Dictionary<string, PackedScene> ToSceneMap()
    { if (_scenes == null) Rebuild(); return new(_scenes, StringComparer.Ordinal); }
    // =========================================================
    // Reject nonfinite positive gameplay values before a type can be spawned.
    private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    // =========================================================
    // Zero capacity/turning/feedback remains an intentional supported setting.
    private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
}
