// Headless placement timing fixture; prints validation, grid refresh and next-frame CPU times.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;

public partial class PlacementPerformanceChecks : Node
{
    // =========================================================
    // Run this unchanged fixture before/after navigation changes; no authored map is saved.
    public override async void _Ready()
    {
        try
        {
            Node2D main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Node2D>(); AddChild(main);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Disable(main);
            WorldGrid grid = main.GetNode<WorldGrid>("World/Grid");
            SpawnManager spawns = main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            PathRequestManager paths = spawns.Paths;
            grid.Level.Walls.Clear(); grid.Level.Ground.Clear();
            for (int y = 0; y < grid.Config.HeightCells; y++)
                for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Ground.SetCell(new(x, y), 0, new(2, 0));
            grid.RebuildTerrain();
            foreach (int population in new[] { 100, 500 })
            {
                foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000);
                PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
                for (int i = 0; i < population; i++)
                { Enemy ship = spawns.Spawn(scene, true); if (ship == null) throw new Exception("Fixture spawn failed"); ship.Movement.SetPhysicsProcess(false); }
                while (paths.PendingCount > 0) paths._PhysicsProcess(1.0 / 60);
                Enemy[] ships = spawns.Registry.Living.ToArray();
                double validation = 0, update = 0, frame = 0;
                Stopwatch watch = new();
                for (int sample = 0; sample < 5; sample++)
                {
                    Vector2I cell = new(20 + sample, 20);
                    watch.Restart(); string failure = spawns.PlacementFailure(cell);
                    validation = Math.Max(validation, watch.Elapsed.TotalMilliseconds);
                    if (failure.Length != 0) throw new Exception(failure);
                    watch.Restart(); grid.TryOccupy(cell); paths.Navigation.Refresh();
                    update = Math.Max(update, watch.Elapsed.TotalMilliseconds);
                    watch.Restart();
                    foreach (Enemy ship in ships) ship.Movement._PhysicsProcess(1.0 / 60);
                    paths._PhysicsProcess(1.0 / 60);
                    frame = Math.Max(frame, watch.Elapsed.TotalMilliseconds);
                    while (paths.PendingCount > 0) paths._PhysicsProcess(1.0 / 60);
                    grid.ReleaseOccupancy(cell); paths.Navigation.Refresh();
                }
                GD.Print($"PLACEMENT {population}: validation max {validation:0.00}ms; grid max {update:0.00}ms; next physics frame max {frame:0.00}ms");
            }
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Deterministic manual ticks prevent rendering from contaminating the CPU measurements.
    private static void Disable(Node node)
    { node.SetProcess(false); node.SetPhysicsProcess(false); foreach (Node child in node.GetChildren()) Disable(child); }
}
