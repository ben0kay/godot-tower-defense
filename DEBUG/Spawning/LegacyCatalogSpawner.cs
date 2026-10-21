// Parked pre-menu catalog cycling helper; deliberately has no input handler or scene attachment.
using Godot;

public sealed class LegacyCatalogSpawner
{
    private int _next;
    // =========================================================
    // Advance only after accepted delivery; retained for old fixtures/manual code-driven checks.
    public Enemy SpawnNext(SpawnManager spawns, bool portalOnly = true, PackedScene fallback = null)
    {
        var scenes = spawns.Catalog?.Scenes;
        if (scenes == null || scenes.Count == 0) return fallback == null ? null : spawns.Spawn(fallback, portalOnly);
        int index = _next % scenes.Count; Enemy enemy = spawns.Spawn(scenes[index], portalOnly);
        if (enemy != null) _next = (index + 1) % scenes.Count;
        return enemy;
    }
}
