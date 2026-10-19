// Explicit data-only campaign roster, ordered by mission number; no filesystem scans or unlock state.
using Godot;
using System;
using System.Linq;

[GlobalClass]
public partial class MissionCatalog : Resource
{
    [Export] public Godot.Collections.Array<MissionDefinition> Missions { get; set; } = new();
    public static MissionCatalog Default => GD.Load<MissionCatalog>("res://LEVELS/Catalog/DefaultMissionCatalog.tres");
    // =========================================================
    // Validate enabled entries once per menu/deployment lookup and reject ambiguous mission identities.
    public MissionDefinition[] Entries()
    {
        if (Missions == null || Missions.Any(m => m == null)) throw new InvalidOperationException("Mission catalog contains an empty entry.");
        var entries = Missions.Where(m => m.Enabled).OrderBy(m => m.Number).ToArray();
        if (entries.Select(m => m.Number).Distinct().Count() != entries.Length)
            throw new InvalidOperationException("Mission catalog has duplicate enabled numbers.");
        foreach (var mission in entries) mission.ValidateMetadata();
        return entries;
    }
}
