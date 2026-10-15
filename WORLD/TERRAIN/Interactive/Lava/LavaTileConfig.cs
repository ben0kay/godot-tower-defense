// Inspector-authored environmental hits; no tower source, secondary resolver or hidden balance constants.
using Godot;
using System;

[GlobalClass]
public partial class LavaTileConfig : Resource
{
    private float _damage = 5, _interval = .5f;
    private bool _entry = true;
    private DamageType _type = DamageType.Energy;
    [ExportGroup("Lava Damage")]
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float DamagePerTick
    { get => _damage; set { _damage = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.01,60,0.01,or_greater")] public float DamageIntervalSeconds
    { get => _interval; set { _interval = value; EmitChanged(); } }
    [Export] public bool DamageOnEntry
    { get => _entry; set { _entry = value; EmitChanged(); } }
    [Export] public DamageType DamageType
    { get => _type; set { _type = value; EmitChanged(); } }

    // =========================================================
    // Reject malformed runtime/editor input before it can create an infinite timer loop or partial hit.
    public void Validate()
    {
        if (!float.IsFinite(_damage) || _damage < 0 || !float.IsFinite(_interval) || _interval <= 0 || !Enum.IsDefined(_type))
            throw new InvalidOperationException("Lava requires finite nonnegative damage, a finite positive interval and a supported damage type.");
    }
}
