// Stateless shared health-to-art mapping; no nodes, processing loops or enemy-type switches.
using System;

public static class EnemyDamageAppearance
{
    // =========================================================
    // With N textures, full HP starts at 0 and each 1/N hull-health loss advances one stage.
    public static int StageForHealth(float health, float maxHealth, int stageCount)
    {
        if (stageCount <= 0) return -1;
        if (!float.IsFinite(health) || !float.IsFinite(maxHealth) || maxHealth <= 0) return 0;
        double fraction = Math.Clamp((double)health / maxHealth, 0d, 1d);
        return Math.Clamp((int)Math.Floor((1d - fraction) * stageCount), 0, stageCount - 1);
    }
}
