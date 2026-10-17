// In-memory terrain/registration helpers for focused multi-cell checks; no saved map edits.
using Godot;
using System;
using static AmmoLoopFixture;

internal sealed class FootprintFixture : IDisposable
{
    internal AmmoLoopFixture World { get; }
    internal WorldGrid Grid => World.Main.GetNode<WorldGrid>("World/Grid");
    internal NavigationGrid Nav => World.Spawns.Paths.Navigation;
    internal Vector2I Anchor = new(12, 12);
    private readonly TileSet _walls;
    internal FootprintFixture(Node owner)
    {
        World = new(owner); _walls = Grid.Level.Impassable.TileSet;
        Grid.Level.Impassable.TileSet = (TileSet)_walls.Duplicate(true);
        var atlas = (TileSetAtlasSource)Grid.Level.Impassable.TileSet.GetSource(0);
        atlas.GetTileData(Vector2I.Zero, 0).SetCustomData("can_build", true);
        Paint(new Rect2I(Anchor - Vector2I.One * 2, new(14, 14)), false); Grid.RebuildTerrain(); Nav.Refresh();
    }
    // =========================================================
    // Same category accepts different art; solid tests use a private buildable wall palette.
    internal void Paint(Rect2I rect, bool blocked, bool alternate = false)
    {
        for (int y = rect.Position.Y; y < rect.End.Y; y++) for (int x = rect.Position.X; x < rect.End.X; x++)
        {
            Vector2I cell = new(x, y); Grid.Level.Passable.EraseCell(cell); Grid.Level.Impassable.EraseCell(cell); Grid.Level.Interactive.EraseCell(cell);
            if (blocked) Grid.Level.Impassable.SetCell(cell, 0, Vector2I.Zero);
            else Grid.Level.Passable.SetCell(cell, 0, new(alternate && (x + y) % 2 == 0 ? 4 : 2, 0));
        }
    }
    internal int Balance => World.Wallet.GetBalance(World.Wallet.CreditsResource);
    internal int Occupied(Vector2I anchor, Vector2I size)
    { int n = 0; for (int y = 0; y < size.Y; y++) for (int x = 0; x < size.X; x++) if (Grid.IsOccupied(anchor + new Vector2I(x, y))) n++; return n; }
    internal TowerBase Build(string id, Vector2I anchor)
    {
        World.Placement.Call("CancelPlacement");
        World.Hud.GetBuildButton(id).EmitSignal(Button.SignalName.Pressed); World.Placement.Call("TryPlace", anchor);
        return World.Placement.TowerAt(anchor);
    }
    internal void NoBuild(string id, Vector2I anchor, Vector2I size)
    {
        int money = Balance, nodes = World.Towers.GetChildCount(), occupied = Occupied(anchor, size), revision = Grid.NavigationRevision;
        Build(id, anchor);
        Require(Balance == money && World.Towers.GetChildCount() == nodes && Occupied(anchor, size) == occupied
            && Grid.NavigationRevision == revision, "Rejected footprint leaves wallet/nodes/occupancy/navigation untouched");
    }
    public void Dispose()
    {
        TileSet copy = Grid.Level.Impassable.TileSet; Grid.Level.Impassable.TileSet = _walls;
        World.Dispose(); copy.Dispose();
    }
}
