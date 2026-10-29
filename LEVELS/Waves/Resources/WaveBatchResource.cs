// Editor-authored batch data; Count is total spawns, Interval is between successful spawns.
using Godot;

[Tool, GlobalClass]
public partial class WaveBatchResource : Resource
{
    [Export(PropertyHint.Range, "1,100000,1,or_greater")] public int Count { get; set; } = 10;
    [Export(PropertyHint.Range, "0,3600,0.05,or_greater")] public double Interval { get; set; } = 0.8;
    [Export(PropertyHint.Range, "0,3600,0.05,or_greater")] public double After { get; set; }
    [Export] public Godot.Collections.Array<WaveEnemyResource> Enemies { get; set; } = new();

    // =========================================================
    // Avoid sharing nested entries between duplicates; original arrays remain untouched.
    public WaveBatchResource Copy()
    {
        var copy = new WaveBatchResource { Count = Count, Interval = Interval, After = After };
        foreach (WaveEnemyResource enemy in Enemies) copy.Enemies.Add(enemy?.Copy());
        return copy;
    }
}
