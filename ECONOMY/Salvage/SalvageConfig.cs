// Mission-local scrap and metal dispatch tuning.
using Godot;

[GlobalClass]
public partial class SalvageConfig : Resource
{
    [Export(PropertyHint.Range, "0,1,.001")] public float ScrapChance { get; set; } = .01f;
    [Export(PropertyHint.Range, "1,64,1")] public int DropLimit { get; set; } = 64;
    [Export(PropertyHint.Range, "1,600,1")] public float DropLifetime { get; set; } = 120;
    [Export(PropertyHint.Range, "1,8,1")] public int SalvagePerCitadel { get; set; } = 2;
    [Export(PropertyHint.Range, "1,64,1")] public int FlightLimit { get; set; } = 24;
    [Export(PropertyHint.Range, "1,100,1")] public int CitadelScrapCapacity { get; set; } = 20;
    [Export(PropertyHint.Range, "1,100,1")] public int CourierCapacity { get; set; } = 10;
    [Export(PropertyHint.Range, "1,100,1")] public int BuilderCapacity { get; set; } = 50;
    [Export(PropertyHint.Range, "0,.95,.01")] public float PreparationFraction { get; set; } = .25f;
    [Export(PropertyHint.Range, "32,2000,1")] public float FlightSpeed { get; set; } = 280;
    [Export(PropertyHint.Range, "0,2,.05")] public float GrabSeconds { get; set; } = .25f;
}
