// Registered portals remain route-protected; a separate eligibility cache controls visibility/spawn permission.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SpawnManager
{
    // =========================================================
    // Authoring validation runs at wave-set setup, never inside per-ship selection or navigation queries.
    internal void ConfigurePortalActivation(WaveDefinition[] waves)
    {
        RefreshSources();
        var grid = Paths.Navigation.Grid;
        HashSet<string> registered = new();
        HashSet<Vector2I> reserved = new();
        foreach (NodePath path in grid.Level.Settings.ActivePortals)
        {
            string id = path?.ToString() ?? "";
            if (id.Length == 0 || id.StartsWith("/") || id.Split('/').Contains("..") || !registered.Add(id)) throw new ArgumentException("Missing/duplicate registered portal path: " + id);
            var portal = grid.Level.GetNodeOrNull<Node2D>(path);
            if (portal?.GetNodeOrNull<Marker2D>("SpawnExit") == null)
                throw new ArgumentException("Registered portal is missing or has no SpawnExit: " + id);
            if (portal is not EnemyPortal typed) continue; // Preserve legacy marker-based fixtures.
            if (typed.AppearAtWave < 0) throw new ArgumentException($"Portal {id}: AppearAtWave must be 0 (START) or a positive wave Number.");
            Vector2I anchor = grid.WorldToCell(typed.GlobalPosition);
            if (typed.SizeCells.X < 1 || typed.SizeCells.Y < 1 || !grid.IsInsideMap(anchor)
                || typed.SizeCells.X > grid.Config.WidthCells - anchor.X || typed.SizeCells.Y > grid.Config.HeightCells - anchor.Y) throw new ArgumentException("Invalid portal footprint: " + id);
            for (int y = 0; y < typed.SizeCells.Y; y++) for (int x = 0; x < typed.SizeCells.X; x++)
            {
                Vector2I cell = anchor + new Vector2I(x, y);
                if (!grid.IsInsideMap(cell) || !reserved.Add(cell)) throw new ArgumentException("Overlapping/out-of-map registered portal footprint: " + id);
            }
            if (typed.AppearAtWave > 0)
            {
                if (grid.Level.Settings.SpawnMode == LevelConfig.SpawnSourceMode.MapEdges)
                    throw new ArgumentException($"Future portal {id}: use Portals or Both mode so its route can be reserved.");
                if (!new Rect2I(anchor, typed.SizeCells).HasPoint(grid.WorldToCell(typed.GetNode<Marker2D>("SpawnExit").GlobalPosition)))
                    throw new ArgumentException($"Future portal {id}: SpawnExit must lie inside its reserved footprint.");
                int source = _portalPaths.IndexOf(id);
                if (source < 0 || !_sourceReachable[source] || !Paths.Navigation.HasClearance(_entries[source], 0)
                    || !CitadelAssignments.CanSpawn(typed, _entries[source], 0))
                    throw new ArgumentException($"Future portal {id}: blocked exit or invalid assigned Citadel/checkpoint route; fix the authored route before deployment.");
                if (!waves.Any(w => w.Number == typed.AppearAtWave))
                    GD.PushWarning($"Portal {id}: Wave {typed.AppearAtWave} is not authored; this portal will remain dormant.");
            }
        }
        foreach (var wave in waves)
        foreach (var batch in wave.Batches)
        foreach (var entry in batch.Entries)
        {
            if (entry.Portal.Length == 0) continue;
            int source = _portalPaths.IndexOf(entry.Portal);
            if (source < 0) throw new ArgumentException($"Wave {wave.Number}: required portal '{entry.Portal}' is not a permitted registered source.");
            if (_portalObjects[source] is EnemyPortal typed && typed.AppearAtWave > 0)
            {
                int activationIndex = Array.FindIndex(waves, w => w.Number == typed.AppearAtWave);
                int requestIndex = Array.IndexOf(waves, wave);
                if (activationIndex < 0 || activationIndex > requestIndex)
                    throw new ArgumentException($"Wave {wave.Number}: required portal '{entry.Portal}' appears at Wave {typed.AppearAtWave}; activate it by this wave or move the batch later.");
            }
        }
        RestorePortalActivation(waves, 0);
    }

    // =========================================================
    // Appearance changes only a small bool array; no fabricated terrain edit, flood, A* or reassignment.
    private void RefreshPortalEligibility()
    {
        if (_portalEligible.Length != _portalCount) _portalEligible = new bool[_portalCount];
        for (int i = 0; i < _portalCount; i++)
            _portalEligible[i] = _portalObjects[i] is not EnemyPortal portal || portal.IsSpawnActive;
        PortalEligibilityRevision++;
    }
    internal void ActivatePortalsForWave(int number)
    {
        bool changed = false;
        foreach (var node in _portalObjects) if (node is EnemyPortal portal) changed |= portal.ActivateForWave(number);
        if (changed) RefreshPortalEligibility();
    }

    // =========================================================
    // Between-wave restore replays availability only, using actual completed authored numbers; no VFX/spawns.
    internal void RestorePortalActivation(WaveDefinition[] waves, int completed)
    {
        foreach (var node in _portalObjects) if (node is EnemyPortal portal) portal.ResetActivation();
        for (int i = 0; i < completed; i++)
            foreach (var node in _portalObjects) if (node is EnemyPortal portal) portal.ActivateForWave(waves[i].Number);
        RefreshPortalEligibility();
    }
}
