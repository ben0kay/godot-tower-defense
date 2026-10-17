// Presentation-only controls shared by all continuous beam weapons.
// Every renderer clones this resource; none of these values affect combat stats.
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class BeamStyle : Resource
{
    #region Existing core / halo
    private Color _core = new(0.8f, 1f, 1f), _outer = new(0.1f, 0.8f, 1f, 0.35f);
    private float _coreWidth = 3f, _outerWidth = 9f, _pulseAmount = 0.06f, _pulseFrequency = 2f;

    [ExportGroup("Color")]
    [Export] public Color CoreColor { get => _core; set => Change(ref _core, value); }
    [Export] public Color OuterColor { get => _outer; set => Change(ref _outer, value); }
    [ExportGroup("Width")]
    [Export(PropertyHint.Range, "0,32,0.5")] public float CoreWidth { get => _coreWidth; set => Change(ref _coreWidth, value); }
    [Export(PropertyHint.Range, "0,64,0.5")] public float OuterWidth { get => _outerWidth; set => Change(ref _outerWidth, value); }
    [ExportGroup("Width Pulse")]
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float PulseAmount { get => _pulseAmount; set => Change(ref _pulseAmount, value); }
    [Export(PropertyHint.Range, "0,10,0.1")] public float PulseFrequency { get => _pulseFrequency; set => Change(ref _pulseFrequency, value); }
    #endregion

    #region Travelling ripple (zero amplitude disables it)
    private float _rippleAmplitude = 0f, _rippleWavelength = 45f, _rippleSpeed = 55f;
    private float _rippleEndFade = 0.15f, _outerRippleScale = 1.3f, _rippleIrregularity = 0f;
    private float _pointSpacing = 18f;

    [ExportGroup("Ripple")]
    [Export(PropertyHint.Range, "0,12,0.1")] public float RippleAmplitude { get => _rippleAmplitude; set => Change(ref _rippleAmplitude, value); }
    [Export(PropertyHint.Range, "8,180,1")] public float RippleWavelength { get => _rippleWavelength; set => Change(ref _rippleWavelength, value); }
    [Export(PropertyHint.Range, "0,400,1")] public float RippleTravelSpeed { get => _rippleSpeed; set => Change(ref _rippleSpeed, value); }
    [Export(PropertyHint.Range, "0,0.45,0.01")] public float RippleEndFadeFraction { get => _rippleEndFade; set => Change(ref _rippleEndFade, value); }
    [Export(PropertyHint.Range, "0,3,0.05")] public float OuterRippleScale { get => _outerRippleScale; set => Change(ref _outerRippleScale, value); }
    [Export(PropertyHint.Range, "0,1,0.01")] public float RippleIrregularity { get => _rippleIrregularity; set => Change(ref _rippleIrregularity, value); }
    [Export(PropertyHint.Range, "10,50,1")] public float PointSpacing { get => _pointSpacing; set => Change(ref _pointSpacing, value); }
    #endregion

    #region Impact and optional owned particle emitters
    private float _impactWanderRadius = 0f, _impactWanderFrequency = 1.8f;
    private float _impactGlowRadius = 0f, _impactGlowOpacity = 0.4f, _impactGlowFlicker = 0.08f;
    private bool _impactParticles, _muzzleParticles;

    [ExportGroup("Impact Contact")]
    [Export(PropertyHint.Range, "0,12,0.1")] public float ImpactWanderRadius { get => _impactWanderRadius; set => Change(ref _impactWanderRadius, value); }
    [Export(PropertyHint.Range, "0,15,0.1")] public float ImpactWanderFrequency { get => _impactWanderFrequency; set => Change(ref _impactWanderFrequency, value); }
    [Export(PropertyHint.Range, "0,15,0.5")] public float ImpactGlowRadius { get => _impactGlowRadius; set => Change(ref _impactGlowRadius, value); }
    [Export(PropertyHint.Range, "0,1,0.01")] public float ImpactGlowOpacity { get => _impactGlowOpacity; set => Change(ref _impactGlowOpacity, value); }
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float ImpactGlowFlicker { get => _impactGlowFlicker; set => Change(ref _impactGlowFlicker, value); }

    [ExportGroup("Optional Particles")]
    [Export] public bool ImpactParticlesEnabled { get => _impactParticles; set => Change(ref _impactParticles, value); }
    [Export] public bool MuzzleParticlesEnabled { get => _muzzleParticles; set => Change(ref _muzzleParticles, value); }
    #endregion

    // =========================================================
    // Live Inspector edits update presentation, never the weapon's stat pipeline.
    private void Change<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; EmitChanged();
    }
}
