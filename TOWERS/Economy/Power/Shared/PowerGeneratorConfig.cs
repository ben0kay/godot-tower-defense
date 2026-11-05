// Shared passive generator inputs; each generator type owns its authored resource, footprint, artwork and stable ID.
using Godot;
using System;

[GlobalClass]
public partial class PowerGeneratorConfig : TowerConfig
{
    private float _output = 250;
    [ExportGroup("Generation")]
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
    [ExportGroup("Construction")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")]
    public int ConstructionCredits
    {
        get => ConstructionCosts[0].Amount;
        set { int next = Math.Max(0, value); if (ConstructionCosts[0].Amount == next) return; ConstructionCosts[0].Amount = next; EmitChanged(); }
    }

    // =========================================================
    // Passive buildings use the existing recipe/work fields and no artificial weapon or purchased-level rows.
    public PowerGeneratorConfig()
    {
        DisplayName = "Power Generator";
        Description = "Supplies electricity to its connected terrain island while operational.";
        ConstructionCosts = new TowerLevel(1, 300, default).Costs;
        ConstructionSeconds = 5; MaxHealth = 1200;
    }

    // =========================================================
    // Fail catalog/placement preflight before registering invalid output or silently adding unsupported paid upgrades.
    public bool ValidGeneration => float.IsFinite(Output) && Output >= 0 && float.IsFinite(MaxHealth) && MaxHealth > 0 && Levels.Count == 0;
    public override string DescribeStats(TowerFinalStats final) => $"Power output: {Output:0.##}\nLocal island supply · no fuel required";
}
