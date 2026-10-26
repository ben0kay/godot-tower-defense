// Dormant battlefield checkpoint coordinator. Each command runs synchronously on the paused main thread.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public sealed class CampaignSession
{
    public const string CombatMessage = "Unavailable during combat";
    private readonly BattlefieldLauncher _battle;
    private CampaignData _campaign;
    public static string RecoveryMessage { get; private set; } = "";
    private static CampaignStore Store() => new(ProjectSettings.GlobalizePath("user://Profiles"));
    public CampaignSession(BattlefieldLauncher battle) { _battle = battle; _campaign = battle.RestoringCampaign; }

    // =========================================================
    // Query only at pause-open/click; no idle polling or full-registry per-frame checks.
    public string SaveUnavailableReason()
    {
        var session = _battle.GetNode<LevelSessionController>("LevelSession");
        var waves = _battle.GetNode<WaveManager>("Waves");
        var spawns = _battle.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        if (waves.State is WaveManager.WaveState.Spawning or WaveManager.WaveState.SpawnBlocked or WaveManager.WaveState.WaitingForClear ||
            spawns.Registry.Living.Count > 0 || spawns.DeathReleases.PendingJobs > 0 ||
            _battle.GetNode<EnemyBulletSystem>("World/EnemyBullets").ActiveCount > 0 ||
            _battle.GetNode<Node>("World/Projectiles").GetChildCount() > 0)
            return CombatMessage;
        if (ProfileStore.Selected == null) return "Select a profile to save this battlefield.";
        if (session.SavedRewardTicket?.ProfileId != ProfileStore.Selected.Id) return "This battlefield belongs to another profile or has no reward ticket.";
        if (session.State != LevelSessionState.Playing || waves.State is not (WaveManager.WaveState.Ready or WaveManager.WaveState.Completed))
            return "No between-wave checkpoint is available.";
        return "";
    }

    // =========================================================
    // Capture the actual selected-profile session; debug batches are explicitly retired rather than partially saved.
    public void Save()
    {
        if (!_battle.GetTree().Paused) throw new InvalidOperationException("Pause before saving.");
        string reason = SaveUnavailableReason(); if (reason.Length > 0) throw new InvalidOperationException(reason);
        foreach (var menu in Descendants(_battle).OfType<EnemySpawnMenu>()) menu.CancelBatch();
        var session = _battle.GetNode<LevelSessionController>("LevelSession");
        var waves = _battle.GetNode<WaveManager>("Waves");
        var citadels = _battle.GetNode<CitadelSession>("CitadelSession");
        var towers = _battle.GetNode<Node>("World/Towers").GetChildren().OfType<TowerBase>().ToArray();
        if (towers.Any(t => t.IsQueuedForDeletion() || t.IsDestroyed || string.IsNullOrEmpty(t.TowerTypeId)))
            throw new InvalidOperationException("A tower transaction is still settling; resume briefly before saving.");
        var sources = towers.ToDictionary(t => t.GetInstanceId(), t => "tower:" + t.SaveId);
        foreach (var objective in citadels.Objectives) sources[objective.Root.GetInstanceId()] = "citadel:" + objective.Id;
        var camera = _battle.GetNode<CameraController>("CameraRig");
        var snapshot = new LevelSaveData
        {
            LevelId = session.SavedLevelId, ContentHash = ContentHash(Mission(session.SavedLevelId)),
            CompletedWaves = waves.CompletedWaves, WaveRandomState = waves.SavedRandomState,
            Balances = _battle.GetNode<EconomyManager>("Economy").CaptureCheckpoint(), Research = citadels.Research.CaptureCheckpoint(),
            RewardTicket = session.SavedRewardTicket, Run = session.CaptureCheckpoint(), Citadels = citadels.CaptureCheckpoint(),
            Towers = towers.Select(t => t.CaptureCheckpoint(id => sources.GetValueOrDefault(id))).ToList(),
            Logistics = _battle.GetNode<AmmunitionLogistics>("World/AmmunitionLogistics").CaptureCheckpoint(),
            BuilderRetry = _battle.GetNode<ConstructionDroneManager>("World/ConstructionDrones").SavedRetry,
            Builders = _battle.GetNode<ConstructionDroneManager>("World/ConstructionDrones").CaptureCheckpoint(),
            CameraPosition = SavePoint.From(camera.GlobalPosition), CameraZoom = camera.ZoomLevel
        };
        var store = Store(); string profile = ProfileStore.Selected.Id;
        _campaign ??= store.Load(profile) ?? CampaignData.Create(profile);
        var updated = _campaign with { Version = 2, SavedUtc = DateTime.UtcNow, Checkpoint = snapshot };
        ValidateTicket(updated); store.Save(profile, updated); _campaign = updated;
    }

    // =========================================================
    // Loading is explicitly profile-owned. Version-one metadata never enables a misleading Continue action.
    public static CampaignData ReadCurrent()
    {
        if (ProfileStore.Selected == null) throw new InvalidOperationException("Select a profile first.");
        var store = Store(); RecoveryMessage = "";
        var saved = store.Load(ProfileStore.Selected.Id); RecoveryMessage = store.RecoveryMessage;
        return saved;
    }

    // =========================================================
    // Menus can disable stale/metadata-only saves without probing or loading an entire battlefield.
    public static string LoadUnavailableReason(CampaignData campaign)
    {
        if (campaign?.Checkpoint == null) return "No saved battlefield for this profile.";
        try { ValidateTicket(campaign); return ""; }
        catch (Exception error) { return error.Message; }
    }

    // =========================================================
    // Fail stale tickets before constructing another world; never issue fresh XP identity for a restored run.
    private static void ValidateTicket(CampaignData campaign)
    {
        if (campaign?.Checkpoint == null) throw new InvalidOperationException("No saved battlefield is available.");
        if (ProfileStore.Selected?.Id != campaign.ProfileId) throw new InvalidOperationException("Saved battlefield belongs to another profile.");
        SaveValidation.Validate(campaign.Checkpoint, campaign.ProfileId);
        var progression = ProgressionService.GetCurrent(); var ticket = campaign.Checkpoint.RewardTicket;
        if (ticket.Number > progression.LastIssuedMissionNumber || ticket.Number <= progression.LastAwardedMissionNumber)
            throw new InvalidOperationException("This checkpoint's mission reward ticket has been retired. Start a fresh mission.");
    }

    // =========================================================
    // Prepare beside the existing scene while paused. Keep the original scene/save if validation or reconstruction fails.
    public static void LoadCurrent(Node context)
    {
        CampaignData campaign = ReadCurrent(); ValidateTicket(campaign);
        MissionDefinition mission = Mission(campaign.Checkpoint.LevelId); mission.Validate();
        if (ContentHash(mission) != campaign.Checkpoint.ContentHash)
            throw new InvalidOperationException("Mission content has changed since this save. The checkpoint was preserved.");
        var tree = context.GetTree(); Node previous = tree.CurrentScene;
        bool paused = tree.Paused; BattlefieldLauncher prepared = null;
        tree.Paused = true;
        try
        {
            prepared = GD.Load<PackedScene>("res://LEVELS/Runtime/Battlefield.tscn").Instantiate<BattlefieldLauncher>();
            prepared.Mission = mission; prepared.RestoringCampaign = campaign;
            tree.Root.AddChild(prepared);
            if (prepared.Saves == null) throw new InvalidOperationException("Saved battlefield initialization failed.");
            prepared.Saves.Restore(campaign.Checkpoint);
            // Retire old callbacks synchronously before enabling the restored world.
            if (previous != null) { previous.GetParent().RemoveChild(previous); previous.QueueFree(); }
            tree.CurrentScene = prepared; tree.Paused = false;
        }
        catch
        {
            if (GodotObject.IsInstanceValid(prepared)) prepared.Free();
            tree.Paused = paused;
            previous?.GetNodeOrNull<Camera2D>("CameraRig/Camera2D")?.MakeCurrent();
            throw;
        }
    }

    // =========================================================
    // Restore all instances first, then durable references, clocks and reservations; do not replay gameplay events.
    private void Restore(LevelSaveData data)
    {
        var session = _battle.GetNode<LevelSessionController>("LevelSession");
        if (session.RunStats == null || session.State != LevelSessionState.Playing) throw new InvalidOperationException("Battlefield initialization failed.");
        var placement = _battle.GetNode<TowerPlacementController>("TowerPlacement");
        var citadels = _battle.GetNode<CitadelSession>("CitadelSession");
        citadels.RestoreCheckpoint(data.Citadels);
        var towers = new Dictionary<string, TowerBase>(StringComparer.Ordinal);
        foreach (var saved in data.Towers) towers.Add(saved.Id, placement.RestorePlacement(saved));
        var sourceIds = towers.ToDictionary(p => "tower:" + p.Key, p => p.Value.GetInstanceId(), StringComparer.Ordinal);
        foreach (var objective in citadels.Objectives) sourceIds.Add("citadel:" + objective.Id, objective.Root.GetInstanceId());
        ulong detached = ulong.MaxValue;
        ulong Source(string key)
        {
            if (key.Length == 0) return 0;
            if (sourceIds.TryGetValue(key, out ulong id)) return id;
            if (!key.StartsWith("detached:", StringComparison.Ordinal)) throw new InvalidDataException("Saved disable source is missing.");
            sourceIds.Add(key, --detached); return detached;
        }
        foreach (var saved in data.Towers) towers[saved.Id].RestoreCheckpoint(saved, Source);
        _battle.GetNode<AmmunitionLogistics>("World/AmmunitionLogistics").RestoreCheckpoint(data.Logistics, towers);
        _battle.GetNode<ConstructionDroneManager>("World/ConstructionDrones").RestoreCheckpoint(data.Builders, towers, data.BuilderRetry);
        _battle.GetNode<EconomyManager>("Economy").RestoreCheckpoint(data.Balances);
        session.RestoreCheckpoint(data, towers.Values);
        _battle.GetNode<WaveManager>("Waves").RestoreCheckpoint(data.CompletedWaves, data.WaveRandomState);
        _battle.GetNode<CameraController>("CameraRig").RestoreCamera(data.CameraPosition, data.CameraZoom);
    }

    // =========================================================
    // Stable allowlisted mission IDs choose content, never arbitrary scene paths from a save file.
    private static MissionDefinition Mission(string id) => PlayableLevelCatalog.Entries.FirstOrDefault(m => m.LevelId == id && m.Enabled)
        ?? throw new InvalidOperationException("Saved mission is unavailable.");

    // =========================================================
    // One explicit on-demand walk supports debug cleanup without adding a world polling service.
    private static IEnumerable<Node> Descendants(Node root)
    { foreach (Node child in root.GetChildren()) { yield return child; foreach (Node nested in Descendants(child)) yield return nested; } }

    // =========================================================
    // Conservatively reject changed authored dependencies. Work runs on Save/Load only, never during combat ticks.
    private static string ContentHash(MissionDefinition mission)
    {
        var seen = new SortedSet<string>(StringComparer.Ordinal);
        void Visit(string path)
        {
            if (path.Length == 0 || !seen.Add(path)) return;
            foreach (string dependency in ResourceLoader.GetDependencies(path)) Visit(dependency.Split("::")[^1]);
        }
        Visit("res://LEVELS/Runtime/Battlefield.tscn"); Visit(mission.ResourcePath); Visit(mission.MapScenePath);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string path in seen)
        {
            if (!Godot.FileAccess.FileExists(path)) throw new InvalidOperationException("Mission dependency is unavailable: " + path);
            hash.AppendData(Encoding.UTF8.GetBytes(path + "\n"));
            hash.AppendData(SHA256.HashData(Godot.FileAccess.GetFileAsBytes(path)));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
