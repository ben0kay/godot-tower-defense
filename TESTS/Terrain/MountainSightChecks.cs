// Optional narrow fixture for mountain metadata, sight traversal, acquisition fallback and cached locks.
using Godot;
using System;
using static AmmoLoopFixture;

public partial class MountainSightChecks : Node
{
    // =========================================================
    // Changes only a disposable runtime map, never authored terrain or player saves.
    public override void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                WorldGrid grid = f.Spawns.Registry.TerrainGrid;
                Require(grid != null, "Registry caches the owning grid");
                TileMapLayer layer = grid.Level.Impassable;
                layer.Clear(); layer.SetCell(new(10, 10), 2, new(2, 0)); grid.RebuildTerrain();
                Vector2 Point(int x, int y) => grid.CellToWorldCenter(new(x, y));
                Vector2 source = Point(8, 10), behind = Point(12, 10), side = Point(8, 15);
                Require(!grid.HasTowerLineOfSight(source, behind), "Mountain blocks horizontal sight");
                Require(!grid.HasTowerLineOfSight(behind, source), "Reverse sight is also blocked");
                Require(!grid.HasTowerLineOfSight(Point(10, 8), Point(10, 12)), "Mountain blocks vertical sight");
                Require(!grid.HasTowerLineOfSight(Point(9, 10), Point(10, 9)), "Corner grazing cannot leak through");
                Require(grid.HasTowerLineOfSight(source, side), "Exposed side remains visible");
                Enemy blocked = f.Target(behind), visible = f.Target(side);
                Require(TowerTargeting.Find(f.Spawns.Registry, source, 500, TowerTargeting.Priority.Closest) == visible,
                    "Blocked closest candidate does not suppress visible fallback");
                Require(!TowerTargeting.Valid(blocked, source, 500), "Cached lock rejects occlusion");
                LaserTower laser = f.Add<LaserTower>("laser", source);
                visible.GlobalPosition = behind;
                float health = visible.Health;
                laser._PhysicsProcess(1);
                Require(visible.Health == health, "Laser cannot acquire/damage a hidden target");
                layer.EraseCell(new(10, 10)); grid.RebuildTerrain();
                Require(grid.TowerSightBlockerCount == 0 && TowerTargeting.Valid(blocked, source, 500),
                    "Explicit terrain removal clears cached sight without occupancy/nav coupling");
                // Old rock keeps its independent movement/placement rules without becoming a sight blocker.
                layer.SetCell(new(10, 10), 2, new(1, 0)); grid.RebuildTerrain();
                Require(grid.TerrainBlocksEnemies(new(10, 10)) && grid.HasTowerLineOfSight(source, behind),
                    "Movement blockage does not implicitly become sight blockage");
            }
            GD.Print("MountainSightChecks: PASS"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
