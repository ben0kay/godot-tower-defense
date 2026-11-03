// One shared Inspector profile for paid tower construction and upgrade sparks.
using Godot;

[GlobalClass]
public partial class ConstructionSparkConfig : Resource
{
    [ExportGroup("Construction Sparks")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "1,64,1")] public int MaxConcurrentSites { get; set; } = 24;
    [Export(PropertyHint.Range, "1,32,1")] public int ParticlesPerSite { get; set; } = 8;
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float ParticleLifetime { get; set; } = .48f;
    [Export(PropertyHint.Range, "0.15,1,0.05")] public float FootprintCoverage { get; set; } = .55f;
    [Export] public Color SparkTint { get; set; } = new(1f, .72f, .36f, .92f);

    // =========================================================
    // Crisp workshop-welding flecks are a separate GPU layer; orange background flecks remain.
    [ExportGroup("Hot Welding Layer")]
    [Export] public bool WeldingEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "1,24,1")] public int WeldingParticlesPerSite { get; set; } = 8;
    [Export(PropertyHint.Range, "0.1,0.8,0.01")] public float WeldingLifetime { get; set; } = .34f;
    [Export(PropertyHint.Range, "0.15,1,0.05")] public float WeldingFootprintCoverage { get; set; } = .85f;
    [Export] public Color WeldingTint { get; set; } = new(1f, 1f, .96f, 1f);
}
