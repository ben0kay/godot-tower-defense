// Optional visual-only tuning; shared resources hold no per-tower animation state.
using Godot;

public readonly record struct TowerRecoilSnapshot(bool Enabled, float Distance, double KickSeconds, double ReturnSeconds, float Maximum);

[GlobalClass]
public partial class TowerRecoilStyle : Resource
{
    #region Inspector
    private bool _enabled;
    private float _distance = 2, _maximum = 4;
    private double _kick = .045, _return = .15;
    [Export] public bool Enabled { get => _enabled; set { if (_enabled == value) return; _enabled = value; EmitChanged(); } }
    [ExportGroup("Motion")]
    [Export(PropertyHint.Range, "0,8,0.1,suffix:px")]
    public float KickDistancePixels { get => _distance; set { if (_distance == value) return; _distance = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.005,1,0.005,suffix:s")]
    public double KickSeconds { get => _kick; set { if (_kick == value) return; _kick = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.005,1,0.005,suffix:s")]
    public double ReturnSeconds { get => _return; set { if (_return == value) return; _return = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0,8,0.1,suffix:px")]
    public float MaximumOffsetPixels { get => _maximum; set { if (_maximum == value) return; _maximum = value; EmitChanged(); } }
    #endregion

    // =========================================================
    // Finite bounded values protect live Inspector edits without changing any combat stat.
    public TowerRecoilSnapshot Capture()
    {
        float maximum = float.IsFinite(_maximum) ? Mathf.Clamp(_maximum, 0, 8) : 4;
        float distance = float.IsFinite(_distance) ? Mathf.Clamp(_distance, 0, maximum) : Mathf.Min(2, maximum);
        double kick = double.IsFinite(_kick) ? System.Math.Clamp(_kick, .005, 1) : .045;
        double settle = double.IsFinite(_return) ? System.Math.Clamp(_return, .005, 1) : .15;
        return new(_enabled && maximum > 0 && distance > 0, distance, kick, settle, maximum);
    }
}
