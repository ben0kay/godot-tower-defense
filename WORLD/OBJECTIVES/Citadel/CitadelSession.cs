// Battlefield connection: arriving enemies damage this level's Citadel and health events update the HUD.
// Keeps objective health logic independent of enemies, waves, and future victory/defeat orchestration.
using Godot;
using System.Collections.Generic;

public partial class CitadelSession : Node
{
    #region Dependencies
    public CitadelHealth Health { get; private set; }
    public System.Collections.Generic.IReadOnlyList<CitadelDestination> Objectives { get; private set; }
    public event System.Action AnyDestroyed;
    public bool AnyIsDestroyed { get { foreach (var objective in Objectives) if (!objective.Available || objective.Health.IsDestroyed) return true; return false; } }
    public CitadelHealth ResultHealth { get { foreach (var objective in Objectives) if (objective.Available && objective.Health.IsDestroyed) return objective.Health; return Health; } }
    private bool _destroyed;

    [ExportGroup("Citadel Credit Production")]
    [Export(PropertyHint.Range, "1,100000,1,or_greater")] public int CreditBufferCapacity { get; set; } = 80;
    [Export(PropertyHint.Range, "1,100000,1,or_greater")] public int CreditsPerCycle { get; set; } = 10;
    [Export(PropertyHint.Range, "0.05,3600,0.05,or_greater")] public double CreditCycleSeconds { get; set; } = 2.0;
    private readonly Dictionary<CitadelDestination, Income> _income = new();
    private WaveManager _waves;
    private EconomyManager _economy;
    private LevelSessionController _levelSession;

    // =========================================================
    // One state bundle per Citadel, with no extra Node or per-enemy state.
    private sealed class Income
    {
        public readonly CreditBuffer Buffer;
        public readonly CreditProductionTimer Clock;
        public Income(int capacity, int amount, double seconds)
        { Buffer = new(capacity); Clock = new(amount, seconds); }
    }

    // =========================================================
    // CP1B world/HUD click targets will query the same authoritative per-Citadel buffer.
    public CreditBuffer CreditsFor(CitadelDestination destination) =>
        destination != null && _income.TryGetValue(destination, out Income income) ? income.Buffer : null;

    // =========================================================
    // A collect command is available for CP1B; no gameplay input consumes it in CP1A.
    public bool TryCollectCredits(CitadelDestination destination)
    {
        if (_destroyed || GetTree().Paused || (_levelSession != null && _levelSession.State != LevelSessionState.Playing)
            || destination?.Available != true || destination.Health.IsDestroyed || _economy == null)
            return false;
        return CreditsFor(destination)?.TryCollect(_economy, _economy.CreditsResource) == true;
    }

    public ResearchDeployment Research { get; private set; } = ResearchDeployment.None;
    private EnemyRegistry _registry;
    private GameplayHUD _hud;
    #endregion

    #region Lifecycle
    // =========================================================
    // Main places this node after World and UI so both are ready before the first health event.
    public override void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        WorldLevel level = GetNode<WorldGrid>("../World/Grid").Level;
        _registry = GetNode<EnemyRegistry>("../World/EnemySystems/Registry");
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD");
        _economy = GetNode<EconomyManager>("../Economy");
        _waves = GetNode<WaveManager>("../Waves");
        _levelSession = GetNodeOrNull<LevelSessionController>("../LevelSession");
        _waves.Changed += RefreshProductionClock;
        var nav = GetNode<NavigationGrid>("../World/EnemySystems/Navigation"); nav.Refresh();
        Objectives = nav.Destinations.ToArray();
        if (Objectives.Count == 0) throw new System.InvalidOperationException("The active level needs at least one valid Citadel.");
        Health = Objectives[0].Health;
        if (Objectives.Count == 1) Health.HealthChanged += UpdateHud;
        _registry.EnemyArrived += OnEnemyArrived;
        Research = ResearchService.CaptureDeployment();
        foreach (var objective in Objectives)
        {
            objective.Health.Initialize(Research.CitadelMaximum(level.Settings.CitadelMaxHealth));
            objective.Health.Destroyed += OnDestroyed;
            _income.Add(objective, new Income(CreditBufferCapacity, CreditsPerCycle, CreditCycleSeconds));
        }
        _hud.BindCitadels(Objectives);
        RefreshProductionClock();
    }

    // =========================================================
    // Remove both subscriptions on unload, avoiding dangling listeners across levels.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) _registry.EnemyArrived -= OnEnemyArrived;
        if (GodotObject.IsInstanceValid(Health)) Health.HealthChanged -= UpdateHud;
        if (Objectives != null) foreach (var objective in Objectives) if (GodotObject.IsInstanceValid(objective.Health)) objective.Health.Destroyed -= OnDestroyed;
        if (GodotObject.IsInstanceValid(_waves)) _waves.Changed -= RefreshProductionClock;
        _income.Clear();
    }
    #endregion

    #region Credit Production
    // =========================================================
    // Only authored wave activity enables ticking; wave transitions sleep the clock immediately.
    private void RefreshProductionClock()
    {
        bool active = _waves != null && _waves.State is WaveManager.WaveState.Spawning
            or WaveManager.WaveState.SpawnBlocked or WaveManager.WaveState.WaitingForClear;
        SetPhysicsProcess(active && !_destroyed);
    }

    // =========================================================
    // Main scene pause automatically stops physics; explicit guards protect direct/test invocations.
    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || _destroyed || (_levelSession != null && _levelSession.State != LevelSessionState.Playing) || _waves == null) return;
        bool active = _waves.State is WaveManager.WaveState.Spawning
            or WaveManager.WaveState.SpawnBlocked or WaveManager.WaveState.WaitingForClear;
        if (!active) return;
        foreach (var pair in _income)
            pair.Value.Clock.Advance(delta, pair.Key.Available && !pair.Key.Health.IsDestroyed, pair.Value.Buffer);
    }
    #endregion

    #region Arrival and Display
    // =========================================================
    // CitadelDamage is per enemy type; killed enemies never emit EnemyArrived.
    private void OnEnemyArrived(Enemy enemy)
    {
        var destination = enemy?.AssignedCitadel;
        if (destination?.Available == true) destination.Health.TakeDamage(enemy.Definition?.CitadelDamage ?? 0);
    }

    // =========================================================
    // One session event feeds the existing outcome arbiter, regardless of how many objectives are destroyed.
    private void OnDestroyed() { if (_destroyed) return; _destroyed = true; SetPhysicsProcess(false); AnyDestroyed?.Invoke(); }

    // =========================================================
    // The HUD only displays authoritative HP; the Citadel owns the changing health.
    private void UpdateHud(int current, int maximum)
    {
        if (GodotObject.IsInstanceValid(_hud)) _hud.SetCitadelHealth(current, maximum);
    }
    #endregion
}
