// Catalog validation freezes authored resources into immutable runtime definitions.
using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public sealed record ResearchNode(string Id, string Name, string Description, int Cost,
    ImmutableArray<string> Prerequisites, Vector2I Axial, ResearchEffectKind Effect, double Percent);

[GlobalClass]
public partial class ResearchCatalog : Resource
{
    [Export] public Godot.Collections.Array<ResearchDefinition> Definitions { get; set; } = new();

    // =========================================================
    // Validate the whole graph once on load; no dangling edges, cycles or overlapping coordinates.
    public IReadOnlyDictionary<string, ResearchNode> Build()
    {
        if (Definitions == null || Definitions.Count == 0 || Definitions.Count > 4096)
            throw new InvalidOperationException("Research catalog must contain 1–4096 nodes.");
        Dictionary<string, ResearchNode> nodes = new(StringComparer.Ordinal);
        HashSet<Vector2I> coordinates = new();
        foreach (ResearchDefinition definition in Definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || definition.Id.Length > 128 ||
                string.IsNullOrWhiteSpace(definition.DisplayName) || definition.PrerequisiteIds == null ||
                !Enum.IsDefined(definition.Effect) || !double.IsFinite(definition.EffectPercent) ||
                definition.EffectPercent < 0 || definition.EffectPercent > 10000 ||
                Math.Abs((long)definition.AxialPosition.X) > 10000 || Math.Abs((long)definition.AxialPosition.Y) > 10000)
                throw new InvalidOperationException("Malformed research definition.");
            bool core = definition.Id == "research_core";
            if (core ? definition.CostRP != 0 || definition.AxialPosition != Vector2I.Zero ||
                definition.PrerequisiteIds.Length != 0 || definition.Effect != ResearchEffectKind.None || definition.EffectPercent != 0
                : definition.CostRP <= 0 || definition.PrerequisiteIds.Length == 0 ||
                  definition.Effect == ResearchEffectKind.None || definition.EffectPercent <= 0)
                throw new InvalidOperationException("Invalid research cost, root or effect.");
            var node = new ResearchNode(definition.Id, definition.DisplayName, definition.Description, definition.CostRP,
                definition.PrerequisiteIds.ToImmutableArray(), definition.AxialPosition, definition.Effect, definition.EffectPercent);
            if (!nodes.TryAdd(node.Id, node) || !coordinates.Add(node.Axial))
                throw new InvalidOperationException("Duplicate research ID or axial coordinate.");
        }
        if (!nodes.ContainsKey("research_core")) throw new InvalidOperationException("Research Core is missing.");
        Dictionary<string, int> marks = new(StringComparer.Ordinal);
        foreach (string id in nodes.Keys) Visit(id, nodes, marks);
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, ResearchNode>(nodes);
    }

    // =========================================================
    // Every non-root node has prerequisites and must ultimately connect to the single free core.
    private static void Visit(string id, Dictionary<string, ResearchNode> nodes, Dictionary<string, int> marks)
    {
        if (marks.TryGetValue(id, out int mark))
        { if (mark == 1) throw new InvalidOperationException("Research prerequisite cycle."); return; }
        marks[id] = 1;
        foreach (string prerequisite in nodes[id].Prerequisites)
        {
            if (prerequisite == null || !nodes.ContainsKey(prerequisite))
                throw new InvalidOperationException("Unknown research prerequisite.");
            Visit(prerequisite, nodes, marks);
        }
        marks[id] = 2;
    }
}
