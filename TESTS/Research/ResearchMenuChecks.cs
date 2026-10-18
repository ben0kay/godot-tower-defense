// Real Research Room purchase and gameplay-effect flow with memory-only identities and temporary progression files.
using Godot;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AmmoLoopFixture;

public partial class ResearchMenuChecks : Node
{
    // =========================================================
    // Keep the runner alive across actual menu transitions without writing profile-index/user progression files.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string root = Path.Combine(Path.GetTempPath(), "td-room-" + Guid.NewGuid().ToString("N"));
        var profilesField = typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic);
        var storeField = typeof(ProgressionService).GetField("_store", BindingFlags.Static | BindingFlags.NonPublic);
        object previousProfiles = profilesField.GetValue(null), previousStore = storeField.GetValue(null);
        PlayerProfile previousSelected = ProfileStore.Selected;
        try
        {
            await Frame(); GetTree().CurrentScene = null;
            var a = new PlayerProfile { Id = Guid.NewGuid().ToString("N"), Name = "Research fixture A" };
            var b = new PlayerProfile { Id = Guid.NewGuid().ToString("N"), Name = "Research fixture B" };
            profilesField.SetValue(null, new List<PlayerProfile> { a, b });
            var store = new ProgressionStore(root); storeField.SetValue(null, store); ProfileStore.Select(a.Id);
            ProgressionService.GrantXP(600);
            MenuNavigation.Open(this, MenuNavigation.CampaignScene);
            await WaitScene<CampaignMenu>();
            var campaign = (CampaignMenu)GetTree().CurrentScene;
            ButtonFor(campaign, "RESEARCH").EmitSignal(Button.SignalName.Pressed);
            await WaitScene<ResearchMenu>(); var room = (ResearchMenu)GetTree().CurrentScene;
            ResearchGraph graph = room.FindChildren("*", "Control", true, false).OfType<ResearchGraph>().Single();
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 600), new Vector2I(800, 600) })
            {
                GetTree().Root.Size = size; GetTree().Root.ContentScaleSize = size;
                await Frame(); await Frame();
                Require(graph.Size.X >= 240 && graph.Size.Y >= 240, "Usable research canvas at " + size);
                Require(graph.GetGlobalRect().End.X <= GetViewport().GetVisibleRect().Size.X &&
                    graph.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y, "Graph stays within viewport at " + size);
                var back = ButtonFor(room, "BACK TO CAMPAIGN");
                Require(back.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y, "Back remains visible at " + size);
            }
            graph.Select("citadel_master_1"); await Frame();
            Button buy = room.FindChild("PurchaseResearch", true, false) as Button;
            Require(buy != null && !buy.Disabled && graph.HitNode(graph.GraphToScreen(ResearchGraph.AxialPosition(new(1, 0)))) == "citadel_master_1", "Live room available node and transformed selection");
            string temporary = Path.Combine(root, a.Id, "progression.json.tmp"); Directory.CreateDirectory(temporary);
            buy.EmitSignal(Button.SignalName.Pressed);
            Require(ProgressionService.GetCurrent().UnspentResearchPoints == 1 && ProgressionService.GetCurrent().PurchasedResearchIds.Length == 0,
                "Failed save never spends RP or marks purchase");
            Directory.Delete(temporary); buy.EmitSignal(Button.SignalName.Pressed);
            Require(ProgressionService.GetCurrent().UnspentResearchPoints == 0 && buy.Disabled && buy.Text == "RESEARCHED", "Real button commits and visibly marks ownership");
            buy.EmitSignal(Button.SignalName.Pressed);
            Require(ProgressionService.GetCurrent().PurchasedResearchIds.Length == 1 && ProgressionService.GetCurrent().UnspentResearchPoints == 0, "Repeated UI purchase cannot charge twice");
            // Read through a fresh store instance to emulate application restart without cached state.
            storeField.SetValue(null, new ProgressionStore(root));
            foreach (string scene in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene })
            {
                using AmmoLoopFixture f = new(this, scene);
                int authored = f.Main.GetNode<WorldGrid>("World/Grid").Level.Settings.CitadelMaxHealth;
                var health = f.Main.GetNode<CitadelSession>("CitadelSession").Health;
                Require(health.MaxHealth == (int)Math.Round(authored * 1.1, MidpointRounding.AwayFromZero) && health.CurrentHealth == health.MaxHealth,
                    "Purchased +10% applies to max/current on " + scene);
                Require(f.Main.GetNode<WorldGrid>("World/Grid").Level.Settings.CitadelMaxHealth == authored, "Authored resource unchanged");
                Require(!f.Main.GetNode<LevelResults>("LevelResults").FindChildren("*", "Button", true, false).OfType<Button>().Any(button => button.Text.Contains("RESEARCH")), "No Results research link");
                Expect(() => ResearchService.Purchase(f.Main, "citadel_master_1"), "No battlefield purchase API");
            }
            ButtonFor(room, "BACK TO CAMPAIGN").EmitSignal(Button.SignalName.Pressed);
            await WaitScene<CampaignMenu>();
            ProfileStore.Select(b.Id); Require(ProgressionService.GetCurrent().PurchasedResearchIds.Length == 0, "Profile B has no research");
            using (AmmoLoopFixture f = new(this))
            {
                int authored = f.Main.GetNode<WorldGrid>("World/Grid").Level.Settings.CitadelMaxHealth;
                Require(f.Main.GetNode<CitadelSession>("CitadelSession").Health.MaxHealth == authored, "Profile B has authored HP");
            }
            ProfileStore.Select(a.Id); MenuNavigation.Open(this, MenuNavigation.ResearchScene);
            await WaitScene<ResearchMenu>(); room = (ResearchMenu)GetTree().CurrentScene;
            graph = room.FindChildren("*", "Control", true, false).OfType<ResearchGraph>().Single();
            graph.Select("citadel_master_1");
            Require(((Button)room.FindChild("PurchaseResearch", true, false)).Text == "RESEARCHED", "Ownership visible after leaving/reopening");
            GD.Print("ResearchMenuChecks: PASS Campaign entry, actual purchase, failed save, repeat/reopen, profile isolation and both-level +10% HP");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally
        {
            // Retire room observers before restoring the caller's isolated profile/service state.
            if (GetTree().CurrentScene is Node current && current != this) current.Free();
            GetTree().CurrentScene = null; ProfileStore.ClearSelection();
            profilesField.SetValue(null, previousProfiles); storeField.SetValue(null, previousStore);
            if (previousSelected != null) ProfileStore.Select(previousSelected.Id);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    // =========================================================
    // Deferred scene replacement settles before tests inspect the active scene.
    private async Task WaitScene<T>() where T : Node
    {
        for (int i = 0; i < 20; i++) { await Frame(); if (GetTree().CurrentScene is T) return; }
        throw new Exception("Scene transition failed: " + typeof(T).Name);
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private static Button ButtonFor(Node node, string text) => node.FindChildren("*", "Button", true, false).OfType<Button>().First(button => button.Text == text);
    private static void Expect(Action action, string name)
    { try { action(); } catch { return; } throw new Exception("Expected rejection: " + name); }
}
