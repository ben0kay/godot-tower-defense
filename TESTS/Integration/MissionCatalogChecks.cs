// Verify resource-driven registration, real shared deployments and retry without a per-level launcher.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MissionFixture;

public partial class MissionCatalogChecks : Node
{
    #region Runner
    // =========================================================
    // Stay outside CurrentScene so actual navigation can replace the battlefield underneath this runner.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MissionCatalog catalog = MissionCatalog.Default;
        MissionDefinition temporary = null;
        try
        {
            GetTree().CurrentScene = null;
            Require(catalog.Entries().Select(m => m.Number).SequenceEqual(new[] { 1, 2, 20, 100 }), "Existing campaign order retained");
            foreach (var mission in catalog.Entries())
            {
                BattlefieldLauncher root = GD.Load<PackedScene>(mission.LaunchPath).Instantiate<BattlefieldLauncher>();
                AddChild(root);
                Require(root.GetNode<WorldGrid>("World/Grid").Level.SceneFilePath == mission.MapScene.ResourcePath,
                    "Shared world installs correct authored map before Grid Ready");
                Require(root.GetNode<WaveManager>("Waves").TotalWaves == WaveCatalog.GetWaves(root.GetNode<WorldGrid>("World/Grid").Level.Settings).Length,
                    "Assigned C# recipe initializes existing scheduler");
                RemoveChild(root); root.Free();
            }
            using (var multi = new MultiCitadelChecks.Fixture(this))
            {
                Require(multi.Session.Objectives.Count == 2, "Off-tree authoring fixture preserves multiple Citadels");
                var left = multi.Spawn("MCA"); var right = multi.Spawn("MCB");
                Require(left.AssignedCitadel.Root == multi.First && right.AssignedCitadel.Root == multi.Second,
                    "Shared launcher preserves independently assigned Citadel routes");
            }
            temporary = new MissionDefinition { Number = 3, Title = "Data-only fixture", MapScenePath = catalog.Entries()[1].MapScenePath };
            Require(ResourceSaver.Save(temporary, "user://MissionCatalogFixture.tres") == Error.Ok, "Write isolated fixture resource");
            temporary = GD.Load<MissionDefinition>("user://MissionCatalogFixture.tres");
            catalog.Missions.Add(temporary);
            Require(PlayableLevelCatalog.NextScene("Level002") == temporary.ResourcePath && PlayableLevelCatalog.IsPlayable(temporary.ResourcePath),
                "New data entry enables immediate next mission with no launcher or code registration");
            temporary.Enabled = false; Require(PlayableLevelCatalog.NextScene("Level002") == null, "Disabled mission is omitted without skipping to020");
            temporary.Enabled = true;
            catalog.Missions.Add(temporary);
            bool duplicate = false; try { catalog.Entries(); } catch (InvalidOperationException) { duplicate = true; }
            catalog.Missions.RemoveAt(catalog.Missions.Count - 1); Require(duplicate, "Duplicate identities reject explicitly");
            MenuNavigation.Open(this, temporary.ResourcePath);
            BattlefieldLauncher first = await Deployment(null);
            Require(first.Mission.Number == 3 && first.GetNode<WorldGrid>("World/Grid").Level.Settings.WaveSetId == "level002", "Resource-only deployment keeps own identity and selected map");
            first.GetNode<CitadelSession>("CitadelSession").Health.TakeDamage(int.MaxValue);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            first.GetNode<LevelResults>("LevelResults").SkipReveal();
            first.GetNode<LevelSessionController>("LevelSession").Retry();
            BattlefieldLauncher retry = await Deployment(first);
            Require(retry.Mission.Number == 3 && !GetTree().Paused && retry.GetNode<LevelSessionController>("LevelSession").State == LevelSessionState.Playing,
                "Resource-only retry retains mission and resets session");
            GetTree().Root.RemoveChild(retry); retry.Free(); GetTree().CurrentScene = null;
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: shared level scenes, data-only catalog, C# recipe references, disabled/duplicate entries and resource-only deployment/retry");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally
        {
            GetTree().Paused = false;
            if (temporary != null) catalog.Missions.Remove(temporary);
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://MissionCatalogFixture.tres"));
        }
    }
    // =========================================================
    // Navigation is deferred by Godot; never mistake the departing scene for the next deployment.
    private async Task<BattlefieldLauncher> Deployment(BattlefieldLauncher previous)
    {
        for (int i = 0; i < 90; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (GetTree().CurrentScene is BattlefieldLauncher root && root != previous && root.IsNodeReady()) return root;
        }
        throw new InvalidOperationException("Deployment did not become ready.");
    }
    #endregion
}
