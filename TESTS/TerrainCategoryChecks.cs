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
        VerifyPalette(passable.TileSet, new[] { 2, 4 });
        VerifyPalette(impassable.TileSet, new[] { 0, 1, 3 });
        VerifyPalette(decor.TileSet, new[] { 5 });
        WorldLevel template = GD.Load<PackedScene>("res://LEVELS/LevelTemplate.tscn").Instantiate<WorldLevel>();
        try
        {
            VerifyPalette(template.Passable.TileSet, new[] { 2, 4 });
            VerifyPalette(template.Impassable.TileSet, new[] { 0, 1, 3 });
            VerifyPalette(template.Decorations.TileSet, new[] { 5 });
        }
        finally { template.Free(); }
        TileSet original = passable.TileSet;
        Vector2I[] cells = { new(1, 1), new(2, 1), new(3, 1), new(4, 1), new(5, 1), new(6, 1), new(7, 1) };
        List<(TileMapLayer layer, Vector2I cell, int source, Vector2I atlas, int alternative)> backup = new();
        foreach (TileMapLayer layer in new[] { passable, impassable, decor })
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
            impassable.SetCell(cells[2], 0, new(1, 0));
            impassable.SetCell(cells[3], 0, new(0, 0));
            decor.SetCell(cells[4], 0, new(5, 0));
            decor.SetCell(cells[0], 0, new(5, 0));
            decor.SetCell(cells[2], 0, new(5, 0));
            passable.SetCell(cells[5], 0, new(2, 0));
            impassable.SetCell(cells[5], 0, new(0, 0));
            grid.RebuildTerrain();
            Require(grid.CanBuild(cells[0]) && !grid.BlocksEnemies(cells[0]), "Passable/buildable floor");
            Require(!grid.CanBuild(cells[1]) && !grid.BlocksEnemies(cells[1]), "Passable/nonbuildable floor");
            Require(grid.CanBuild(cells[2]) && grid.BlocksEnemies(cells[2]), "Impassable/buildable platform");
            Require(!grid.CanBuild(cells[3]) && grid.BlocksEnemies(cells[3]), "Impassable/nonbuildable wall");
            Require(!grid.CanBuild(cells[6]) && !grid.BlocksEnemies(cells[6]), "Unpainted space remains unbuildable/passable");
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
    private static void VerifyPalette(TileSet set, int[] expected)
    {
        Require(set.TileSize == new Vector2I(64, 64) && set.GetSourceCount() == 1, "Palette uses one 64px atlas");
        TileSetAtlasSource source = (TileSetAtlasSource)set.GetSource(0);
        Require(source.GetTilesCount() == expected.Length, "Palette contains only its category tiles");
        foreach (int x in expected) Require(source.HasTile(new(x, 0)), "Original atlas coordinate remains available");
    }

    // =========================================================
    // Fail integration checks immediately on a terrain-rule regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
