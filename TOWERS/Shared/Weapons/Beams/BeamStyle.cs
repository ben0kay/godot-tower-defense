// Beam presentation only; no targeting, damage, economy or stat-pipeline values.
using Godot;
using System.Collections.Generic;
[GlobalClass]
public partial class BeamStyle : Resource
{
    private Color _core = new(0.8f, 1, 1), _outer = new(0.1f, 0.8f, 1, 0.35f);
    private float _coreWidth = 3, _outerWidth = 9, _pulseAmount = 0.06f, _pulseFrequency = 2;
    [ExportGroup("Color")]
    [Export] public Color CoreColor { get => _core; set => Change(ref _core, value); }
    [Export] public Color OuterColor { get => _outer; set => Change(ref _outer, value); }
    [ExportGroup("Width")]
    [Export(PropertyHint.Range, "0,32,0.5")] public float CoreWidth { get => _coreWidth; set => Change(ref _coreWidth, value); }
    [Export(PropertyHint.Range, "0,64,0.5")] public float OuterWidth { get => _outerWidth; set => Change(ref _outerWidth, value); }
    [ExportGroup("Pulse")]
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float PulseAmount { get => _pulseAmount; set => Change(ref _pulseAmount, value); }
    [Export(PropertyHint.Range, "0,10,0.1")] public float PulseFrequency { get => _pulseFrequency; set => Change(ref _pulseFrequency, value); }
    // =========================================================
    // Inspector/C# visual edits refresh this renderer without rebuilding gameplay stats.
    private void Change<T>(ref T field, T value)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; EmitChanged(); }
}
