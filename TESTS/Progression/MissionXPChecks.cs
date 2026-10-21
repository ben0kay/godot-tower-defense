// Mission XP arithmetic, migration, atomic failure/retry and real paused Results/profile deployment integration.
using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using static MissionFixture;

public partial class MissionXPChecks : Node
{
    // =========================================================
    // All profile state is memory-only and all progression writes live in a disposable temporary directory.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string root = Path.Combine(Path.GetTempPath(), "td-mission-xp-" + Guid.NewGuid().ToString("N"));
        FieldInfo profiles = typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo storeField = typeof(ProgressionService).GetField("_store", BindingFlags.Static | BindingFlags.NonPublic);
        object oldProfiles = profiles.GetValue(null), oldStore = storeField.GetValue(null); PlayerProfile oldSelected = ProfileStore.Selected;
        try
        {
            Pure(new ProgressionStore(root), root);
            var a = new PlayerProfile { Id = Guid.NewGuid().ToString("N"), Name = "Mission A" };
            var b = new PlayerProfile { Id = Guid.NewGuid().ToString("N"), Name = "Mission B" };
            profiles.SetValue(null, new List<PlayerProfile> { a, b }); storeField.SetValue(null, new ProgressionStore(root));
            ProfileStore.Select(a.Id);
            foreach (string scene in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene })
            {
                long before = ProgressionService.GetCurrent().LifetimePlayerXP;
                using MissionFixture f = new(this, scene);
                f.Start().TakeDamage(999999); await Frames(); f.Results.SkipReveal();
                ProgressBar bar = (ProgressBar)f.Results.FindChild("PlayerXPBar", true, false);
                Require(f.Results.XPRevealCompleted && bar.Value == f.Session.Reward.After.CurrentXP &&
                    bar.MaxValue == f.Session.Reward.After.RequiredXP, "R2 Skip resolves to the persisted player XP value");
                Require(f.Session.Reward != null && f.Session.Reward.Breakdown.TotalXP == 500, "Real victory grants authored baseline on both maps");
                Require(ProgressionService.GetCurrent().LifetimePlayerXP == before + 500, "Outcome committed before Results actions");
                string heading = ((Label)f.Results.FindChild("MissionXPHeading", true, false)).Text;
                Require(heading.Contains("500 XP SAVED"), "Results shows saved reward");
                if (before == 0) Require(((Label)f.Results.FindChild("PlayerXPSummary", true, false)).Text.Contains("LEVEL UP"), "Results shows a crossed level and earned RP");
                await Geometry(f.Results);
                f.Results.ShowResults(f.Session.Snapshot); f.Results.SkipReveal();
                Require(ProgressionService.GetCurrent().LifetimePlayerXP == before + 500, "Showing/skipping presentation never awards");
            }
            ProfileStore.Select(b.Id);
            using (MissionFixture f = new(this))
            {
                f.Start(); f.Health.TakeDamage(int.MaxValue); await Frames(); f.Results.SkipReveal();
                Require(f.Session.Reward?.Breakdown.TotalXP == 0 && ProgressionService.GetCurrent().LifetimePlayerXP == 0,
                    "Defeat is honestly recorded with zero provisional XP");
            }
            using (MissionFixture f = new(this))
            {
                string blocked = Path.Combine(root, b.Id, "progression.json.tmp"); Directory.CreateDirectory(blocked);
                f.Start().TakeDamage(999999); await Frames(); f.Results.SkipReveal();
                Require(f.Session.Reward == null && f.Session.RewardError.Length > 0 && ProgressionService.GetCurrent().LifetimePlayerXP == 0,
                    "Failed disk commit does not claim or grant reward");
                Directory.Delete(blocked); ButtonFor(f.Results, "RETRY XP SAVE").EmitSignal(Button.SignalName.Pressed);
                Require(f.Session.Reward != null && ProgressionService.GetCurrent().LifetimePlayerXP == 500, "Paused Results retry commits once");
                Require(((ProgressBar)f.Results.FindChild("PlayerXPBar", true, false)).Value == f.Session.Reward.After.CurrentXP,
                    "R2 late save retry displays the final bar when reveal was skipped");
                ButtonFor(f.Results, "RETRY XP SAVE").EmitSignal(Button.SignalName.Pressed);
                Require(ProgressionService.GetCurrent().LifetimePlayerXP == 500, "Repeated retry cannot award twice");
            }
            using (MissionFixture f = new(this))
            {
                ProfileStore.Select(a.Id); f.Start().TakeDamage(999999); await Frames(); f.Results.SkipReveal();
                Require(f.Session.Reward == null && ProgressionService.GetCurrent().LifetimePlayerXP == 1000, "Profile switch never redirects mission reward");
                ProfileStore.Select(b.Id); ButtonFor(f.Results, "RETRY XP SAVE").EmitSignal(Button.SignalName.Pressed);
                Require(ProgressionService.GetCurrent().LifetimePlayerXP == 1000, "Restored deployment profile receives its own reward");
            }
            ProfileStore.ClearSelection();
            using (MissionFixture f = new(this))
            {
                f.Start().TakeDamage(999999); await Frames(); f.Results.SkipReveal();
                Require(f.Session.Reward == null && f.Session.RewardError.Length == 0 &&
                    ((Label)f.Results.FindChild("MissionXPHeading", true, false)).Text.Contains("NOT SAVED"), "Profileless F6 grants nothing and says so");
            }
            GD.Print("MissionXPChecks: PASS scoring, receipts/migration, once-only reload, RP, both outcomes/maps, failure/retry and profile isolation"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally
        {
            GetTree().Paused = false; ProfileStore.ClearSelection(); profiles.SetValue(null, oldProfiles); storeField.SetValue(null, oldStore);
            if (oldSelected != null) ProfileStore.Select(oldSelected.Id);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    // =========================================================
    // Persist watermarks with each award: duplicate reloads and older retired tickets cannot add XP/RP again.
    private static void Pure(ProgressionStore store, string root)
    {
        string id = Guid.NewGuid().ToString("N"); PlayerProgression player = store.Load(id);
        var policy = new MissionXPPolicy(1500, 25);
        var ticket = MissionRewardLedger.Begin(store, player, "Level001", policy);
        var win = new LevelResultSnapshot { LevelName = "Level001", Outcome = LevelOutcome.Victory };
        MissionRewardResult award = MissionRewardLedger.Award(store, ticket, win);
        Require(award.After.Level == 3 && award.After.CurrentXP == 250 && award.After.UnspentResearchPoints == 2, "Multi-level reward grants RP for every crossed threshold");
        var reloaded = new ProgressionStore(root);
        Require(MissionRewardLedger.Award(reloaded, ticket, win).AlreadySaved && reloaded.Load(id).LifetimePlayerXP == 1500, "Receipt survives reload and deduplicates");
        store.Save(store.Load(id) with { UnspentResearchPoints = 0,
            PurchasedResearchIds = ImmutableArray.Create("citadel_master_1", "battle_weapons_1") });
        Require(MissionRewardLedger.Award(store, ticket, win).After.UnspentResearchPoints == 2 && store.Load(id).UnspentResearchPoints == 0,
            "Duplicate receipt preserves its original RP summary without restoring spent research points");
        var next = MissionRewardLedger.Begin(store, store.Load(id), "Level001", policy);
        MissionRewardLedger.Award(store, next, new() { LevelName = "Level001", Outcome = LevelOutcome.Defeat });
        Require(store.Load(id).LifetimePlayerXP == 1525, "Defeat baseline independently editable");
        Expect(() => MissionRewardLedger.Award(store, ticket, win), "Retired older ticket cannot be re-awarded");
        Require(store.Load(id).LifetimePlayerXP == 1525, "Older request leaves progression unchanged");
        string saved = Path.Combine(root, id, "progression.json"); File.WriteAllText(saved, "{broken");
        Require(store.Load(id).LifetimePlayerXP == 1500 && store.RecoveryMessage.Length > 0, "Backup recovers consistent XP and award watermarks");
        Require(MissionRewardLedger.Award(store, next, new() { LevelName = "Level001", Outcome = LevelOutcome.Defeat }).After.LifetimePlayerXP == 1525,
            "Retry after backup recovery restores one coherent award, not duplicate XP");
        var signed = new MissionXPBreakdown(ImmutableArray.Create(new MissionXPLine("base", "Base", 500), new MissionXPLine("penalty", "Penalty", -600)));
        Require(signed.TotalXP == 0, "Future signed contributors floor mission reward at zero");
        Expect(() => MissionXPPolicy.Capture(new LevelConfig { VictoryBaseXP = -1 }), "Invalid settings rejected");
        string legacy = Guid.NewGuid().ToString("N"), path = Path.Combine(root, legacy, "progression.json"); Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "{\"Version\":2,\"ProfileId\":\"" + legacy + "\",\"LifetimePlayerXP\":600,\"UnspentResearchPoints\":0,\"FirstLevelXP\":500,\"LevelStepXP\":250,\"PurchasedResearchIds\":[\"citadel_master_1\"]}");
        PlayerProgression migrated = store.Load(legacy);
        Require(migrated.Version == 3 && migrated.LifetimePlayerXP == 600 && migrated.PurchasedResearchIds.Length == 1 &&
            migrated.LastIssuedMissionNumber == 0 && File.ReadAllText(path).Contains("\"Version\":2"), "v2 migration preserves XP/research without writing on read");
    }
    // =========================================================
    // Check the actual Results frame after XP content is visible at common viewport sizes.
    private async Task Geometry(LevelResults results)
    {
        Control screen = results.GetNode<Control>("Screen");
        var frame = (PanelContainer)typeof(LevelResults).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(results);
        ScrollContainer statScroll = (ScrollContainer)frame.FindChild("MissionStatsScroll", true, false);
        Require(statScroll != null, "Results exposes its scrollable stat area for responsive checks");
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        foreach (Vector2 size in new[] { new Vector2(1920,1080), new Vector2(1366,768), new Vector2(960,540) })
        {
            screen.Size = size; await Frames(); Rect2 rect = frame.GetGlobalRect(), bounds = screen.GetGlobalRect();
            Require(rect.Position.X >= bounds.Position.X - 1 && rect.Position.Y >= bounds.Position.Y - 1 &&
                rect.End.X <= bounds.End.X + 1 && rect.End.Y <= bounds.End.Y + 1, "Results XP frame fits " + size + ": " + rect);
            Require(rect.Size.X >= Mathf.Min(1020f, size.X - 48f) - 1f &&
                rect.Size.Y >= Mathf.Min(900f, size.Y - 48f) - 1f,
                "Results expands to viewport-aware debrief dimensions at " + size + ": " + rect);
            if (size.Y >= 900f)
                Require(!statScroll.GetVScrollBar().Visible, "Full-size desktop debrief shows all ten mission stats without scrolling");
        }
    }
    // =========================================================
    // Let deferred outcome arbitration, pause-aware reveal and engine cleanup settle normally.
    private async Task Frames()
    { for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    // =========================================================
    // A rejected transaction must be observable and must not silently produce a replacement balance.
    private static void Expect(Action action, string message)
    { try { action(); } catch { return; } throw new InvalidOperationException(message); }
}
