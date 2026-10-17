// Campaign-room purchase boundary and cached validated research catalog; no battlefield purchase path.
using Godot;
using System;
using System.Collections.Generic;

public static class ResearchService
{
    private static IReadOnlyDictionary<string, ResearchNode> _nodes;
    public static IReadOnlyDictionary<string, ResearchNode> Nodes => _nodes ??= LoadCatalog();

    // =========================================================
    // Freeze the resource catalog for the process; new authoring is read on next game launch.
    private static IReadOnlyDictionary<string, ResearchNode> LoadCatalog() =>
        (GD.Load<ResearchCatalog>("res://SYSTEMS/Progression/Research/DefaultResearchCatalog.tres") ??
            throw new InvalidOperationException("Research catalog is missing.")).Build();

    // =========================================================
    // The public command checks the actual active research scene, not a caller-provided boolean.
    public static PlayerProgression Purchase(Node context, string id)
    {
        if (context is not ResearchMenu || !context.IsInsideTree() || context.IsQueuedForDeletion() ||
            context.GetTree().CurrentScene != context || context.SceneFilePath != MenuNavigation.ResearchScene ||
            context.GetTree().Paused)
            throw new InvalidOperationException("Research can only be purchased in the Campaign Research Room.");
        if (!Nodes.TryGetValue(id, out ResearchNode node)) throw new ArgumentException("Unknown research ID.");
        PlayerProgression before = ProgressionService.GetCurrent();
        PlayerProgression after = ResearchRules.Purchase(before, node);
        ProgressionService.CommitResearch(after);
        return after;
    }

    // =========================================================
    // Profileless F6 tests retain their authored health and never create a profile.
    public static ResearchDeployment CaptureDeployment() => ProfileStore.Selected == null ? ResearchDeployment.None :
        ResearchDeployment.Resolve(ProgressionService.GetCurrent(), Nodes);

    // =========================================================
    // Compatibility/tooling accessor; battlefield owners retain CaptureDeployment instead of repeatedly reading profiles.
    public static int ResolveCitadelMaximum(int authored) => CaptureDeployment().CitadelMaximum(authored);
}
