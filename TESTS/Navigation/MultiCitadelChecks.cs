// Runtime-only two-Citadel authored fixture; existing gameplay maps and waves are never edited.
using Godot;
using System;
using System.Linq;
using System.Diagnostics;
using static AmmoLoopFixture;
public partial class MultiCitadelChecks : Node
{
    #region Fixture
    internal sealed class Fixture : IDisposable
    {
        internal readonly Node Main;
        internal readonly WorldGrid Grid;
        internal readonly SpawnManager Spawns;
        internal readonly CitadelSession Session;
        internal readonly Node2D First, Second;
        internal readonly PathRequestManager Paths;
        internal Fixture(Node owner, bool checkpoint = false, bool detour = false, bool keepResults = false, int citadelCount = 2)
        {
            Main = GD.Load<PackedScene>("res://Main.tscn").Instantiate();
            if (!keepResults) SubsystemBattlefield.DetachSession(Main);
            ((BattlefieldLauncher)Main).PrepareMap();
            WorldLevel level = Main.GetNode<WorldLevel>("World/Level");
            level.Settings = (LevelConfig)level.Settings.Duplicate(true);
            level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals;
            level.Settings.ActivePortals = new() { new NodePath("MCA"), new NodePath("MCB"), new NodePath("MCTie") };
            level.Passable.Clear(); level.Impassable.Clear(); level.Interactive.Clear();
            for (int y = 0; y < 32; y++) for (int x = 0; x < 40; x++) level.Impassable.SetCell(new(x,y), 0, Vector2I.Zero);
            void Open(int x, int y) { level.Impassable.EraseCell(new(x,y)); level.Passable.SetCell(new(x,y), 0, new(2,0)); }
            for (int x = 5; x <= 27; x++) { Open(x,8); Open(x,16); }
            for (int y = 8; y <= 16; y++) Open(14,y);
            if (detour) { level.Passable.EraseCell(new(21,8)); level.Impassable.SetCell(new(21,8),0,Vector2I.Zero); for (int y=8;y<=16;y++) Open(27,y); }
            First = level.GetNode<Node2D>(level.Settings.CitadelPath); First.Position = At(28,7);
            Second = GD.Load<PackedScene>("res://WORLD/OBJECTIVES/Citadel/Citadel.tscn").Instantiate<Node2D>();
            Second.Name = "MCSecond"; level.GetNode("Objectives").AddChild(Second); Second.Position = At(28,15);
            for (int i = 2; i < citadelCount; i++)
            {
                Node2D extra = GD.Load<PackedScene>("res://WORLD/OBJECTIVES/Citadel/Citadel.tscn").Instantiate<Node2D>();
                extra.Name = "MCExtra" + (i + 1); level.GetNode("Objectives").AddChild(extra); extra.Position = At(32 + (i - 2) * 4, 7);
                foreach (Node2D point in extra.GetNode("ApproachPoints").GetChildren()) point.Position = At(27, i % 2 == 0 ? 8 : 16) - extra.Position;
            }
            foreach (var pair in new[] { (First, At(27,8)), (Second, At(27,16)) })
                foreach (Node2D point in pair.Item1.GetNode("ApproachPoints").GetChildren()) point.Position = pair.Item2 - pair.Item1.Position;
            // Explicit authored order also proves the legacy path still works when the array is empty elsewhere.
            level.Settings.CitadelPaths = new() { level.GetPathTo(First), level.GetPathTo(Second) };
            EnemyPortal Portal(string name, int x, int y)
            { var portal = new EnemyPortal { Name=name, Position=At(x,y) }; portal.AddChild(new Marker2D { Name="SpawnExit" }); level.AddChild(portal); return portal; }
            EnemyPortal a = Portal("MCA",6,8); Portal("MCB",6,16); Portal("MCTie",14,12);
            if (checkpoint)
            { var point = new RouteCheckpoint { Name="MCCheckpoint", Position=At(14,16), RadiusPixels=10 }; level.AddChild(point); a.CheckpointPath = a.GetPathTo(point); }
            owner.AddChild(Main); Disable(Main);
            Grid = Main.GetNode<WorldGrid>("World/Grid"); Spawns = Main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            Paths=Spawns.Paths; Session=Main.GetNode<CitadelSession>("CitadelSession");
            Paths.Config=(EnemyMasterConfig)Paths.Config.Duplicate(true); Paths.Navigation.Config=Paths.Config;
            Paths.Config.EnablePathDiversity=Paths.Config.EnablePathOffsets=Paths.Config.EnableCorridorVariation=false;
            Paths.Config.CongestionInfluence=0; Paths.Config.PortalSpawnOffsetPixels=0; Paths.Config.MaxPathCalculationsPerFrame=2;
        }
        internal Enemy Spawn(string portal)
        { var enemy=Spawns.SpawnWave("basic_mk1",new RandomNumberGenerator { Seed=4 },portal); Require(enemy!=null,"Spawn from "+portal+": "+Spawns.LastMessage); Disable(enemy); return enemy; }
        public void Dispose() { Main.GetTree().Paused=false; Main.Free(); }
    }
    // =========================================================
    // Authoring coordinates match the production 64px grid without requiring an initialized WorldGrid.
    private static Vector2 At(int x,int y) => new(32+x*64,32+y*64);
    // =========================================================
    // Drain the real bounded queue; no test-only unbounded route calculations.
    private static void Drain(PathRequestManager paths)
    {
        for(int i=0;i<5000 && (paths.PendingCount>0 || paths.Journeys.PendingEdges>0);i++)
        { paths._PhysicsProcess(1.0/60); Require(paths.LastFrameCalculations<=2,"Combined query budget retained"); }
        Require(paths.PendingCount==0 && paths.Journeys.PendingEdges==0,"Routing queue drained");
    }
    #endregion

    #region Production Checks
    // =========================================================
    // Exercise destination identity, setup comparisons, placement, arrival and actual mission defeat independently.
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        try
        {
            using(var single=new AmmoLoopFixture(this))
            {
                SubsystemBattlefield.DetachSession(single.Main);
                Require(single.Spawns.Paths.Navigation.Destinations.Count==1 && single.Spawns.CitadelAssignments.SelectionQueries==0,"One Citadel skips destination comparisons");
                Enemy ship=single.Spawns.Spawn("basic_mk1"); Require(ship?.AssignedCitadel==single.Spawns.Paths.Navigation.SingleDestination,"Legacy spawn gets sole objective"); ship.Free();
                var nav=single.Spawns.Paths.Navigation; bool checkedFastPath=false;
                foreach (Vector2I cell in nav.Grid.Level.Passable.GetUsedCells())
                {
                    if (nav.Grid.BlocksEnemies(cell) || nav.Grid.PlacementFailure(cell).Length!=0) continue;
                    int floods=nav.PlacementFloodCount; single.Spawns.PlacementFailure(cell);
                    Require(nav.PlacementFloodCount==floods+1,"One-Citadel placement retains exactly one flood"); checkedFastPath=true; break;
                }
                Require(checkedFastPath && single.Spawns.CitadelAssignments.SelectionQueries==0,"Single-objective placement adds no comparisons");
            }
            using(var f=new Fixture(this))
            {
                var nav=f.Paths.Navigation;
                Require(nav.Destinations.Count==2 && f.Session.Objectives.Count==2,"Two independent authored objectives discovered");
                var a=f.Spawn("MCA"); var b=f.Spawn("MCB"); var tie=f.Spawn("MCTie");
                Require(a.AssignedCitadel.Root==f.First && b.AssignedCitadel.Root==f.Second && tie.AssignedCitadel.Root==f.First,"Shortest neutral journey and authored-order tie");
                Drain(f.Paths);
                Require(a.Movement.Waypoints[^1]==At(27,8) && b.Movement.Waypoints[^1]==At(27,16),"Final routes reach their assigned approach goals");
                b.Movement._PhysicsProcess(8); // Make the second-destination ship genuinely farther along its own journey.
                f.Paths._PhysicsProcess(1.0/60);
                var closest=TowerTargeting.Find(f.Spawns.Registry,At(14,12),4000,TowerTargeting.Priority.First);
                Require(closest==b,"First compares remaining journey across Citadels");
                Require(TowerTargeting.Find(f.Spawns.Registry,At(14,12),4000,TowerTargeting.Priority.Last)==a,"Last compares remaining journey across Citadels");
                a.Definition=(EnemyDefinition)a.Definition.Duplicate(true); a.Definition.MaxShield=20; a.RestoreShield(20);
                Require(TowerTargeting.Find(f.Spawns.Registry,At(14,12),4000,TowerTargeting.Priority.First,preference:TowerTargeting.Preference.Shielded)==a,"Shield preference remains ahead of cross-Citadel journey score");
                a.ApplyStatus(new(StatusEffectId.Freeze,0,1));
                Require(TowerTargeting.Find(f.Spawns.Registry,At(14,12),4000,TowerTargeting.Priority.First,StatusEffectId.Freeze,TowerTargeting.Preference.Shielded)==b,"Frozen exclusion still precedes shield preference across Citadels");
                TimedStatusScheduler.For(f.Spawns.Registry)._PhysicsProcess(1);
                string failure=f.Spawns.PlacementFailure(new(21,8));
                Require(failure.Contains("assigned Citadel"),"Cannot block assigned route while another Citadel stays reachable: "+failure);
                long queries=f.Spawns.CitadelAssignments.SelectionQueries;
                f.Grid.TryOccupy(new(21,8));
                Require(f.Spawns.SpawnWave("basic_mk1",new RandomNumberGenerator { Seed=1 },"MCA")==null,"Blocked assigned portal waits instead of switching");
                Require(f.Spawns.CitadelAssignments.SelectionQueries==queries && a.AssignedCitadel.Root==f.First,"Occupancy cannot reassign sources or existing enemies");
                f.Grid.ReleaseOccupancy(new(21,8));
                int firstHP=f.Session.Objectives[0].Health.CurrentHealth, secondHP=f.Session.Objectives[1].Health.CurrentHealth;
                b.Arrive(); Require(f.Session.Objectives[0].Health.CurrentHealth==firstHP && f.Session.Objectives[1].Health.CurrentHealth==secondHP-1,"Arrival damages only assigned objective");
                var handoff=a.Movement.CaptureRouteHandoff();
                Enemy child=GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>(); f.Main.GetNode("World/Enemies").AddChild(child); child.GlobalPosition=At(20,16);
                child.Initialize(f.Spawns.Registry,f.Paths,routeHandoff:handoff); Disable(child);
                Require(child.AssignedCitadel==a.AssignedCitadel,"Handoff preserves destination even beside another Citadel");
                child.Free();
                a.GlobalPosition=At(20,16); f.Spawns.Registry.UpdatePosition(a);
                var childScene=GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
                var childDefinition=GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy001/Enemy001.tres");
                var release=new DeathReleaseJob(a,new DeathSpawnConfig { ChildEnemyId="basic_mk1", ChildCount=1, ScatterRadiusPixels=0, InheritCheckpointProgress=false }, childScene, childDefinition, 1, null);
                var released=f.Spawns.SpawnDeathChild(release);
                Require(released!=null && released.AssignedCitadel==a.AssignedCitadel,"Actual DeathSpawn retains destination even with checkpoint inheritance disabled");
                Disable(released); Drain(f.Paths);
                Require(released.Movement.Waypoints[^1]==At(27,8),"Released child routes past nearer Citadel to inherited destination");
                released.Free(); tie.Free(); a.Free();
                Stopwatch watch=Stopwatch.StartNew();
                for(int i=0;i<500;i++) { var enemy=f.Spawn(i%2==0?"MCA":"MCB"); Require(enemy.AssignedCitadel.Root==(i%2==0?f.First:f.Second),"Burst retains cached assignment"); }
                Drain(f.Paths); watch.Stop();
                Require(f.Spawns.CitadelAssignments.SelectionQueries==queries,"500 spawns perform zero additional destination-comparison A*");
                GD.Print($"MC BENCH: setup={f.Spawns.CitadelAssignments.SetupMilliseconds:0.00}ms; selection queries={queries}; 500 spawn+bounded drain={watch.Elapsed.TotalMilliseconds:0.0}ms; shared profiles={nav.TotalSharedProfileCount}");
            }
            using(var f=new Fixture(this,checkpoint:true))
            {
                var ship=f.Spawn("MCA"); Require(ship.AssignedCitadel.Root==f.Second && ship.Movement.HeadingToCheckpoint,"Mandatory checkpoint changes neutral destination selection");
                var other=GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
                f.Main.GetNode("World/Enemies").AddChild(other); other.GlobalPosition=ship.GlobalPosition;
                other.Initialize(f.Spawns.Registry,f.Paths,checkpoint:ship.Movement.Checkpoint,requiresCheckpoint:true,assignedCitadel:f.Session.Objectives[0]); Disable(other);
                Drain(f.Paths);
                Require(float.IsFinite(ship.RemainingJourneyDistance) && other.RemainingJourneyDistance>ship.RemainingJourneyDistance,"Shared checkpoint tail cache distinguishes two assigned Citadels");
            }
            using(var f=new Fixture(this,checkpoint:true))
            {
                // One assigned Citadel has approach markers on BOTH sides of a prospective disconnection.
                f.Second.GetNode<Node2D>("ApproachPoints/Approach01").GlobalPosition=At(27,8);
                f.Grid.RebuildTerrain();
                var ship=f.Spawn("MCA"); Require(ship.AssignedCitadel.Root==f.Second,"Checkpoint source retains the second objective");
                string failure=f.Spawns.PlacementFailure(new(14,11));
                Require(failure.Contains("checkpoint"),"Cannot confuse disconnected approaches of the SAME Citadel: "+failure);
                f.Grid.TryOccupy(new(14,11));
                Require(f.Spawns.SpawnWave("basic_mk1",new RandomNumberGenerator { Seed=3 },"MCA")==null,"Source/radius cache refuses disconnected required checkpoint despite a reachable assigned approach");
            }
            using(var f=new Fixture(this,detour:true))
            { var ship=f.Spawn("MCA"); Require(ship.AssignedCitadel.Root==f.Second,"Navigable shortest path wins over nearer straight-line objective"); }
            using(var f=new Fixture(this))
            {
                Enemy original=f.Spawn("MCA"); var assigned=original.AssignedCitadel;
                f.Grid.TryOccupy(new(21,8)); f.Grid.TryOccupy(new(21,16)); f.Grid.RebuildTerrain();
                Require(f.Spawns.SpawnWave("basic_mk1",new RandomNumberGenerator { Seed=2 },"MCA")==null,"Explicit rebuild with no reachable Citadel refuses spawn");
                Require(original.AssignedCitadel==assigned,"Explicit source rebuild never redirects an existing enemy");
                f.Second.Free(); original.Movement._PhysicsProcess(.1);
                Require(original.AssignedCitadel==assigned,"Removing another objective cannot change destination identity");
                f.First.Free(); Vector2 before=original.GlobalPosition; original.Movement._PhysicsProcess(1);
                Require(original.GlobalPosition==before && !float.IsFinite(original.RemainingJourneyDistance),"Removed assigned objective stops movement and excludes stale scores");
            }
            using(var f=new Fixture(this,keepResults:true))
            {
                int events=0; f.Session.AnyDestroyed+=()=>events++;
                f.Session.Objectives[1].Health.TakeDamage(int.MaxValue);
                Require(f.Main.GetNode<LevelSessionController>("LevelSession").State==LevelSessionState.Defeat && events==1,"Nonprimary destruction triggers existing mission defeat exactly once");
                f.Session.Objectives[0].Health.TakeDamage(int.MaxValue); Require(events==1,"Two destroyed objectives cannot duplicate defeat");
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var outcome=f.Main.GetNode<LevelSessionController>("LevelSession");
                Require(outcome.Snapshot?.Outcome==LevelOutcome.Defeat && outcome.Snapshot.CitadelHealth==0,"One frozen defeat snapshot records failed objective");
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("MultiCitadelChecks: PASS MC1/MC2 destinations, fixed ownership, constrained selection, cross-objective targeting, placement, arrival, inheritance, one-time defeat and 500 enemies"); GetTree().Quit();
        }
        catch(Exception error) { GetTree().Paused=false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion
}
