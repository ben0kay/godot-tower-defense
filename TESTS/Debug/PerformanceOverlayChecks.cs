// Real battlefield overlay checks: layout coexistence, read-only counters, scoped F2 and pause behavior.
using Godot;
using System;
using System.Linq;
using static MissionFixture;

public partial class PerformanceOverlayChecks : Node
{
    #region Runner
    // =========================================================
    // Sample actual scene-owned services on all launchers and verify teardown through the existing fixture.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            foreach (string scene in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level002Scene, MenuNavigation.Level020Scene })
            {
                using MissionFixture f = new(this, scene);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                PerformanceOverlay overlay = f.Main.GetNode<PerformanceOverlay>("PerformanceOverlay");
                Require(overlay.Visible && overlay.IsProcessing(), "Debug overlay starts visible");
                LevelDebugController debug = f.Main.GetNode<LevelDebugController>("World/Level/Debug");
                Require(debug.RoutePreview != null, "Route Preview initializes alongside Enemy Spawner");
                DeveloperHub hub = f.Main.GetNode<DeveloperHub>("DeveloperHub");
                hub._Input(new InputEventKey { Keycode = Key.F1, Pressed = true });
                hub._UnhandledKeyInput(new InputEventKey { Keycode = Key.Key1, Pressed = true });
                Require(debug.SpawnMenu.IsOpen, "F1 then 1 opens Enemy Spawner");
                hub._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
                Require(!debug.SpawnMenu.IsOpen && !GetTree().Paused, "Escape dismisses the tool without pausing");
                debug.SpawnMenu.StartBatch(0, 1, 0); debug.SpawnMenu._PhysicsProcess(1.0 / 60);
                Require(f.Spawns.Registry.Living.Count == 1, "Enemy Spawner batch creates a ship on this real battlefield");
                Enemy enemy = f.Spawns.Registry.Living.Single();
                AmmoLoopFixture.Disable(enemy);
                long queries = f.Spawns.Paths.TotalCalculations;
                Node projectiles = f.Main.GetNode("World/Projectiles"); Node dummy = new(); projectiles.AddChild(dummy);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree().CreateTimer(.35), SceneTreeTimer.SignalName.Timeout);
                Label[] rows = overlay.FindChildren("*", "Label", true, false).OfType<Label>().ToArray();
                Require(rows.Length == 25 && rows.Any(row => row.Text.Contains("Enemies 1") && row.Text.Contains("projectile nodes 1")),
                    "Throttled display reads cached actual service/container counts: " + string.Join(" | ", rows.Select(row => row.Text)));
                Require(rows[21].Text.Contains("Power islands") && rows[22].Text.Contains("Power hover"), "Power topology has a cached readout");
                Require(rows[23].Text.Contains("Power Σ supply") && rows[24].Text.Contains("accounting only"), "Power accounting is readable without gating gameplay");
                Require(f.Spawns.Paths.TotalCalculations == queries, "Reading diagnostics never performs new route searches");
                Require(overlay.FindChildren("*", "Control", true, false).OfType<Control>().All(c => c.MouseFilter == Control.MouseFilterEnum.Ignore),
                    "No overlay Control can intercept build/selection mouse input");
                PanelContainer panel = overlay.GetNode<PanelContainer>("Panel");
                Control top = f.Main.GetNode<Control>("UI/GameplayHUD/TopBar");
                Require(panel.Position.Y >= top.GetGlobalRect().End.Y, "Panel clears the HUD bar");
                Control legacy = f.Main.GetNodeOrNull<Control>("World/Level/Debug/HUD/Info");
                Require(legacy == null, "Retired map debug readout is removed");
                Require(!overlay.Profiler.Running, "Subsystem profiling defaults off");
                hub._Input(new InputEventKey { Keycode = Key.F2, Pressed = true, ShiftPressed = true });
                Require(overlay.Visible && overlay.Profiler.Running, "Shift+F2 enables profiling without hiding the panel");
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Require(overlay.Profiler.Samples > 0, "Early/late physics callbacks close real samples");
                foreach (Vector2 size in new[] { new Vector2(1366, 768), new Vector2(960, 540) })
                {
                    GetViewport().GetWindow().ContentScaleSize = (Vector2I)size;
                    await ToSignal(GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
                    Require(panel.Position.X >= 0 && panel.Position.Y >= top.GetGlobalRect().End.Y, "Resized overlay stays below HUD");
                    Control bottom = f.Main.GetNode<Control>("UI/GameplayHUD/BottomArea");
                    if (panel.Visible && bottom.IsVisibleInTree())
                        Require(panel.Position.Y + panel.Size.Y * panel.Scale.Y <= bottom.GetGlobalRect().Position.Y,
                            "Scaled panel avoids the lower HUD on small viewports");
                }
                Require(rows[19].Visible && rows[20].Text.Contains("overlap"), "Expanded profiling rows remain readable and explain overlap");
                if (scene == MenuNavigation.Level001Scene) hub._Input(new InputEventKey { Keycode = Key.F3, Pressed = true });
                int bindings = GetNode<GameInput>("/root/GameInput").BindingCount;
                hub._Input(new InputEventKey { Keycode = Key.F2, Pressed = true, Echo = true });
                Require(overlay.Visible, "OS repeat does not toggle");
                GetTree().Paused = true;
                hub._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
                Require(!overlay.Visible && !overlay.IsProcessing() && !overlay.IsPhysicsProcessing() && !overlay.Profiler.Running, "F2 hides/stops sampling while paused");
                hub._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
                Require(overlay.Visible && GetTree().Paused && overlay.Profiler.Running && overlay.Profiler.Samples == 0, "F2 reopens fresh capture without resuming gameplay");
                await ToSignal(GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
                Require(rows[0].Text.Contains("PAUSED") && f.Spawns.Registry.Living.Count == 1
                    && GetNode<GameInput>("/root/GameInput").BindingCount == bindings, "Paused sampling remains read-only without new input bindings");
                GetTree().Paused = false; dummy.Free();
                GetViewport().GetWindow().ContentScaleSize = new(1920, 1080);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: performance overlay on all three battlefields, cached counts, mouse pass-through, HUD/debug coexistence, resize, F2/echo, pause and hidden sampling");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    #endregion
}
