// Headless integration checks for the live level, F2 spawning, navigation and combat.
using Godot;
using System;
using System.Linq;

public partial class EnemyIntegrationChecks : Node
{
    private Node2D _battle;
    private SpawnManager _spawns;
    private PathRequestManager _paths;
    private WorldGrid _grid;
    private int _checks;

    // =========================================================
    // Run against actual authored resources and production C# helpers.
    public override async void _Ready()
    {
        try
        {
            _battle = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Node2D>();
            AddChild(_battle);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            DisableTicks(_battle);
            _grid = _battle.GetNode<WorldGrid>("World/Grid");
            _spawns = _battle.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            _paths = _spawns.Paths;
            LevelDebugController debug = _grid.Level.GetNode<LevelDebugController>("Debug");
            debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true });
            Check(_spawns.Registry.Living.Count == 1, "F2 creates one Enemy001");
            debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true, Echo = true });
            Check(_spawns.Registry.Living.Count == 1, "F2 ignores OS key repeat");
            Enemy first = _spawns.Registry.Living[0]; first.Movement.SetPhysicsProcess(false);
            Check(first.Definition.EnemyId == "enemy001" && first.Health == 60, "Enemy001 resource identity and health");
            DefenseBar bar = first.GetNode<DefenseBar>("DefenseBar");
            Check(!bar.Visible, "Full-health Enemy001 starts with no health bar");
            first.TakeDamage(12);
            Check(bar.Visible && Mathf.IsEqualApprox(bar.HealthFraction, 0.8f),
                "Damage reveals reusable health bar at correct fill");
            first.Rotation = Mathf.Pi * 0.5f; first.UpdateDefenseBarTransform();
            Check(Mathf.Abs(Mathf.AngleDifference(first.Rotation, -bar.Rotation)) < 0.001f,
                "Health bar cancels ship rotation");
            float normalWidth = bar.WidthPixels;
            bar.SetHealth(5000f, 10000f);
            Check(bar.WidthPixels > normalWidth
                && bar.WidthPixels <= bar.Style.MaxWidthPixels,
                "High-HP defense bar grows but respects configured width cap");
            bar.SetHealth(10000f, 10000f);
            Check(!bar.Visible, "Full health hides reusable defense bar");
            bar.SetHealth(0f, 10000f);
            Check(!bar.Visible, "Zero health hides reusable defense bar");
            bar.SetHealth(first.Health, first.Definition.MaxHealth);
            int ticks = 0;
            while (first.IsAlive && ticks++ < 6000)
            {
                _paths._PhysicsProcess(1.0 / 60);
                first.Movement._PhysicsProcess(1.0 / 60);
                if (first.IsAlive) CheckSilent(_paths.Navigation.HasClearance(first.GlobalPosition, first.Definition.RadiusPixels), "Ship crossed blocked terrain");
            }
            Check(!first.IsAlive && _spawns.Registry.ArrivedCount == 1, "Authored Portal route reaches a Citadel approach");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
            for (int i = 0; i < 100; i++)
            {
                Enemy ship = _spawns.Spawn(scene, true);
                CheckSilent(ship != null, "Spam spawn failed"); ship.Movement.SetPhysicsProcess(false);
            }
            int frames = 0;
            while (_paths.PendingCount > 0 && frames++ < 1000)
            {
                _paths._PhysicsProcess(1.0 / 60);
                CheckSilent(_paths.LastFrameCalculations <= _paths.Config.MaxPathCalculationsPerFrame, "Path budget exceeded");
            }
            Check(_paths.PendingCount == 0 && _spawns.Registry.Living.Count == 100, "100 spammed enemies obey A* budget");
            Enemy a = _spawns.Registry.Living[0], b = _spawns.Registry.Living[1];
            a.GlobalPosition = new Vector2(640, 640); b.GlobalPosition = new Vector2(740, 640);
            a.Movement.AcceptPath(new[] { a.GlobalPosition, a.GlobalPosition + new Vector2(0, 700) }, _grid.NavigationRevision);
            b.Movement.AcceptPath(new[] { b.GlobalPosition, b.GlobalPosition + new Vector2(0, 100) }, _grid.NavigationRevision);
            Vector2 towerPos = new(640, 640);
            Check(TowerTargeting.Find(_spawns.Registry, towerPos, 150, TowerTargeting.Priority.First) == b, "First uses route distance");
            Check(TowerTargeting.Find(_spawns.Registry, towerPos, 150, TowerTargeting.Priority.Closest) == a, "Closest uses tower distance");
            Check(TowerTargeting.Find(_spawns.Registry, towerPos, 50, TowerTargeting.Priority.First) == a, "Targeting respects weapon range");
            foreach (Enemy ship in _spawns.Registry.Living.ToArray()) ship.TakeDamage(10000);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Enemy target = _spawns.Spawn(scene, true); target.Movement.SetPhysicsProcess(false);
            while (_paths.PendingCount > 0) _paths._PhysicsProcess(1.0 / 60);
            BattleTower tower = GD.Load<PackedScene>("res://TOWERS/Offensive/BattleTower/BattleTower.tscn").Instantiate<BattleTower>();
            tower.Config = (BattleTowerConfig)tower.Config.Duplicate();
            _battle.GetNode<Node2D>("World/Towers").AddChild(tower); tower.SetPhysicsProcess(false);
            tower.GlobalPosition = target.GlobalPosition + new Vector2(100, 0);
            tower.Config.TargetSearchInterval = 0.05f;
            tower.GetNode<Node2D>("TurretPivot").Rotation = Mathf.Pi * 0.5f;
            for (int i = 0; i < 12; i++) tower._PhysicsProcess(1.0 / 60);
            Check(_battle.GetNode<Node2D>("World/Projectiles").GetChildCount() == 0,
                "Tower holds fire while turning toward an opposite target");
            Check(tower.Rotation == 0, "Aiming leaves placement root fixed");
            tower.GetNode<Node2D>("TurretPivot").Rotation = -Mathf.Pi * 0.5f;
            HudTowerChecks.Run(_battle);
            int killedBefore = _spawns.Registry.KilledCount;
            for (int i = 0; i < 380 && target.IsAlive; i++)
            {
                tower._PhysicsProcess(1.0 / 60);
                foreach (BasicProjectile bullet in _battle.GetNode<Node2D>("World/Projectiles").GetChildren().OfType<BasicProjectile>().ToArray())
                { bullet.SetPhysicsProcess(false); if (!bullet.IsQueuedForDeletion()) bullet._PhysicsProcess(1.0 / 60); }
                if (i == 44) Check(target.Health == 48, "One configured 12-damage shot during first 0.75 seconds");
                if (i == 89) Check(target.Health == 36, "One shot per second respects cooldown");
            }
            Check(!target.IsAlive && _spawns.Registry.KilledCount == killedBefore + 1, "Configured tower fires bullets and kills Enemy001");
            target.TakeDamage(1000);
            Check(_spawns.Registry.KilledCount == killedBefore + 1, "Death and deregistration happen once");
            tower.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Enemy repath = _spawns.Spawn(scene, true); repath.Movement.SetPhysicsProcess(false);
            while (_paths.PendingCount > 0) _paths._PhysicsProcess(1.0 / 60);
            Vector2 before = repath.GlobalPosition;
            _grid.RebuildTerrain(); repath.Movement._PhysicsProcess(1.0 / 60);
            _paths._PhysicsProcess(1.0 / 60);
            Check(repath.GlobalPosition != before && _paths.PendingCount == 0, "Unchanged terrain audit preserves a valid moving route");
            repath.TakeDamage(10000);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _grid.Level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.MapEdges;
            _grid.Level.Settings.AllowedEdges = LevelConfig.MapEdge.Left;
            _grid.RebuildTerrain();
            Enemy edge = _spawns.Spawn(scene);
            Check(edge != null && _grid.WorldToCell(edge.GlobalPosition).X == 0, "Map-edge mode uses allowed edge");
            edge.TakeDamage(10000);
            Check(_spawns.Spawn(scene, true) == null, "Portal-only debug does not override edge-only level permissions");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            SwarmNavigationChecks.Run(_grid, _paths, _spawns);
            ConfigureCorridor();
            Check(_spawns.PlacementFailure(new Vector2I(5, 10)).Length > 0, "Tower cannot seal the only spawn route");
            _battle.GetNode<TowerPlacementController>("TowerPlacement").Call("TryPlace", new Vector2I(5, 10));
            Check(!_grid.IsOccupied(new Vector2I(5, 10)), "Actual placement rejects a sealed route");
            Check(_paths.Navigation.FindPath(_grid.CellToWorldCenter(new(3, 10)), new(9, 10)).Length > 0, "Controlled corridor has a route");
            _grid.TryOccupy(new Vector2I(5, 10));
            Check(_paths.Navigation.FindPath(_grid.CellToWorldCenter(new(3, 10)), new(9, 10)).Length == 0, "Blocked corridor has no path fallback");
            _grid.ReleaseOccupancy(new Vector2I(5, 10));
            Check(_paths.Navigation.FindPath(_grid.CellToWorldCenter(new(3, 10)), new(9, 10)).Length > 0, "Occupancy removal restores route");
            GD.Print($"PASS: {_checks} enemy/navigation/combat integration checks"); GetTree().Quit();
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Replace terrain only inside this isolated test instance, keeping committed level art untouched.
    private void ConfigureCorridor()
    {
        _grid.Level.Ground.Clear(); _grid.Level.Walls.Clear();
        for (int y = 0; y < _grid.Config.HeightCells; y++)
            for (int x = 0; x < _grid.Config.WidthCells; x++)
                _grid.Level.Walls.SetCell(new(x, y), 0, new(0, 0));
        for (int x = 2; x <= 9; x++)
        { _grid.Level.Walls.EraseCell(new(x, 10)); _grid.Level.Ground.SetCell(new(x, 10), 0, new(2, 0)); }
        Node2D portal = _grid.Level.GetNode<Node2D>("Portals/EnemyPortal");
        portal.GlobalPosition = _grid.CellToWorldCenter(new(2, 10));
        Node points = _grid.Level.Citadel.GetNode<Node>("ApproachPoints");
        foreach (Node point in points.GetChildren()) ((Node2D)point).GlobalPosition = _grid.CellToWorldCenter(new(9, 10));
        _grid.Level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals;
        _grid.RebuildTerrain();
    }

    // =========================================================
    // Manual ticks make combat and navigation checks deterministic and fast.
    private void DisableTicks(Node node)
    {
        node.SetProcess(false); node.SetPhysicsProcess(false);
        foreach (Node child in node.GetChildren()) DisableTicks(child);
    }

    // =========================================================
    // Print one result per behavior while silently checking every movement segment.
    private void Check(bool condition, string message)
    { CheckSilent(condition, message); _checks++; GD.Print("PASS: " + message); }

    // =========================================================
    // Fail the headless run immediately on a correctness violation.
    private void CheckSilent(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
