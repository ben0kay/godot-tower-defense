// Level-owned outcomes, frozen telemetry and once-only mission reward orchestration; Results never grants XP.
using Godot;
using System;
using System.Collections.Generic;

public enum LevelSessionState { Playing, Victory, Defeat }

public partial class LevelSessionController : Node
{
    public LevelSessionState State { get; private set; } = LevelSessionState.Playing;
    public LevelResultSnapshot Snapshot { get; private set; }
    public MissionRewardResult Reward { get; private set; }
    public string RewardError { get; private set; } = "";
    private MissionRewardTicket _rewardTicket;
    private MissionXPPolicy _rewardPolicy;
    private bool _rewardPolicyCaptured;
    private LevelConfig _levelConfig;
    private string _rewardProfileId;
    public string NextScenePath { get; private set; }
    private WaveManager _waves;
    private CitadelHealth _health;
    private CitadelSession _citadels;
    private SpawnManager _spawns;
    private EnemyRegistry _registry;
    private LevelResults _results;
    public LevelRunStats RunStats { get; private set; }
    private PauseMenu _pause;
    private GameplayHUD _hud;
    private GameInput _input;
    private readonly Dictionary<Enemy, Action> _waveEnemies = new();
    private int _killed, _breached;
    private string _levelName, _scenePath;
    private bool _victoryQueued, _navigating;

    #region Lifecycle and tallying
    // =========================================================
    // Both battlefield launchers place this after services/results; no scene discovery or per-frame polling.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; SetProcess(false); SetPhysicsProcess(false);
        _waves = GetNode<WaveManager>("../Waves"); _citadels = GetNode<CitadelSession>("../CitadelSession"); _health = _citadels.Health;
        _spawns = GetNode<SpawnManager>("../World/EnemySystems/Spawning"); _registry = _spawns.Registry;
        _results = GetNode<LevelResults>("../LevelResults"); _pause = GetNode<PauseMenu>("../PauseMenu");
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD"); _input = GetNode<GameInput>("/root/GameInput");
        RunStats = new LevelRunStats { Name = "RunStats" }; AddChild(RunStats);
        _scenePath = GetParent() is BattlefieldLauncher launcher && launcher.Mission != null
            ? launcher.Mission.LaunchPath : GetParent().SceneFilePath;
        _levelName = PlayableLevelCatalog.Identify(GetParent(), _spawns.Paths.Navigation.Grid.Level);
        _levelConfig = _spawns.Paths.Navigation.Grid.Level.Settings;
        _rewardProfileId = GetNode<CitadelSession>("../CitadelSession").Research.ProfileId;
        if (GetParent() is BattlefieldLauncher savedLauncher && savedLauncher.RestoringCampaign?.Checkpoint != null)
        {
            _rewardTicket = savedLauncher.RestoringCampaign.Checkpoint.RewardTicket;
            _rewardPolicy = _rewardTicket.Policy; _rewardPolicyCaptured = true;
        }
        else if (!string.IsNullOrEmpty(_rewardProfileId)) PrepareReward();
        _spawns.WaveEnemyCreated += TrackWaveEnemy;
        _registry.EnemyKilled += CountKill; _registry.EnemyArrived += CountBreach;
        _citadels.AnyDestroyed += Defeat; _waves.WaveCompleted += OnWaveCompleted;
        _results.RetryRequested += Retry; _results.NextLevelRequested += NextLevel;
        _results.MenuRequested += ReturnToCampaign; _results.ExitGameRequested += ExitGame;
        _results.RewardRetryRequested += RetryReward;
        if (_citadels.AnyIsDestroyed) Defeat(); else QueueVictoryIfComplete();
    }

    // =========================================================
    // Provenance arrives before Initialize can resolve a spawn on entry hazards. F2 never emits this event.
    private void TrackWaveEnemy(Enemy enemy)
    {
        if (State != LevelSessionState.Playing || _waveEnemies.ContainsKey(enemy)) return;
        Action exit = () => Forget(enemy);
        _waveEnemies.Add(enemy, exit); enemy.TreeExiting += exit;
    }
    private bool Forget(Enemy enemy)
    {
        if (!_waveEnemies.Remove(enemy, out Action exit)) return false;
        if (GodotObject.IsInstanceValid(enemy)) enemy.TreeExiting -= exit;
        return true;
    }
    // Count the lethal arrival even though CitadelSession's handler latches defeat earlier in the same callback chain.
    private void CountKill(Enemy enemy) { if (Forget(enemy) && Snapshot == null) _killed++; }
    private void CountBreach(Enemy enemy) { if (Forget(enemy) && Snapshot == null) _breached++; }
    #endregion

    #region Outcome arbitration
    // =========================================================
    // WaveManager remains the authority for full authored completion, never a living-enemy-count shortcut.
    private void OnWaveCompleted(int number) => QueueVictoryIfComplete();
    private void QueueVictoryIfComplete()
    {
        if (State != LevelSessionState.Playing || _victoryQueued || _waves.TotalWaves == 0
            || _waves.State != WaveManager.WaveState.AllWavesComplete) return;
        _victoryQueued = true; Callable.From(ResolveVictory).CallDeferred();
    }
    private void ResolveVictory()
    {
        _victoryQueued = false;
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || State != LevelSessionState.Playing || _waves.TotalWaves == 0
            || _waves.State != WaveManager.WaveState.AllWavesComplete) return;
        if (_citadels.AnyIsDestroyed) { Defeat(); return; }
        State = LevelSessionState.Victory;
        Capture(); Freeze(); Present();
    }

    // =========================================================
    // Latch/freeze defeat immediately. Settle this enemy's synchronous tally/wave callbacks before freezing the DTO.
    private void Defeat()
    {
        if (State != LevelSessionState.Playing) return;
        State = LevelSessionState.Defeat; Freeze();
        Callable.From(() => { if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || Snapshot != null) return; Capture(); Present(); }).CallDeferred();
    }
    private void Capture() => Snapshot = new LevelResultSnapshot
    {
        Outcome = State == LevelSessionState.Victory ? LevelOutcome.Victory : LevelOutcome.Defeat,
        LevelName = _levelName, EnemiesKilled = _killed, EnemiesBreached = _breached,
        WavesCompleted = _waves.CompletedWaves, TotalWaves = _waves.TotalWaves,
        CitadelHealth = _citadels.ResultHealth.CurrentHealth, CitadelMaxHealth = _citadels.ResultHealth.MaxHealth,
        TowersPlaced = RunStats.TowersPlaced, TowersConstructed = RunStats.TowersConstructed,
        UpgradesPurchased = RunStats.UpgradesPurchased, MissionSeconds = RunStats.MissionSeconds
    };
    private void Freeze()
    {
        RunStats.Freeze();
        _hud.GetNodeOrNull<NarrationDisplay>("NarrationDisplay")?.Clear();
        _pause.LockForResults(); _hud.SetTowerBarOpen(false, true);
        _input.Bind(this, GameInput.BackAction, ConsumeBack, 200);
        _input.Bind(this, GameInput.BuildAction, () => true, 200);
        GetTree().Paused = true;
    }
    private bool ConsumeBack() { if (!_results.DismissError()) _results.SkipReveal(); return true; }
    private void Present()
    {
        NextScenePath = Snapshot.Outcome == LevelOutcome.Victory ? PlayableLevelCatalog.NextScene(_levelName) : null;
        TryAwardReward();
        _results.ShowResults(Snapshot, NextScenePath != null);
        _results.SetMissionReward(Reward, RewardError, string.IsNullOrEmpty(_rewardProfileId));
    }

    // =========================================================
    // Reserve a monotonic ticket at deployment; failed preparation is visible and can be retried at Results.
    private void PrepareReward()
    {
        try
        {
            if (!_rewardPolicyCaptured) { _rewardPolicy = MissionXPPolicy.Capture(_levelConfig); _rewardPolicyCaptured = true; }
            _rewardTicket = ProgressionService.BeginMission(_rewardProfileId, _levelName, _rewardPolicy);
            RewardError = "";
        }
        catch (Exception error) { RewardError = error.Message; }
    }

    // =========================================================
    // One persisted award after authoritative outcome capture; failures never masquerade as saved XP.
    private void TryAwardReward()
    {
        if (Reward != null || string.IsNullOrEmpty(_rewardProfileId) || Snapshot == null) return;
        if (_rewardTicket == null) { if (RewardError.Length == 0) RewardError = "Mission reward preparation failed."; return; }
        try { Reward = ProgressionService.AwardMission(_rewardTicket, Snapshot); RewardError = ""; }
        catch (Exception error) { RewardError = error.Message; }
    }

    // =========================================================
    // Retrying persistence does not rerun gameplay, outcome calculation, reveal or navigation.
    private void RetryReward()
    {
        if (Snapshot == null || Reward != null || string.IsNullOrEmpty(_rewardProfileId)) return;
        if (_rewardTicket == null) PrepareReward();
        TryAwardReward(); _results.SetMissionReward(Reward, RewardError, false);
    }
    #endregion

    #region Navigation and cleanup
    // =========================================================
    // Guard even programmatically emitted intents; hidden/revealing controls cannot navigate early.
    private bool CanNavigate => Snapshot != null && _results.ActionsAvailable && !_navigating;
    public void Retry() { if (CanNavigate && State == LevelSessionState.Defeat) Navigate(_scenePath); }
    public void NextLevel() { if (CanNavigate && State == LevelSessionState.Victory && NextScenePath != null) Navigate(NextScenePath); }
    public void ReturnToCampaign() { if (CanNavigate) Navigate(MenuNavigation.CampaignScene); }
    public void ExitGame() { if (!CanNavigate) return; _navigating = true; GetTree().Paused = false; GetTree().Quit(); }
    private void Navigate(string scene)
    {
        _navigating = true;
        try
        {
            if (string.IsNullOrEmpty(scene) || !ResourceLoader.Exists(scene)) throw new InvalidOperationException("Mission scene is unavailable.");
            MenuNavigation.Open(this, scene);
        }
        catch (Exception error) { _navigating = false; _results.ShowNavigationError(error.Message); }
    }

    // =========================================================
    // Unload retires callbacks/provenance; external scene replacement cannot leave the next screen paused.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_spawns)) _spawns.WaveEnemyCreated -= TrackWaveEnemy;
        if (GodotObject.IsInstanceValid(_registry)) { _registry.EnemyKilled -= CountKill; _registry.EnemyArrived -= CountBreach; }
        if (GodotObject.IsInstanceValid(_citadels)) _citadels.AnyDestroyed -= Defeat;
        if (GodotObject.IsInstanceValid(_waves)) _waves.WaveCompleted -= OnWaveCompleted;
        foreach (var pair in _waveEnemies) if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.TreeExiting -= pair.Value;
        _waveEnemies.Clear();
        if (GodotObject.IsInstanceValid(_results))
        { _results.RewardRetryRequested -= RetryReward; _results.RetryRequested -= Retry; _results.NextLevelRequested -= NextLevel; _results.MenuRequested -= ReturnToCampaign; _results.ExitGameRequested -= ExitGame; }
        if (GodotObject.IsInstanceValid(_input)) _input.Unbind(this);
        if (State != LevelSessionState.Playing) GetTree().Paused = false;
    }
    #endregion

    // =========================================================
    // Explicit checkpoint input/output, with no purchase or reward replay.
    internal MissionRewardTicket SavedRewardTicket => _rewardTicket;
    internal string SavedLevelId => _levelName;
    internal RunSaveData CaptureCheckpoint() => RunStats.CaptureCheckpoint(_killed, _breached);
    internal void RestoreCheckpoint(LevelSaveData data, IEnumerable<TowerBase> towers)
    { _killed = data.Run.Killed; _breached = data.Run.Breached; RunStats.RestoreCheckpoint(data.Run, towers); }
}
