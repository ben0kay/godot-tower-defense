// Shared authoring for one shield pulse; no runtime timers, recipient state or global effects here.
using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class ShieldEmitterConfig : Resource
{
    public enum TargetEligibility { AnyShieldCapacity, AlreadyShieldedOnly }
    private float _radius = 192, _amount = 20, _interval = 1;
    private int _candidates = 128, _recipients = 32;
    private bool _self;
    private TargetEligibility _eligibility;
    [ExportGroup("Shield Pulse")]
    [Export(PropertyHint.Range, "1,1024,8,or_greater")] public float RadiusPixels { get => _radius; set => Change(ref _radius, value); }
    [Export(PropertyHint.Range, "0,500,1,or_greater")] public float ShieldPerPulse { get => _amount; set => Change(ref _amount, value); }
    [Export(PropertyHint.Range, "0.1,10,0.1,or_greater")] public float PulseIntervalSeconds { get => _interval; set => Change(ref _interval, value); }
    [ExportGroup("Eligibility")]
    [Export] public bool AffectSelf { get => _self; set => Change(ref _self, value); }
    [Export] public TargetEligibility Eligibility { get => _eligibility; set => Change(ref _eligibility, value); }
    [ExportGroup("Query Budget")]
    [Export(PropertyHint.Range, "1,512,1")] public int MaxCandidatesPerPulse { get => _candidates; set => Change(ref _candidates, value); }
    [Export(PropertyHint.Range, "1,512,1")] public int MaxRecipientsPerPulse { get => _recipients; set => Change(ref _recipients, value); }
    public bool IsValid => float.IsFinite(RadiusPixels) && RadiusPixels > 0 && float.IsFinite(ShieldPerPulse) && ShieldPerPulse >= 0 &&
        float.IsFinite(PulseIntervalSeconds) && PulseIntervalSeconds >= 0.1f && Enum.IsDefined(Eligibility) &&
        MaxCandidatesPerPulse > 0 && MaxRecipientsPerPulse > 0;
    // =========================================================
    // Inspector/C# authoring edits notify only subscribed optional components.
    private void Change<T>(ref T field, T value)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; EmitChanged(); }
}
