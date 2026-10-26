// Focused S1 disk checks in a disposable directory; never reads or changes actual user profiles.
using Godot;
using System;
using System.IO;
using System.Text.Json;

public partial class CampaignStoreChecks : Node
{
    // =========================================================
    // Exercise storage boundaries once; no gameplay simulation or long-running suite.
    public override void _Ready()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-saving-" + Guid.NewGuid().ToString("N"));
        try
        {
            string owner = Guid.NewGuid().ToString("N"), other = Guid.NewGuid().ToString("N");
            CampaignStore store = new(root);
            Check(store.Load(owner) == null && !Directory.Exists(root), "Missing slot is read-only.");
            CampaignData first = CampaignData.Create(owner);
            store.Save(owner, first);
            Check(new CampaignStore(root).Load(owner) == first, "Fresh store reload matches metadata.");
            Check(store.Load(other) == null, "Profiles are isolated.");
            Reject(() => store.Save(other, first));
            Reject(() => store.Load("../escape"));
            Reject(() => store.Save(owner, CampaignData.Create(owner)));
            CampaignData second = first with { SavedUtc = first.SavedUtc.AddSeconds(1) };
            store.Save(owner, second);
            string path = Path.Combine(root, owner, "Campaigns", "current.json");
            string primary = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
            Reject(() => store.Save(owner, first));
            Check(File.ReadAllText(path) == primary, "Stale write preserves primary.");
            File.WriteAllText(path, "broken");
            Check(store.Load(owner) == first && store.RecoveryMessage.Length > 0, "Valid backup recovers read-only.");
            Reject(() => store.Save(owner, second));
            Check(File.ReadAllText(path) == "broken" && File.ReadAllText(path + ".bak") == backup, "Corrupt evidence and backup preserved.");
            File.Delete(path + ".bak");
            foreach (string invalid in new[] { "{}", primary.Replace("\"Version\": 2", "\"Version\": 99"),
                primary.Replace(owner, other), primary.Replace("\"Version\": 2", "\"Version\": 2, \"Version\": 2"),
                primary.Replace("\"Version\": 2", "\"Unexpected\": 0, \"Version\": 2"), new string('x', CampaignStore.MaximumFileBytes + 1) })
            {
                File.WriteAllText(path, invalid);
                Reject(() => store.Load(owner));
                Check(File.ReadAllText(path) == invalid, "Invalid load preserves file.");
            }
            File.WriteAllText(path, primary);
            var checkpoint = SampleCheckpoint(owner);
            var complete = second with { SavedUtc = second.SavedUtc.AddSeconds(1), Checkpoint = checkpoint };
            store.Save(owner, complete);
            var loaded = new CampaignStore(root).Load(owner);
            Check(JsonSerializer.Serialize(loaded.Checkpoint) == JsonSerializer.Serialize(checkpoint), "Typed checkpoint round-trip includes stock, work, flights and frozen inputs.");
            string full = File.ReadAllText(path);
            File.Delete(path + ".bak");
            File.WriteAllText(path, full.Replace("\"CompletedWaves\": 1,", ""));
            Reject(() => store.Load(owner));
            File.WriteAllText(path, full);
            Reject(() => store.Save(owner, complete with { Checkpoint = checkpoint with { RewardTicket = checkpoint.RewardTicket with { ProfileId = other } } }));
            Check(File.ReadAllText(path) == full, "Cross-profile reward checkpoint cannot overwrite primary.");
            // Old metadata is still readable and can be explicitly upgraded without inventing a battlefield.
            File.WriteAllText(path, JsonSerializer.Serialize(first with { Version = 1 }));
            Check(store.Load(owner).Checkpoint == null, "Version-one metadata remains non-resumable.");
            Check(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories).Length == 0, "No operation temporary files remain.");
            GD.Print("CampaignStoreChecks: PASS (storage and typed checkpoint round-trip).");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    // =========================================================
    // Stop immediately on a failed storage invariant.
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    // =========================================================
    // Only expected validation/storage errors qualify as a safe rejection.
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException or JsonException) { return; }
        throw new Exception("Expected operation to reject invalid data.");
    }

    // =========================================================
    // Representative data fixture covers the cross-system format without touching real profiles or gameplay scenes.
    private static LevelSaveData SampleCheckpoint(string owner)
    {
        string factory = Guid.NewGuid().ToString("N"), depot = Guid.NewGuid().ToString("N"), site = Guid.NewGuid().ToString("N");
        return new()
        {
            LevelId = "Level001", ContentHash = new string('a', 64), CompletedWaves = 1, WaveRandomState = 125,
            RewardTicket = new(owner, 7, "Level001", new(500, 0)), Research = new(owner, 10, new() { ["battle"] = new(5) }),
            Run = new(12, 1, 3, 2, 1, 42.5), Balances = new() { ["credits"] = 1234 },
            Citadels = new() { new("Citadel", 95, 20, .5, false) }, CameraPosition = new(50, 70), CameraZoom = 1,
            Towers = new()
            {
                new() { Id = factory, TypeId = "ammo_factory", Tier = 1, Health = 500, Ammo = 20, Produced = 1000, AmmoSeconds = .5 },
                new() { Id = depot, TypeId = "ammo_depot", X = 5, Tier = 1, Health = 400, Ammo = 30 },
                new() { Id = site, TypeId = "battle", X = 10, Tier = 1, Health = 100, Ammo = 40, Held = true,
                    Work = new(TowerWorkPhase.Constructing, 0, 2, 0), Priority = TowerTargeting.Priority.Closest }
            },
            Logistics = new()
            {
                Jobs = new() { new(factory, depot, 1000, 1000, false, new(15, 20), 220), new(depot, "", 0, 0, true, new(10, 20), 220) },
                Factories = new() { factory }, Depots = new() { depot }, FactoryCursor = 1, RequestCursor = 0, FactoryTurn = true, Elapsed = .1,
                Lost = 4, Delivered = 60
            },
            Builders = new() { new() { Tower = site, HomeId = "Citadel", Phase = ConstructionDronePhase.Flying,
                Home = new(0, 0), Target = new(640, 0), Position = new(100, 0), HalfWidth = 20, Height = 48 } }
        };
    }
}
