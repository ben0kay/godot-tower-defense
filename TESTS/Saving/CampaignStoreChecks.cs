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
            foreach (string invalid in new[] { "{}", primary.Replace("\"Version\": 1", "\"Version\": 99"),
                primary.Replace(owner, other), primary.Replace("\"Version\": 1", "\"Version\": 1, \"Version\": 1"),
                primary.Replace("\"Version\": 1", "\"Unexpected\": 0, \"Version\": 1"), new string('x', CampaignStore.MaximumFileBytes + 1) })
            {
                File.WriteAllText(path, invalid);
                Reject(() => store.Load(owner));
                Check(File.ReadAllText(path) == invalid, "Invalid load preserves file.");
            }
            Check(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories).Length == 0, "No operation temporary files remain.");
            GD.Print("CampaignStoreChecks: PASS (metadata only; S2/S3 not implemented).");
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
}
