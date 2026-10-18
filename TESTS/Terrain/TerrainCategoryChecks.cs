// Check independent movement/build rules and palette filtering without changing saved maps.
using Godot;
using System;
using System.Collections.Generic;

public static class TerrainCategoryChecks
{
    // =========================================================
    // Use unused cells and a temporary passable/nonbuildable tile; restore every authored input.
    public static void Run(WorldGrid grid)
    {
        TileMapLayer passable = grid.Level.Passable, impassable = grid.Level.Impassable, decor = grid.Level.Decorations;
        VerifyPalette(passable.TileSet, new[] { 2, 4 }, new[] { 2, 3 }, new[] { 0 });
        VerifyPalette(impassable.TileSet, new[] { 0, 3 }, new[] { 0, 1 }, new[] { 1 });
        VerifyPalette(decor.TileSet, new[] { 5 });
        WorldLevel template = GD.Load<PackedScene>("res://LEVELS/LevelTemplate.tscn").Instantiate<WorldLevel>();
        try
        {
            VerifyPalette(template.Passable.TileSet, new[] { 2, 4 }, new[] { 2, 3 }, new[] { 0 });
            VerifyPalette(template.Impassable.TileSet, new[] { 0, 3 }, new[] { 0, 1 }, new[] { 1 });
            VerifyPalette(template.Decorations.TileSet, new[] { 5 });
        }
        finally { template.Free(); }
        TileSet original = passable.TileSet;
        Vector2I[] cells = { new(1, 1), new(2, 1), new(3, 1), new(4, 1), new(5, 1), new(6, 1), new(7, 1), new(8, 1), new(9, 1), new(10, 1), new(11, 1), new(12, 1), new(13, 1) };
        List<(TileMapLayer layer, Vector2I cell, int source, Vector2I atlas, int alternative)> backup = new();
        foreach (TileMapLayer layer in new[] { passable, impassable, grid.Level.Interactive, decor })
            foreach (Vector2I cell in cells)
            {
                backup.Add((layer, cell, layer.GetCellSourceId(cell), layer.GetCellAtlasCoords(cell), layer.GetCellAlternativeTile(cell)));
                layer.EraseCell(cell);
            }
        try
        {
            passable.TileSet = (TileSet)original.Duplicate(true);
            ((TileSetAtlasSource)passable.TileSet.GetSource(0)).GetTileData(new(4, 0), 0).SetCustomData("can_build", false);
            passable.SetCell(cells[0], 0, new(2, 0));
            passable.SetCell(cells[1], 0, new(4, 0));
            grid.Level.Interactive.SetCell(cells[2], 0, new(1, 0));
            impassable.SetCell(cells[3], 0, new(0, 0));
            decor.SetCell(cells[4], 0, new(5, 0));
            decor.SetCell(cells[0], 0, new(5, 0));
            decor.SetCell(cells[2], 0, new(5, 0));
            passable.SetCell(cells[5], 0, new(2, 0));
            impassable.SetCell(cells[5], 0, new(0, 0));
            // New tiles live under stable source 1; source 0 and all authored painted IDs are untouched.
            passable.SetCell(cells[7], 1, new(2, 0)); // metal walkway, nonbuildable
            passable.SetCell(cells[8], 1, new(3, 0)); // walkable foundation, buildable
            impassable.SetCell(cells[9], 1, new(0, 0)); // bolted wall, buildable
            impassable.SetCell(cells[10], 1, new(1, 0)); // cracked wall, nonbuildable
            passable.SetCell(cells[11], 2, new(0, 0)); // light Martian dust; walkable, not buildable
            impassable.SetCell(cells[12], 2, new(1, 0)); // rocky Martian ground; blocked, not buildable
            grid.RebuildTerrain();
            Require(grid.CanBuild(cells[0]) && !grid.BlocksEnemies(cells[0]), "Passable/buildable floor");
            Require(!grid.CanBuild(cells[1]) && !grid.BlocksEnemies(cells[1]), "Passable/nonbuildable floor");
            Require(grid.CanBuild(cells[2]) && grid.BlocksEnemies(cells[2]), "Impassable/buildable platform");
            Require(!grid.CanBuild(cells[3]) && grid.BlocksEnemies(cells[3]), "Impassable/nonbuildable wall");
            Require(!grid.CanBuild(cells[6]) && !grid.BlocksEnemies(cells[6]), "Unpainted space remains unbuildable/passable");
            Require(!grid.CanBuild(cells[7]) && !grid.BlocksEnemies(cells[7]), "New decorative metal walkway is walkable but nonbuildable");
            Require(grid.CanBuild(cells[8]) && !grid.BlocksEnemies(cells[8]), "New cyan foundation is walkable and buildable");
            Require(grid.CanBuild(cells[9]) && grid.BlocksEnemies(cells[9]), "New bolted wall is blocked but buildable");
            Require(!grid.CanBuild(cells[10]) && grid.BlocksEnemies(cells[10]), "New cracked wall is blocked and nonbuildable");
            Require(!grid.CanBuild(cells[11]) && !grid.BlocksEnemies(cells[11]), "Mars dust is passable but unbuildable");
            Require(!grid.CanBuild(cells[12]) && grid.BlocksEnemies(cells[12]), "Mars rock is impassable and unbuildable");
            Require(grid.TryOccupy(cells[8]) && grid.BlocksEnemies(cells[8]), "Tower occupancy blocks the new cyan foundation");
            grid.ReleaseOccupancy(cells[8]);
            Require(!grid.BlocksEnemies(cells[8]), "Walkable foundation clears after tower removal");
            Require(grid.TryOccupy(cells[9]), "Tower may occupy buildable preblocked wall");
            grid.ReleaseOccupancy(cells[9]);
            Require(grid.BlocksEnemies(cells[9]), "Bolted wall remains blocked after tower removal");
            Require(!grid.CanBuild(cells[4]) && !grid.BlocksEnemies(cells[4]), "Decal on empty space has no gameplay effect");
            Require(!grid.CanBuild(cells[5]) && grid.BlocksEnemies(cells[5]), "Impassable overrides overlapping Passable");
            Require(grid.TryOccupy(cells[0]) && grid.BlocksEnemies(cells[0]), "Tower turns passable floor into an obstacle");
            grid.ReleaseOccupancy(cells[0]);
            foreach (NodePath path in grid.Level.Settings.ActivePortals)
                Require(!grid.CanBuild(grid.WorldToCell(grid.Level.GetNode<Node2D>(path).GlobalPosition)), "Portal footprint remains reserved");
            Require(!grid.CanBuild(grid.WorldToCell(grid.Level.Citadel.GlobalPosition))
                && grid.BlocksEnemies(grid.WorldToCell(grid.Level.Citadel.GlobalPosition)), "Citadel remains reserved and blocked");
        }
        finally
        {
            passable.TileSet = original;
            foreach (var cell in backup)
            {
                if (cell.source < 0) cell.layer.EraseCell(cell.cell);
                else cell.layer.SetCell(cell.cell, cell.source, cell.atlas, cell.alternative);
            }
            grid.RebuildTerrain();
        }
        Require(original.GetSource(0) is TileSetAtlasSource source
            && source.GetTileData(new(4, 0), 0).GetCustomData("can_build").AsBool(), "Temporary test tile does not mutate saved defaults");
        GD.Print("PASS: filtered palettes, four terrain combinations, decoration neutrality, overlap priority and reservations");
    }

    // =========================================================
    // Each selected layer exposes only its intended atlas choices, with the original coordinates.
    private static void VerifyPalette(TileSet set, int[] expected, int[] additions = null, int[] mars = null)
    {
        Require(set.TileSize == new Vector2I(64, 64) && set.GetSourceCount() == (mars != null ? 3 : additions != null ? 2 : 1), "Palette has correct 64px atlas source count");
        TileSetAtlasSource source = (TileSetAtlasSource)set.GetSource(0);
        Require(source.GetTilesCount() == expected.Length, "Original palette retains its old tile count");
        foreach (int x in expected) Require(source.HasTile(new(x, 0)), "Original atlas coordinate remains available");
        if (additions == null) return;
        TileSetAtlasSource added = (TileSetAtlasSource)set.GetSource(1);
        Require(added.TextureRegionSize == new Vector2I(64, 64) && added.GetTilesCount() == additions.Length,
            "New source1 adds only category-compatible 64px tiles");
        foreach (int x in additions) Require(added.HasTile(new(x, 0)), "New atlas coordinate is available in this palette");
        if (mars == null) return;
        TileSetAtlasSource planet = (TileSetAtlasSource)set.GetSource(2);
        Require(planet.TextureRegionSize == new Vector2I(64, 64) && planet.GetTilesCount() == mars.Length,
            "Mars palette source exposes only the intended terrain type");
        foreach (int x in mars) Require(planet.HasTile(new(x, 0)), "Mars atlas cell is registered with a stable source ID");
    }

    // =========================================================
    // Fail integration checks immediately on a terrain-rule regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
