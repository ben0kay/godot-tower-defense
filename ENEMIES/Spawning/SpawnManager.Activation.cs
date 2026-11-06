// Registered portals remain route-protected; a separate eligibility cache controls visibility/spawn permission.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SpawnManager
{
    internal enum PortalActivationContext { FreshDeployment, CheckpointRestore }
    public event Action PortalReadinessChanged;
    public event Action<int> PortalOpeningWarning;
    public bool WaitingForPortal { get; private set; }
    private readonly HashSet<EnemyPortal> _watchedPortals = new();

    // =========================================================
    // Authoring validation runs at wave-set setup, never inside per-ship selection or navigation queries.
    internal void ConfigurePortalActivation(WaveDefinition[] waves, PortalActivationContext context = PortalActivationContext.FreshDeployment, int completed = 0)
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
        WatchPortalReadiness();
        if (context == PortalActivationContext.CheckpointRestore) RestorePortalActivation(waves, completed);
        else
        {
            int order = 0;
            foreach (var node in _portalObjects)
            {
                if (node is not EnemyPortal portal) continue;
                portal.ResetActivation();
                if (portal.AppearAtWave == 0)
                    portal.BeginMaterialization(order++ * PortalMaterializationStyle.Safe(portal.MaterializationStyle.StartStaggerSeconds, 0, 0, .2f));
            }
            RefreshPortalEligibility();
        }
    }

    // =========================================================
    // Appearance changes only a small bool array; no fabricated terrain edit, flood, A* or reassignment.
    private void RefreshPortalEligibility()
    {
        if (_portalEligible.Length != _portalCount) _portalEligible = new bool[_portalCount];
        for (int i = 0; i < _portalCount; i++)
            _portalEligible[i] = _portalObjects[i] is not EnemyPortal portal || portal.IsSpawnReady;
        PortalEligibilityRevision++;
    }
    internal void ActivatePortalsForWave(int number)
    {
        bool changed = false; int warnings = 0;
        foreach (var node in _portalObjects)
            if (node is EnemyPortal portal && portal.ActivateForWave(number))
            { changed = true; if (portal.MaterializationStyle.WarningEnabled) warnings++; }
        if (changed) RefreshPortalEligibility();
        if (warnings > 0) PortalOpeningWarning?.Invoke(warnings);
    }

    // =========================================================
    // Between-wave restore replays availability only, using actual completed authored numbers; no VFX/spawns.
    internal void RestorePortalActivation(WaveDefinition[] waves, int completed)
    {
        foreach (var node in _portalObjects)
        {
            if (node is not EnemyPortal portal) continue;
            bool active = portal.AppearAtWave == 0;
            for (int i = 0; i < completed && !active; i++) active = waves[i].Number == portal.AppearAtWave;
            portal.RestoreActivation(active);
        }
        WaitingForPortal = false;
        RefreshPortalEligibility();
    }

    // =========================================================
    // Readiness changes one eligibility slot; terrain and destination caches are untouched.
    private void WatchPortalReadiness()
    {
        ClearPortalReadiness();
        foreach (var node in _portalObjects)
            if (node is EnemyPortal portal && _watchedPortals.Add(portal)) portal.SpawnReady += OnPortalReady;
    }
    private void ClearPortalReadiness()
    {
        foreach (var portal in _watchedPortals)
            if (GodotObject.IsInstanceValid(portal)) portal.SpawnReady -= OnPortalReady;
        _watchedPortals.Clear();
    }
    private void OnPortalReady(EnemyPortal portal)
    {
        int source = _portalObjects.IndexOf(portal);
        if (source < 0 || source >= _portalEligible.Length) return;
        _portalEligible[source] = portal.IsSpawnReady;
        PortalEligibilityRevision++;
        PortalReadinessChanged?.Invoke();
    }
    private bool HasFormingPortal(string requiredPortal = "")
    {
        for (int i = 0; i < _portalCount; i++)
            if ((requiredPortal.Length == 0 || _portalPaths[i] == requiredPortal) &&
                _portalObjects[i] is EnemyPortal { OpeningState: EnemyPortal.PortalState.Forming }) return true;
        return false;
    }
}
