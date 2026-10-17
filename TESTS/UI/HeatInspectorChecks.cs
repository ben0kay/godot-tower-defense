// HD3 real selection/event lifecycle and compact heat/disable UI without polling or stat resolution on heat ticks.
using Godot;
using System;
using static AmmoLoopFixture;

public partial class HeatInspectorChecks : Node
{
    private const string Card = "BottomArea/Inspector/Insets/Body/Columns/Side/Conditions";
    // =========================================================
    // Real HUD, catalog tower instances, paid work clocks and deferred condition notifications.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                BattleTower battle = f.Add<BattleTower>("battle", new(2000,2128), c => c.SetLevelStats(1, c.Levels[0].Stats with { Heat = new(10, 10, 0, 1, .35f) }));
                battle.SetTargetPriority(TowerTargeting.Priority.Closest);
                f.Placement.Call("SelectTower", battle); await Frames();
                ProgressBar bar = f.Hud.GetNode<ProgressBar>(Card + "/HeatBar");
                Label heat = f.Hud.GetNode<Label>(Card + "/HeatText"), reasons = f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/DisableReasons");
                Require(bar.IsVisibleInTree() && bar.Value == 0 && bar.MaxValue == 10 && heat.Text.Contains("0 / 10"), "Cold selected heat tower shows actual capacity");
                Require(f.Hud.GetNode<Control>(Card).TooltipText.Contains("NEXT LEVEL"), "Cached current/next thermal details available without extra rows");
                var grid = f.Hud.GetNode<GridContainer>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeComparison");
                ulong firstCell = grid.GetChild(0).GetInstanceId(); TowerFinalStats cache = battle.FinalStats;
                Enemy enemy = f.Target(new(2000,2000)); battle._PhysicsProcess(1); Freeze(battle); await Frames();
                Require(bar.Value == 10 && heat.Text.Contains("OVERHEATED / COOLING") && reasons.Text.Contains("OVERHEATED"), "Overheat updates selected bar/state automatically");
                Require(f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Heading/WorkSection/Status").Text == "DISABLED", "Header does not call a disabled tower operational");
                TowerDisableHandle emp1 = battle.ApplyDisable(TowerDisableReason.EMP), emp2 = battle.ApplyDisable(TowerDisableReason.EMP);
                var power = battle.ApplyDisable(TowerDisableReason.NoPower); var other = battle.ApplyDisable(TowerDisableReason.Other); Freeze(battle);
                await Frames(); Require(reasons.Text.Contains("EMP ×2") && reasons.Text.Contains("NO POWER") && reasons.Text.Contains("OTHER"), "Overlapping reasons grouped with independent source counts");
                battle.AdvanceConditions(7); Freeze(battle); await Frames();
                Require(Math.Abs(bar.Value - 3) < .001 && heat.Text.Contains("COOLING") && !reasons.Text.Contains("OVERHEATED") && reasons.Text.Contains("EMP ×2"), "Cooling removes only overheat UI cause: " + bar.Value + " | " + heat.Text + " | " + reasons.Text);
                Require(ReferenceEquals(cache, battle.FinalStats) && grid.GetChild(0).GetInstanceId() == firstCell, "Heat-only updates preserve stat cache and comparison cells");
                f.Hud.ClearInspection(); string hiddenText = heat.Text; battle.AdvanceConditions(1); Freeze(battle); await Frames();
                Require(heat.Text == hiddenText && !f.Hud.IsInspectionVisible, "Closed Inspector detaches heat notifications");
                f.Placement.Call("SelectTower", battle); await Frames(); Require(Math.Abs(bar.Value - 2) < .001, "Reselect refreshes actual heat after hidden interval");
                var laser = f.Add<LaserTower>("laser", new(2400,2128)); f.Placement.Call("SelectTower", laser);
                laser.ApplyDisable(TowerDisableReason.EMP); await Frames();
                Require(!bar.Visible && !heat.Visible && reasons.IsVisibleInTree() && reasons.Text == "DISABLED: EMP", "Heatless towers show generic disable reasons without fake heat");
                battle.AdvanceConditions(1); Freeze(battle); await Frames(); Require(!bar.Visible && reasons.Text == "DISABLED: EMP", "Old tower cannot overwrite new selection");
                f.Placement.Call("SelectTower", battle); await Frames();
                battle.RemoveDisable(emp1); Freeze(battle); await Frames(); Require(reasons.Text.Contains("EMP") && !reasons.Text.Contains("×2"), "Individual source removal updates remaining count");
                battle.RemoveDisable(emp2); battle.RemoveDisable(power); battle.RemoveDisable(other); Freeze(battle); await Frames(); Require(!reasons.Visible && bar.Visible, "Final reason clears while heat remains visible");
                GetTree().Paused = true; double paused = bar.Value; battle.AdvanceConditions(20); await Frames();
                Require(bar.Value == paused, "Paused gameplay freezes displayed heat"); GetTree().Paused = false;
                f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
                Require(TowerUpgradeController.TryUpgrade(battle, f.Wallet, f.Placement.Settings, 1, out string error), error);
                ConstructionTestClock.FinishWork(f.Main); Freeze(battle); await Frames();
                Require(battle.UpgradeLevel == 2 && bar.MaxValue == 100 && bar.Value == paused, "Paid tier changes capacity without losing current heat");
                f.Placement.Call("SelectTower", laser); battle.Free(); enemy.Free(); f.ClearProjectiles(); laser.Free(); await Frames();
                await StressLayout(f);
                await InspectorTargetingChecks.Layout(f);
                TowerBase pending = f.Add<TowerBase>("battle", new(2000,2128));
                f.Placement.Call("SelectTower", pending); pending.ApplyDisable(TowerDisableReason.EMP);
                // Disposal below deliberately leaves a queued condition UI callback across scene unload.
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: HD3 live heat/bar/overheat, generic overlapping disable reasons, selection/close/pause/free, cached previews/paid tiers and compact all-tower viewport layouts");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    // =========================================================
    // Worst current card: Legendary rank, all disable reasons, hot bar and busy purchased upgrade.
    private async System.Threading.Tasks.Task StressLayout(AmmoLoopFixture f)
    {
        BattleTower tower = f.Add<BattleTower>("battle", new(2000,2128), c => c.SetLevelStats(1, c.Levels[0].Stats with { Heat = new(10, 10, 0, 1, .35f) }));
        tower.SetTargetPriority(TowerTargeting.Priority.Closest); Enemy enemy = f.Target(new(2000,2000)); tower._PhysicsProcess(1);
        tower.ApplyDisable(TowerDisableReason.EMP); tower.ApplyDisable(TowerDisableReason.EMP);
        tower.ApplyDisable(TowerDisableReason.NoPower); tower.ApplyDisable(TowerDisableReason.Other);
        TowerRankController.AwardExperience(tower, 15000); Freeze(tower);
        f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out string error), error);
        f.Placement.Call("SelectTower", tower);
        f.Hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        foreach (Vector2 viewport in new[] { new Vector2(1920,1080), new Vector2(1366,768), new Vector2(960,540) })
        {
            f.Hud.Size = viewport; await Frames(); Control panel = f.Hud.GetNode<Control>("BottomArea/Inspector");
            if (panel.Size.Y > 331.5) Dump(panel);
            Require(panel.Size.X <= 560.5 && panel.Size.Y <= 331.5, "Heat plus busy/MAX RANK keeps compact footprint: " + panel.Size);
            Contained(panel, panel.GetGlobalRect());
            GD.Print($"HD3 STRESS GEOMETRY {viewport}: {panel.Size}");
        }
        f.Hud.ClearInspection(); tower.Free(); enemy.Free(); f.ClearProjectiles();
    }
    // =========================================================
    // Failed layout diagnostics expose the actual control that requested extra height.
    private static void Dump(Node node)
    {
        foreach (Node child in node.GetChildren())
        { if (child is Control c && c.IsVisibleInTree()) GD.Print($"HD3 LAYOUT {c.Name}: {c.Size} " + (c is Label label ? label.Text : "")); Dump(child); }
    }
    // =========================================================
    // Verify all actual visible child controls, not only the panel's preferred minimum size.
    private static void Contained(Node node, Rect2 bounds)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Control control && control.IsVisibleInTree())
            {
                Rect2 r = control.GetGlobalRect();
                Require(r.Position.X >= bounds.Position.X - 1 && r.Position.Y >= bounds.Position.Y - 1
                    && r.End.X <= bounds.End.X + 1 && r.End.Y <= bounds.End.Y + 1, "Heat Inspector child remains inside panel " + control.Name);
            }
            Contained(child, bounds);
        }
    }
    // =========================================================
    // Freeze only the test fixture's automatic clock; production uses normal pause-aware physics.
    private static void Freeze(TowerBase tower) => tower.GetNodeOrNull<TowerConditions>("CombatConditions")?.SetPhysicsProcess(false);
    // =========================================================
    // Deferred events and real Godot container layout settle without wall-clock sleeps.
    private async System.Threading.Tasks.Task Frames()
    { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
}
