// Pure research eligibility/purchase and explicit gameplay effect resolution, independent of UI/save I/O.
using System;
using System.Collections.Generic;
using System.Linq;

public enum ResearchState { Locked, Available, InsufficientPoints, Researched }

public static class ResearchRules
{
    // =========================================================
    // Core is a virtual free prerequisite; it never consumes RP or appears in saved purchases.
    public static ResearchState State(PlayerProgression player, ResearchNode node)
    {
        if (node.Id == "research_core" || player.PurchasedResearchIds.Contains(node.Id)) return ResearchState.Researched;
        if (node.Prerequisites.Any(id => id != "research_core" && !player.PurchasedResearchIds.Contains(id))) return ResearchState.Locked;
        return player.UnspentResearchPoints < node.Cost ? ResearchState.InsufficientPoints : ResearchState.Available;
    }

    // =========================================================
    // Candidate payment and ownership change together; persistence must commit this once.
    public static PlayerProgression Purchase(PlayerProgression player, ResearchNode node)
    {
        if (State(player, node) != ResearchState.Available)
            throw new InvalidOperationException("Research is locked, already owned or requires more Research Points.");
        return player with { UnspentResearchPoints = checked(player.UnspentResearchPoints - node.Cost),
            PurchasedResearchIds = player.PurchasedResearchIds.Add(node.Id) };
    }

    // =========================================================
    // Always start from authored maximum; opening menus never compounds existing HP.
    public static int CitadelMaximum(int authored, PlayerProgression player, IReadOnlyDictionary<string, ResearchNode> nodes)
    {
        return ResearchDeployment.Resolve(player, nodes).CitadelMaximum(authored);
    }
}
