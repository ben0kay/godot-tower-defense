// Authored baseline Citadel generation; independent of hull health, Credits production and wave activity.
using Godot;
using System;

[GlobalClass]
public partial class CitadelPowerConfig : Resource
{
    private float _output = 100f;
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")]
    public float Output
    {
        get => _output;
        set
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_output == value) return;
            _output = value; EmitChanged();
        }
    }
}
