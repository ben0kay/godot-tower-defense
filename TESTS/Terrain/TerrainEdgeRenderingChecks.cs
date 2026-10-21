// E1/E2 regression: generic profile masks, real Level002 lifecycle, unchanged gameplay, and cached image output.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class TerrainEdgeRenderingChecks : Node
{
    #region Runner
    // =========================================================
    // Only runtime fixture tiles change; saved map scenes and navigation rules are never rewritten.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this, MenuNavigation.Level002Scene))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                var grid = f.Spawns.Paths.Navigation.Grid; var level = grid.Level;
                var controller = f.Main.GetNode<TerrainEdgeController>("World/TerrainEdges");
                var renderer = controller.GetNode<TerrainEdgeRenderer>("Renderer");
                var original = level.Passable.Material;
                TerrainEdgeFoundationChecks.Run();
                TerrainEdgeProfile profile = new() { MaterialId = "generic_soil", SourceId = 2, AtlasCells = new() { new(0, 0) } };
                TerrainEdgeMasterConfig config = new() { Enabled = true, Profiles = new() { profile } }; controller.Config = config;
                level.Passable.Clear(); level.Impassable.Clear(); level.Interactive.Clear();
                void Paint(int x, int y) => level.Passable.SetCell(new(x, y), 2, new(0, 0));
                Paint(2, 2); Paint(5, 2); Paint(6, 2); Paint(5, 3); Paint(6, 3);
                for (int x = 10; x < 16; x++) Paint(x, 2);
                for (int y = 6; y < 11; y++) for (int x = 2; x < 7; x++) if (x != 4 || y != 8) Paint(x, y);
                Paint(10, 6); Paint(10, 7); Paint(11, 7); Paint(0, 0); level.Passable.SetCell(new(80, 70), 1, new(3, 0));
                grid.RebuildTerrain();
                Require(renderer.SurfaceCount == 1 && !renderer.IsProcessing() && !renderer.IsPhysicsProcessing(), "One event-driven shared mask surface");
                Require(level.Passable.GetNode<TerrainEdgeFillRenderer>("CornerFill").StampCount > 0, "Concave quads use persistent cached canvas drawing");
                int revision = grid.NavigationRevision; long queries = f.Spawns.Paths.TotalCalculations;
                ShaderMaterial material = level.Passable.Material as ShaderMaterial;
                Require(material != null, "Original square pixels are masked on their original layer");
                ImageTexture texture = material.GetShaderParameter("edge_mask").AsGodotObject() as ImageTexture;
                byte[] first = texture.GetImage().GetData(); renderer.Rebuild();
                ImageTexture second = ((ShaderMaterial)level.Passable.Material).GetShaderParameter("edge_mask").AsGodotObject() as ImageTexture;
                Require(first.SequenceEqual(second.GetImage().GetData()), "Fixed-seed masks deterministic");
                Require(grid.NavigationRevision == revision && queries == f.Spawns.Paths.TotalCalculations, "Visual rebuild adds no navigation rebuild or AStar");
                Require(TerrainEdgeMask.Coverage(new(.01f, .01f), TerrainEdgeNeighbours.None, profile, new(2, 2), 1337) < .1f
                    && TerrainEdgeMask.Coverage(new(.5f, .5f), TerrainEdgeNeighbours.None, profile, new(2, 2), 1337) == 1,
                    "Isolated tile loses square corners but keeps center");
                var all = TerrainEdgeNeighbours.North | TerrainEdgeNeighbours.East | TerrainEdgeNeighbours.South | TerrainEdgeNeighbours.West;
                Require(TerrainEdgeMask.Coverage(new(.01f, .01f), all, profile, new(2, 2), 1337) == 1, "Painted neighbouring materials preserve closed joins");
                Require(TerrainEdgeMask.InnerCorner(new(.01f, .01f), profile) > .9f
                    && TerrainEdgeMask.InnerCorner(new(.5f, .5f), profile) == 0, "Concave fill rounds hole corner without covering hole center");
                int rebuilds = renderer.RebuildCount;
                Require(grid.TryOccupy(new(80, 70)) && renderer.RebuildCount == rebuilds, "Tower occupancy does not rebuild masks"); grid.ReleaseOccupancy(new(80, 70));
                using Image mask = second.GetImage(); mask.SavePng("user://terrain-edge-mask.png");
                level.Passable.Position += Vector2.Right * 64; grid.RebuildTerrain();
                Require(renderer.SurfaceCount == 1 && controller.Materials.ContainsKey(new(3, 2)), "Grid-aligned transformed layer uses world-to-grid mapping");
                level.Passable.Position += Vector2.Right; grid.RebuildTerrain();
                Require(renderer.SurfaceCount == 0, "Unsupported subcell transform preserves original visuals");
                level.Passable.Position = Vector2.Zero; grid.RebuildTerrain();
                config.Enabled = false; controller.Rebuild();
                Require(renderer.SurfaceCount == 0 && level.Passable.Material == original && renderer.MaskBytes == 0, "Disabled restores exact original material and frees cache");
                profile.SourceId = 1; profile.AtlasCells = new() { new(3, 0) }; profile.MaterialId = "other_material";
                config.Enabled = true; controller.Rebuild(); Require(renderer.SurfaceCount == 1, "Different texture and material opt in without renderer changes");
                profile.RenderEdges = false; controller.Rebuild(); Require(renderer.SurfaceCount == 0, "Profile can opt out independently");
                profile.RenderEdges = true; profile.Shape = TerrainEdgeShape.Straight; controller.Rebuild(); Require(renderer.SurfaceCount == 0, "Straight profile retains original presentation");
                profile.Shape = TerrainEdgeShape.Soft; profile.SourceId = 2; profile.AtlasCells = new() { new(0, 0) };
                level.Passable.Clear();
                for (int y = 1; y < 61; y++) for (int x = 1; x < 81; x++) Paint(x, y);
                grid.RebuildTerrain(); GD.Print($"EDGES cells=4800; buildMs={renderer.LastBuildMilliseconds:0.###}; maskBytes={renderer.MaskBytes}; surfaces={renderer.SurfaceCount}");
                Require(renderer.MaskBytes < 4 * 1048576 && renderer.SurfaceCount == 1, "Large map has bounded mask storage");
                config.MaximumMaskMegapixels = 1; controller.Rebuild(); Require(renderer.SurfaceCount == 0 && level.Passable.Material == original, "Over-budget masks safely retain original tiles");
                using Image art = new(); art.LoadSvgFromString(FileAccess.GetFileAsString("res://WORLD/TERRAIN/Tiles/Mars/mars_ground_atlas.svg"), 4); art.SavePng("user://mars-after.png");
                for (int y = 0; y < art.GetHeight(); y++) Require(art.GetPixel(255, y).R > .5f, "Rock artwork cannot bleed into dust cell");
                GD.Print("EDGES images=" + OS.GetUserDataDir());
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: E1/E2 shared profiles, masks, corners, deterministic cache, opt out, bounded generation and unchanged navigation"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Fail fast with the visual/gameplay contract under test.
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    #endregion
}
