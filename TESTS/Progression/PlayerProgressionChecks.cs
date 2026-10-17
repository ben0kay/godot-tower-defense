// Isolated arithmetic/storage checks; temporary directory is always removed.
using Godot;
using System;
using System.IO;

public partial class PlayerProgressionChecks : Node
{
    // =========================================================
    // Run deterministic progression checks without selecting or modifying real profiles.
    public override void _Ready()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-progression-" + Guid.NewGuid().ToString("N"));
        try
        {
            string a = Guid.NewGuid().ToString("N"), b = Guid.NewGuid().ToString("N");
            ProgressionStore store = new(root);
            PlayerProgression start = store.Load(a);
            Check(start.Level == 1 && start.CurrentXP == 0 && start.RequiredXP == 500, "new profile");
            Check(!Directory.Exists(root), "reading missing data does not write");
            PlayerProgression exact = start.AddXP(500);
            Check(exact.Level == 2 && exact.CurrentXP == 0 && exact.UnspentResearchPoints == 1, "exact threshold");
            PlayerProgression carry = start.AddXP(600);
            Check(carry.Level == 2 && carry.CurrentXP == 100 && carry.RequiredXP == 750, "carry");
            PlayerProgression multi = start.AddXP(1500);
            Check(multi.Level == 3 && multi.CurrentXP == 250 && multi.UnspentResearchPoints == 2, "multiple levels");
            Check(multi.AddXP(0) == multi, "zero grant");
            Expect(() => start.AddXP(-1), "negative grant");
            Expect(() => PlayerLevelCurve.LevelAt(0, 0, 250), "invalid curve");
            Expect(() => (start with { LifetimePlayerXP = long.MaxValue }).AddXP(1), "overflow");
            store.Save(carry);
            Check(new ProgressionStore(root).Load(a) == carry, "disk reload");
            Check(store.Load(b).Level == 1, "profile isolation");
            Check(store.Load(a, 100, 0).RequiredXP == 750, "stored curve survives balance changes");
            string path = Path.Combine(root, a, "progression.json");
            store.Save(multi); // carry becomes the backup
            File.WriteAllText(path, "{broken");
            Check(store.Load(a) == carry && store.RecoveryMessage.Length > 0, "backup recovery");
            store.Save(carry.AddXP(10));
            Check(store.Load(a).CurrentXP == 110, "save after recovery");
            File.WriteAllText(path, "{}");
            File.WriteAllText(path + ".bak", "{}");
            Expect(() => store.Load(a), "corruption never resets");
            Expect(() => store.Load("../escape"), "GUID validation");
            string blocked = Path.Combine(root, "blocked");
            File.WriteAllText(blocked, "file");
            Expect(() => new ProgressionStore(blocked).Save(start), "save failure");
            GD.Print("PlayerProgressionChecks: PASS");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    // =========================================================
    // Fail immediately with a concrete scenario name.
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    // =========================================================
    // Invalid inputs/storage failures must be observable to callers.
    private static void Expect(Action action, string message)
    {
        try { action(); } catch { return; }
        throw new Exception("Expected rejection: " + message);
    }
}
