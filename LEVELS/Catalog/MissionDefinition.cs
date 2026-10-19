// One editable campaign entry; gameplay settings remain owned by the referenced map's LevelConfig.
using Godot;
using System;

[GlobalClass]
public partial class MissionDefinition : Resource
{
    #region Authoring
    [Export(PropertyHint.Range, "1,999,1")] public int Number { get; set; } = 1;
    [Export] public string Title { get; set; } = "New mission";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export(PropertyHint.File, "*.tscn")] public string MapScenePath { get; set; } = "";
    public PackedScene MapScene => GD.Load<PackedScene>(MapScenePath);
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.File, "*.tscn")] public string LegacyLauncher { get; set; } = "";
    public string LevelId => $"Level{Number:000}";
    public string LaunchPath => string.IsNullOrEmpty(LegacyLauncher) ? ResourcePath : LegacyLauncher;
    #endregion

    // =========================================================
    // Reject incomplete authoring before replacing a running scene; raw maps never become launchers.
    public void ValidateMetadata()
    {
        if (Number < 1 || Number > 999 || string.IsNullOrWhiteSpace(Title)
            || string.IsNullOrEmpty(MapScenePath) || !ResourceLoader.Exists(MapScenePath, "PackedScene"))
            throw new InvalidOperationException("Mission requires number 1–999, title and existing map scene path.");
    }
    // =========================================================
    // Deep validation belongs to deployment, so a large campaign menu does not load every map.
    public void Validate()
    {
        ValidateMetadata();
        Node map = MapScene.Instantiate();
        try
        {
            if (map is not WorldLevel level || level.Settings == null || level.GetNodeOrNull<Node>("Terrain/Passable") == null)
                throw new InvalidOperationException($"{LevelId} requires a WorldLevel map with settings and terrain.");
        }
        finally { map.Free(); }
    }
}
