// Inspector-editable presentation only; no hit amount, falloff, enemy selection or wave timing authority.
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class ShockwaveStyle : Resource
{
    private Color _ring = Colors.White, _glow = new(1, 1, 1, .2f);
    private float _width = 2, _opacity = 1, _glowWidth, _duration = .4f, _radius = 128;
    private float _start, _fadeStart = .5f, _fadePower = 1, _fill;
    private Texture2D _artwork;
    [ExportGroup("Ring")]
    [Export] public Color RingColor { get => _ring; set => Change(ref _ring, value); }
    [Export] public float RingWidth { get => _width; set => Change(ref _width, value); }
    [Export(PropertyHint.Range, "0,1,0.01")] public float Opacity { get => _opacity; set => Change(ref _opacity, value); }
    [ExportGroup("Outer Glow")]
    [Export] public Color GlowColor { get => _glow; set => Change(ref _glow, value); }
    [Export] public float GlowWidth { get => _glowWidth; set => Change(ref _glowWidth, value); }
    [ExportGroup("Autonomous Expansion")]
    [Export] public float ExpansionDuration { get => _duration; set => Change(ref _duration, value); }
    [Export] public float VisualRadius { get => _radius; set => Change(ref _radius, value); }
    [Export(PropertyHint.Range, "0,1,0.01")] public float StartRadiusFraction { get => _start; set => Change(ref _start, value); }
    [ExportGroup("Fade and Artwork")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float FadeStartFraction { get => _fadeStart; set => Change(ref _fadeStart, value); }
    [Export] public float FadePower { get => _fadePower; set => Change(ref _fadePower, value); }
    [Export(PropertyHint.Range, "0,1,0.01")] public float FillOpacity { get => _fill; set => Change(ref _fill, value); }
    [Export] public Texture2D Artwork { get => _artwork; set => Change(ref _artwork, value); }

    // =========================================================
    // Restyling refreshes only the effect, never a tower's FinalStats or damage delivery.
    private void Change<T>(ref T field, T value)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; EmitChanged(); }
}
