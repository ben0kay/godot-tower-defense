// One finite-wave scheduler with bounded spawning, manual progression and exact ownership.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WaveManager : Node
{
    public enum WaveState { Ready, Spawning, SpawnBlocked, WaitingForClear, Completed, AllWavesComplete, PausedError, Disabled }
    public WaveState State { get; private set; } = WaveState.Disabled;
    public int CurrentWave => _waveIndex < 0 ? 0 : _waves[_waveIndex].Number;
    public int TotalWaves => _waves.Length;
    public int PlannedRemaining { get; private set; }
    public int AliveCount => _owned.Count;
    public int EnemiesRemaining => PlannedRemaining + AliveCount;
    public bool CanStart => (State == WaveState.Ready || State == WaveState.Completed) && _waveIndex + 1 < _waves.Length;
    public string StatusMessage { get; private set; } = "";
    public event Action Changed;
    public event Action<int> WaveCompleted;
    private SpawnManager _spawns;
    private GameplayHUD _hud;
    private WaveDefinition[] _waves = Array.Empty<WaveDefinition>();
    private IReadOnlyDictionary<string, PackedScene> _scenes;
    private readonly HashSet<Enemy> _owned = new();
    private readonly RandomNumberGenerator _random = new();
    private WaveEnemyEntry[] _sequence;
    private int _waveIndex = -1, _batchIndex, _entryIndex;
    private double _countdown;
    private bool _startAttemptReserved;

    #region Lifecycle
    // =========================================================
    // Main places Waves after UI/services; no first wave starts merely from loading the level.
    public override void _Ready()
    {
        _spawns = GetNode<SpawnManager>("../World/EnemySystems/Spawning");
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD");
        _hud.WaveStartRequested += OnStartRequested;
        try
        {
            string set = _spawns.Paths.Navigation.Grid.Level.Settings.WaveSetId;
            Configure(WaveCatalog.GetWaves(set), WaveCatalog.LoadScenes());
        }
        catch (Exception exception) { SetError(exception.Message); }
    }

    // =========================================================
    // Detach owned ships and HUD on unload; no timers/coroutines survive the scene.
    public override void _ExitTree()
    {
        ReleaseOwnership();
        if (GodotObject.IsInstanceValid(_hud)) _hud.WaveStartRequested -= OnStartRequested;
    }

    // =========================================================
    // Reset scheduler state; existing live ships become unowned and cannot delay the next set.
    public void Configure(WaveDefinition[] waves, IReadOnlyDictionary<string, PackedScene> scenes)
    {
        ReleaseOwnership(); _waveIndex = -1; PlannedRemaining = 0; _sequence = null; _countdown = 0; _startAttemptReserved = false;
        _waves = Array.Empty<WaveDefinition>(); _scenes = null;
        try { WaveCatalog.Validate(waves, scenes); }
        catch (Exception exception) { SetError(exception.Message); return; }
        _waves = (WaveDefinition[])waves.Clone();
        _scenes = new Dictionary<string, PackedScene>(scenes);
        _random.Seed = unchecked((ulong)_spawns.Paths.Config.WaveRandomSeed);
        State = _waves.Length > 0 ? WaveState.Ready : WaveState.Disabled;
        StatusMessage = ""; SetPhysicsProcess(false); Publish();
    }

    // =========================================================
    // Keep signal handlers void while StartWave exposes an idempotent acceptance result.
    private void OnStartRequested() => StartWave();

    // =========================================================
    // Accept one manual start, prepare its first batch and spawn its first ship immediately.
    public bool StartWave()
    {
        if (!CanStart) return false;
        _waveIndex++; _batchIndex = 0; _entryIndex = 0;
        PlannedRemaining = _waves[_waveIndex].Batches.Sum(b => b.Count);
        _sequence = WaveCatalog.Compose(_waves[_waveIndex].Batches[0], _random);
        _countdown = 0; StatusMessage = ""; State = WaveState.Spawning;
        SetPhysicsProcess(true); _startAttemptReserved = true; Publish(); Advance(0, 1);
        return true;
    }

    // =========================================================
    // The only scheduler tick; missed time never becomes an unbounded spawn catch-up burst.
    public override void _PhysicsProcess(double delta)
    {
        int limit = Mathf.Max(1, _spawns.Paths.Config.MaxWaveSpawnAttemptsPerFrame);
        // Reserve the immediate button-start attempt against the first scheduler frame.
        if (_startAttemptReserved) { limit--; _startAttemptReserved = false; }
        Advance(delta, limit);
    }

    // =========================================================
    // Process at most the configured attempt count; retry the same selected entry on failure.
    private void Advance(double delta, int limit)
    {
        if (State != WaveState.Spawning && State != WaveState.SpawnBlocked) return;
        _countdown = Math.Max(0, _countdown - Math.Max(0, delta));
        if (_countdown > 0.0000001) return;
        for (int attempt = 0; attempt < limit; attempt++)
        {
            WaveBatch batch = _waves[_waveIndex].Batches[_batchIndex];
            if (_entryIndex == _sequence.Length)
            {
                _batchIndex++; _entryIndex = 0;
                batch = _waves[_waveIndex].Batches[_batchIndex];
                _sequence = WaveCatalog.Compose(batch, _random);
            }
            WaveEnemyEntry entry = _sequence[_entryIndex];
            Enemy ship = _spawns.SpawnWave(_scenes[entry.EnemyId], _random, entry.Portal);
            if (ship == null)
            {
                State = WaveState.SpawnBlocked; StatusMessage = _spawns.LastMessage;
                _countdown = Mathf.Max(0.1f, _spawns.Paths.Config.WaveSpawnRetrySeconds);
                Publish(); return;
            }
            _owned.Add(ship); ship.Removed += OnEnemyRemoved;
            PlannedRemaining--; _entryIndex++;
            State = WaveState.Spawning; StatusMessage = "";
            if (_entryIndex == _sequence.Length)
            {
                if (_batchIndex + 1 == _waves[_waveIndex].Batches.Length)
                {
                    State = WaveState.WaitingForClear; SetPhysicsProcess(false);
                    Publish(); CompleteIfClear(); return;
                }
                _countdown = batch.After;
            }
            else _countdown = batch.Interval;
            Publish();
            if (_countdown > 0) return;
        }
    }

    // =========================================================
    // Kill and arrival both resolve once; debug/F2 ships never subscribe to this wave.
    private void OnEnemyRemoved(Enemy ship)
    {
        if (!_owned.Remove(ship)) return;
        ship.Removed -= OnEnemyRemoved;
        Publish(); CompleteIfClear();
    }

    // =========================================================
    // No alive-count shortcut can drop scheduled ships or auto-start a later wave.
    private void CompleteIfClear()
    {
        if (State != WaveState.WaitingForClear || PlannedRemaining != 0 || _owned.Count != 0) return;
        int completedNumber = CurrentWave;
        State = _waveIndex + 1 < _waves.Length ? WaveState.Completed : WaveState.AllWavesComplete;
        SetPhysicsProcess(false); Publish(); WaveCompleted?.Invoke(completedNumber);
    }

    // =========================================================
    // Unsubscribe without killing ships; loading a new Main scene owns physical cleanup.
    private void ReleaseOwnership()
    {
        foreach (Enemy ship in _owned) ship.Removed -= OnEnemyRemoved;
        _owned.Clear(); SetPhysicsProcess(false);
    }

    // =========================================================
    // Invalid setup is visible and cannot silently run another level's wave set.
    private void SetError(string message)
    {
        State = WaveState.PausedError; StatusMessage = message; SetPhysicsProcess(false);
        GD.PushWarning("Wave setup: " + message); Publish();
    }

    // =========================================================
    // Only events update UI; counts distinguish scheduled and owned live ships in the tooltip.
    private void Publish()
    {
        if (GodotObject.IsInstanceValid(_hud))
        {
            string button = State switch
            {
                WaveState.Ready => "START WAVE", WaveState.Completed => "NEXT WAVE",
                WaveState.AllWavesComplete => "COMPLETE", WaveState.SpawnBlocked => "BLOCKED",
                WaveState.PausedError => "SETUP ERROR", WaveState.Disabled => "NO WAVES",
                _ => $"{EnemiesRemaining} LEFT"
            };
            _hud.SetWaveControls(CurrentWave, TotalWaves, button, CanStart,
                $"{State}: {PlannedRemaining} scheduled, {AliveCount} alive. {StatusMessage}");
        }
        Changed?.Invoke();
    }
    #endregion
}
