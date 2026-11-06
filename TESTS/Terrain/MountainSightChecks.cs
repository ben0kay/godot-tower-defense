// Optional narrow fixture for mountain metadata, sight traversal, acquisition fallback and cached locks.
using Godot;
using System;
using System.Collections.Generic;
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
                Require(!grid.HasWeaponLineOfSight(source, behind) && !grid.HasWeaponLineOfSight(behind, source)
                    && grid.HasTowerLineOfSight(source, side) == grid.HasWeaponLineOfSight(source, side),
                    "Neutral weapon API and compatibility wrapper share symmetric terrain rules");
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
                TowerRegistry towers = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
                Require(towers.TerrainGrid == grid && !EnemyTowerTargeting.Valid(towers, laser, behind, 500, behind),
                    "Enemy cached target rejects the same mountain as friendly targeting");
                BattleTower exposedTower = f.Add<BattleTower>("battle", Point(12, 15));
                List<TowerBase> candidates = new();
                Require(EnemyTowerTargeting.Closest(towers, behind, 500, candidates, behind) == exposedTower,
                    "Enemy skips hidden closest tower and acquires an exposed fallback");
                Require(EnemyTowerTargeting.Valid(towers, laser, behind, 500, Point(12, 12)),
                    "An independent offset muzzle can see around the obstacle while the hull-center view is blocked");
                Enemy gunner = GD.Load<PackedScene>("res://ENEMIES/Types/HeavyGunnerDrone/HeavyGunnerDrone.tscn").Instantiate<Enemy>();
                f.Main.GetNode("World/Enemies").AddChild(gunner); gunner.GlobalPosition = behind;
                gunner.Initialize(f.Spawns.Registry, f.Spawns.Paths); AmmoLoopFixture.Disable(gunner);
                EnemyMinigun gun = gunner.GetNode<EnemyMinigun>("Sprite/Weapons/Minigun");
                gun._PhysicsProcess(1);
                Require(gun.Target == null && !gun.Aligned && gun.ShotRequests == 0,
                    "A real enemy mount cannot acquire, aim or emit shots through the mountain");
                layer.EraseCell(new(10, 10)); grid.RebuildTerrain();
                gun._PhysicsProcess(1);
                Require(gun.Target == laser && gun.Aligned && gun.ShotRequests > 0,
                    "The same mount resumes normal firing after terrain sight clears");
                Require(EnemyTowerTargeting.Valid(towers, laser, behind, 500, behind),
                    "Terrain removal restores hostile weapon vision through the same cache");
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
