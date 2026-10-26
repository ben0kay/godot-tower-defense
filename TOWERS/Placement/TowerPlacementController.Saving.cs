// Saved placements reuse the real footprint index and removal lifecycle without payment or placement events.
using Godot;
using System;
using System.IO;

public partial class TowerPlacementController
{
    // =========================================================
    // One restore-only command is invoked in a new paused battlefield; failures release the whole new scene.
    internal TowerBase RestorePlacement(TowerSaveData saved)
    {
        TowerDefinition definition = Catalog.Find(saved.TypeId) ?? throw new InvalidDataException("Saved tower type is missing.");
        Vector2I cell = new(saved.X, saved.Y);
        string failure = _spawns.PlacementFailure(cell, definition.Footprint);
        if (failure.Length != 0) throw new InvalidDataException("Saved placement is no longer valid: " + failure);
        TowerBase tower = definition.Scene.Instantiate<TowerBase>();
        bool occupied = false;
        try
        {
            tower.InitializeDefinition(definition, Settings, GetNode<CitadelSession>("../CitadelSession").Research.ForTower(definition.TowerId));
            if (!tower.ValidateScene()) throw new InvalidDataException("Saved tower scene is invalid.");
            tower.PrepareCheckpoint(saved, _construction, _builders);
            if (!_grid.TryOccupy(cell, definition.Footprint)) throw new InvalidDataException("Saved footprints overlap.");
            occupied = true;
            tower.Position = _towers.ToLocal(_grid.FootprintCenter(cell, definition.Footprint));
            _towers.AddChild(tower); tower.SetPlacement(cell, _grid.FootprintCenter(cell, definition.Footprint));
            Vector2I footprint = definition.Footprint;
            for (int y = 0; y < footprint.Y; y++) for (int x = 0; x < footprint.X; x++)
                _placedTowers.Add(cell + new Vector2I(x, y), tower);
            Action exit = () => RemovePlacement(tower, cell, footprint);
            _placementExits.Add(tower, exit); tower.TreeExiting += exit;
            return tower;
        }
        catch
        {
            tower.CancelConstruction(); if (occupied) _grid.ReleaseOccupancy(cell, definition.Footprint);
            if (GodotObject.IsInstanceValid(tower)) tower.Free(); throw;
        }
    }
}
