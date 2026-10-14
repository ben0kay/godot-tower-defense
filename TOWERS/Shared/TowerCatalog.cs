// Inspector-authored registry; publish a lookup only after all entries validate successfully.
using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class TowerCatalog : Resource
{
    [Export] public Godot.Collections.Array<TowerDefinition> Definitions { get; set; } = new();
    private Dictionary<string, TowerDefinition> _byId;

    // =========================================================
    // Call after authoring changes; duplicate IDs and malformed assets fail before any build occurs.
    public void Rebuild()
    {
        _byId = null;
        if (Definitions == null) throw new InvalidOperationException("Tower catalog is missing entries.");
        Dictionary<string, TowerDefinition> next = new(StringComparer.Ordinal);
        foreach (TowerDefinition entry in Definitions)
        {
            if (entry == null) throw new InvalidOperationException("Null tower definition.");
            entry.Validate();
            if (!next.TryAdd(entry.TowerId, entry)) throw new InvalidOperationException("Duplicate tower ID: " + entry.TowerId);
        }
        _byId = next;
    }

    // =========================================================
    // Unavailable types remain registered but cannot become placement choices.
    public TowerDefinition Find(string id)
    {
        if (_byId == null) Rebuild();
        return id != null && _byId.TryGetValue(id, out TowerDefinition entry) ? entry : null;
    }
}
