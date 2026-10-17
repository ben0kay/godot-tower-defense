// Frozen mission research inputs, resolved once per deployment from the selected profile's purchased IDs.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public sealed class ResearchDeployment
{
    public static ResearchDeployment None { get; } = new("", 0, ImmutableDictionary<string, TowerResearchBonuses>.Empty);
    public string ProfileId { get; }
    public double CitadelHealthPercent { get; }
    private readonly ImmutableDictionary<string, TowerResearchBonuses> _towers;

    // =========================================================
    // The snapshot owns immutable values; later profile changes cannot alter an active mission.
    private ResearchDeployment(string profileId, double citadel, ImmutableDictionary<string, TowerResearchBonuses> towers)
    { ProfileId = profileId; CitadelHealthPercent = citadel; _towers = towers; }

    // =========================================================
    // Resolve explicit effect kinds once; repeat percentage nodes for a type add against the same stat input.
    public static ResearchDeployment Resolve(PlayerProgression player, IReadOnlyDictionary<string, ResearchNode> nodes)
    {
        double citadel = 0;
        var towers = ImmutableDictionary.CreateBuilder<string, TowerResearchBonuses>(StringComparer.Ordinal);
        foreach (string id in player.PurchasedResearchIds)
        {
            if (!nodes.TryGetValue(id, out ResearchNode node)) continue; // Preserve retired IDs without inventing effects.
            if (node.Effect == ResearchEffectKind.CitadelMaxHealthPercent) citadel += node.Percent;
            else if (node.Effect == ResearchEffectKind.TowerDamagePercent)
            {
                towers.TryGetValue(node.TowerId, out TowerResearchBonuses current);
                towers[node.TowerId] = current with { DamagePercent = current.DamagePercent + node.Percent };
            }
        }
        return new(player.ProfileId, citadel, towers.ToImmutable());
    }

    // =========================================================
    // A missing tower entry is neutral, including all direct profileless test instances.
    public TowerResearchBonuses ForTower(string id) => _towers.TryGetValue(id ?? "", out var value) ? value : default;

    // =========================================================
    // Effective HP always starts from authored health rather than a previously multiplied value.
    public int CitadelMaximum(int authored)
    {
        if (authored <= 0) throw new ArgumentOutOfRangeException(nameof(authored));
        return checked((int)Math.Round(authored * (1 + CitadelHealthPercent / 100), MidpointRounding.AwayFromZero));
    }
}
