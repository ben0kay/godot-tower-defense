// MC3 production HUD and F3 preview checks on the isolated multi-objective fixture.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;
public partial class MultiCitadelHudChecks : Node
{
    #region Presentation Checks
    // =========================================================
    // Verify actual controls/events and actual preview paths; no fabricated route selection or HP models.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture single = new(this))
            {
                SubsystemBattlefield.DetachSession(single.Main);
                Require(single.Hud.GetNodeOrNull("TopBar/Status/CitadelRow") == null && single.Hud.GetNode<Control>("TopBar/Status/Citadel").Visible,
                    "Single-Citadel layout retains its original nodes");
            }
            foreach (int count in new[] { 2, 4 })
            {
                using MultiCitadelChecks.Fixture f = new(this, citadelCount: count);
                var hud = f.Main.GetNode<GameplayHUD>("UI/GameplayHUD");
                var row = hud.GetNode<CitadelHudRow>("TopBar/Status/CitadelRow");
                Require(row.GetChildCount() == count && !hud.GetNode<Control>("TopBar/Status/Citadel").Visible, "One row replaces primary-only display");
                foreach (Vector2 size in new[] { new Vector2(1920,1080), new Vector2(1366,768), new Vector2(960,540) })
                {
                    GetTree().Root.Size=(Vector2I)size; GetTree().Root.ContentScaleSize=(Vector2I)size; hud.Size=size;
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    var status=hud.GetNode<Control>("TopBar/Status");
                    Require(Math.Abs(row.Position.X+row.Size.X/2-status.Size.X/2)<1,"Row is exactly centered");
                    float width=row.GetChild<Control>(0).Size.X, previousRight=-1;
                    for(int i=0;i<count;i++)
                    {
                        var cell=row.GetChild<Control>(i); var bar=cell.GetNode<ProgressBar>("Bar");
                        Require(Math.Abs(cell.Size.X-width)<.01 && cell.Position.Y==0 && cell.Position.X>=previousRight,"Equal side-by-side cells, no stacking");
                        Require(cell.Position.X+cell.Size.X<=row.Size.X+.1 && bar.Size.X<=cell.Size.X+.1,"Bars stay inside center portion");
                        Require(cell.GetNode<Label>("Name").Text==$"CITADEL {i+1}","Stable visible objective numbers");
                        previousRight=cell.Position.X+cell.Size.X;
                    }
                    Require(row.GetGlobalRect().Position.X>=status.GetNode<Control>("Credits").GetGlobalRect().End.X
                        && row.GetGlobalRect().End.X<=status.GetNode<Control>("Wave").GetGlobalRect().Position.X,"Center row avoids Credits and Wave");
                }
                string first=row.GetChild<Control>(0).GetNode<Label>("Health").Text;
                f.Session.Objectives[1].Health.TakeDamage(100);
                Require(row.GetChild<Control>(1).GetNode<Label>("Health").Text=="900 / 1,000" && row.GetChild<Control>(0).GetNode<Label>("Health").Text==first,
                    "Only damaged objective's authoritative number changes");
                f.Session.Objectives[1].Health.TakeDamage(900);
                var changedBar=row.GetChild<Control>(1).GetNode<ProgressBar>("Bar");
                Require(((StyleBoxFlat)changedBar.GetThemeStylebox("fill")).BgColor==hud.Config.HealthCriticalColor
                    && ((StyleBoxFlat)row.GetChild<Control>(0).GetNode<ProgressBar>("Bar").GetThemeStylebox("fill")).BgColor==hud.Config.HealthHealthyColor,"Threshold colors are independent");
                await ToSignal(GetTree().CreateTimer(hud.Config.CitadelBarTweenSeconds+.08),SceneTreeTimer.SignalName.Timeout);
                Require(Math.Abs(changedBar.Value)<.01 && row.GetChild<Control>(1).GetNode<Label>("Health").Text=="0 / 1,000","Damaged meter animation reaches authoritative zero");
                f.Session.Objectives[count-1].Health.Initialize(2000);
                Require(row.GetChild<Control>(count-1).GetNode<Label>("Health").Text=="2,000 / 2,000","Each objective retains independent maximum");
                Preview(f);
                hud.BindCitadels(f.Session.Objectives);
                Require(hud.GetNode<CitadelHudRow>("TopBar/Status/CitadelRow").GetChildCount()==count,"Rebinding replaces old subscriptions and cells");
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("MultiCitadelHudChecks: PASS single/two/four horizontal meters at three sizes, independent HP, rebinding, assigned F3 routes, goal invalidation and live-state isolation"); GetTree().Quit();
        }
        catch(Exception error) { GetTree().Paused=false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // ALL PORTALS must use cached live targets, not the preview pool's legacy primary Goals list.
    private static void Preview(MultiCitadelChecks.Fixture f)
    {
        var debug=f.Grid.Level.GetNode<LevelDebugController>("Debug"); var overlay=debug.RoutePreview;
        debug._UnhandledKeyInput(new InputEventKey { Keycode=Key.F3, Pressed=true });
        Node panel=overlay.GetNode("PreviewUI/Panel"); var portals=Find<OptionButton>(panel,"Portal");
        Find<SpinBox>(panel,"Samples").Value=2; portals.Select(portals.ItemCount-1);
        int living=f.Spawns.Registry.Living.Count, pending=f.Paths.PendingCount, pools=f.Paths.Navigation.TotalSharedProfileCount;
        long queries=f.Paths.TotalCalculations, selections=f.Spawns.CitadelAssignments.SelectionQueries;
        overlay.Simulate();
        for(int i=0;i<5000 && overlay.PortalJobs.Any(j=>!j.Complete);i++) overlay._PhysicsProcess(.016);
        Require(overlay.PortalJobs.Count==3 && overlay.PortalJobs.All(j=>j.Complete && j.Results.Count==2),"All three registered portals have completed previews");
        foreach(var job in overlay.PortalJobs)
        {
            Require(job.Destination!=null && f.Session.Objectives.Contains(job.Destination),"Job retains actual cached destination identity");
            foreach(var route in job.Results)
                Require(job.Destination.Goals.Contains(f.Grid.WorldToCell(route.Points[^1])),"Preview ends at the assigned Citadel");
        }
        string legend=Find<RichTextLabel>(panel,"Legend").Text;
        Require(legend.Contains("CITADEL 1") && legend.Contains("CITADEL 2"),"Legend names portal-to-Citadel assignments");
        Require(f.Spawns.Registry.Living.Count==living && f.Paths.PendingCount==pending && f.Paths.TotalCalculations==queries
            && f.Spawns.CitadelAssignments.SelectionQueries==selections && f.Paths.Navigation.TotalSharedProfileCount==pools,"F3 neither spawns nor alters live routing/assignments/pools");
        var target=f.Session.Objectives[1]; target.Goals.Add(new Vector2I(26,16)); overlay._PhysicsProcess(.016);
        Require(overlay.Job==null,"Secondary objective goal edits invalidate cached previews too"); target.Goals.RemoveAt(target.Goals.Count-1);
        debug._UnhandledKeyInput(new InputEventKey { Keycode=Key.F3, Pressed=true }); Require(!overlay.IsOpen,"F3 closes preview");
    }
    // =========================================================
    // Locate production panel controls without coupling the check to container nesting.
    private static T Find<T>(Node root,string name) where T:Node
    {
        if(root is T match && root.Name==name)return match;
        foreach(Node child in root.GetChildren()) { var found=Find<T>(child,name); if(found!=null)return found; }
        return null;
    }
    #endregion
}
