// Shared eligibility and tuning for splash impacts that can briefly stagger ships.
using Godot;

internal static class ImpactStagger
{
    private static FeedbackMasterConfig _settings;
    internal static FeedbackMasterConfig Settings => _settings ??=
        GD.Load<FeedbackMasterConfig>("res://FEEDBACK/DefaultFeedbackConfig.tres") ?? new FeedbackMasterConfig();

    internal const int VisualLimit = 64;

    internal static float Strength(EnemyClass enemyClass, int power, float proximity)
    {
        int resistance = enemyClass switch
        {
            EnemyClass.Tiny when power >= 2 => 1,
            EnemyClass.Light => 1,
            EnemyClass.Standard => 2,
            _ => 0
        };
        if (resistance == 0 || power < resistance) return 0;
        return (power == resistance ? .5f : 1f) * proximity;
    }
}
