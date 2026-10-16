// Global ammunition policy; per-tower resupply thresholds are intentionally deferred.
using Godot;

[GlobalClass]
public partial class AmmunitionMasterConfig : Resource
{
    [ExportGroup("Resupply")]
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ResupplyThresholdFraction
    {
        get => _threshold;
        set { if (_threshold == value) return; _threshold = value; EmitChanged(); }
    }
    private float _threshold = .10f;

    // =========================================================
    // Compare actual rounds to a fraction of the CURRENT max, not a hardcoded quantity.
    // An absent/disabled ammo capability never requests resupply.
    public bool NeedsResupply(int currentAmmo, int maxAmmo)
    {
        if (maxAmmo <= 0 || currentAmmo < 0) return false;
        float threshold = float.IsFinite(ResupplyThresholdFraction)
            ? Mathf.Clamp(ResupplyThresholdFraction, 0f, 1f) : 0.10f;
        return currentAmmo <= (double)maxAmmo * threshold;
    }
}
