// Real world/session + private open terrain/settings for bounded deterministic death-release checks.
using Godot;
using System;
using System.Linq;
using static WaveBuilder;
using static MissionFixture;

internal sealed class DeathSpawnFixture : IDisposable
{
    internal MissionFixture Mission { get; }
    internal Node Main => Mission.Main;
    internal SpawnManager Spawns => Mission.Spawns;
    internal DeathReleaseQueue Queue => Spawns.DeathReleases;
    internal WaveManager Waves => Mission.Waves;
    internal WorldGrid Grid => Spawns.Paths.Navigation.Grid;
    internal Vector2 Position => Grid.CellToWorldCenter(new(20, 20));
    private readonly EnemyMasterConfig _settings;
    private ulong _frame;
    internal DeathSpawnFixture(Node runner, string scene = "res://Main.tscn")
    {
        Mission = new(runner, scene); Queue.ProcessMode = Node.ProcessModeEnum.Disabled;
        _settings = (EnemyMasterConfig)Spawns.Paths.Config.Duplicate(true);
        Spawns.Paths.Config = _settings; Spawns.Paths.Navigation.Config = _settings;
        Grid.Level.Passable.Clear(); Grid.Level.Impassable.Clear(); Grid.Level.Interactive.Clear();
        for (int y = 0; y < Grid.Config.HeightCells; y++) for (int x = 0; x < Grid.Config.WidthCells; x++) Grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
        Grid.RebuildTerrain(); Spawns.Paths.Navigation.Refresh();
    }
    internal Enemy Parent(bool wave = false, string id = "transporter_mk1")
    {
        Enemy ship;
        if (wave)
        {
            Waves.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy(id))) }, Spawns.Catalog.ToSceneMap());
            Require(Waves.StartWave(), "Death release test starts actual authored wave"); ship = Spawns.Registry.Living.Single();
        }
        else ship = Spawns.Spawn(id, true);
        Require(ship != null, "Registered type spawns through production API"); ship.GlobalPosition = Position; AmmoLoopFixture.Disable(ship); return ship;
    }
    internal DeathSpawnConfig Config(Enemy parent) => parent.GetNode<DeathSpawn>("Abilities/DeathSpawn").Config;
    internal void Tick(double delta)
    { Queue.Advance(delta, _frame = Math.Max(_frame + 1, Engine.GetPhysicsFrames() + 1)); foreach (Enemy child in Spawns.Registry.Living) AmmoLoopFixture.Disable(child); }
    internal void FinishReleases() { for (int i = 0; Queue.PendingJobs > 0 && i < 200; i++) Tick(.12); Require(Queue.PendingJobs == 0, "Bounded release jobs retire"); }
    public void Dispose() { Mission.Dispose(); _settings.Dispose(); }
}
