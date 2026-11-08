// Short shared impact-ring presentation for status application; never owns effects, damage or ticks.
using Godot;
public static class StatusEffectFeedback
{
    private static ShockwaveStyle _corruption;
    public static void Applied(Enemy enemy, StatusEffectId id)
    {
        if (id != StatusEffectId.Corruption) return;
        _corruption ??= GD.Load<ShockwaveStyle>("res://FEEDBACK/StatusEffects/CorruptionImpact.tres");
        ShockwaveEffect.Spawn(enemy.GetNodeOrNull<Node2D>("../../Effects"), enemy.VisualWorldCentre, _corruption,
            Mathf.Clamp(enemy.Definition.RadiusPixels * 1.2f, 8, 36) * enemy.AppliedVisualScale);
    }
}
