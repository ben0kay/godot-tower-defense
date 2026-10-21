// Battlefield connection: incoming enemies damage assigned Citadels; health events update HUD and critical alerts.
// Reuses the existing alert catalog and outcome arbiter; no per-frame health polling or gameplay ownership changes.
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
    private NotificationManager _alerts;
    private readonly Dictionary<CitadelDestination, System.Action<int, int>> _criticalHandlers = new();
    private readonly HashSet<CitadelDestination> _criticalLatched = new();

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
    // Both physical and HUD Citadel click targets query the same authoritative per-Citadel buffer.
    public CreditBuffer CreditsFor(CitadelDestination destination) =>
        destination != null && _income.TryGetValue(destination, out Income income) ? income.Buffer : null;

    // =========================================================
    // World Citadel and its HUD cell collect through the same command; wallet failure preserves storage.
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
        _alerts = _hud.GetNodeOrNull<NotificationManager>("NotificationManager");
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
            System.Action<int, int> handler = (current, maximum) => OnHealthForAlert(objective, current, maximum);
            objective.Health.HealthChanged += handler;
            _criticalHandlers.Add(objective, handler);
            _income.Add(objective, new Income(CreditBufferCapacity, CreditsPerCycle, CreditCycleSeconds));
        }
        _hud.BindCitadels(Objectives, this);
        foreach (var objective in Objectives)
        {
            var display = new CitadelCreditDisplay { Name = "CreditDisplay", Position = new Vector2(-8, 166), Size = new Vector2(144, 24) };
            objective.Root.AddChild(display); display.Bind(_income[objective].Buffer);
        }
        RefreshProductionClock();
    }

    // =========================================================
    // Remove both subscriptions on unload, avoiding dangling listeners across levels.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) _registry.EnemyArrived -= OnEnemyArrived;
        if (GodotObject.IsInstanceValid(Health)) Health.HealthChanged -= UpdateHud;
        if (Objectives != null) foreach (var objective in Objectives)
        {
            if (!GodotObject.IsInstanceValid(objective.Health)) continue;
            objective.Health.Destroyed -= OnDestroyed;
            if (_criticalHandlers.TryGetValue(objective, out var handler)) objective.Health.HealthChanged -= handler;
        }
        if (GodotObject.IsInstanceValid(_waves)) _waves.Changed -= RefreshProductionClock;
        _criticalHandlers.Clear(); _criticalLatched.Clear(); _income.Clear();
        _alerts = null;
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
    // Health thresholds are event-driven per Citadel, not checked during combat/physics callbacks.
    // Warn once on entering the living critical band; rearm only after recovery above the threshold.
    private void OnHealthForAlert(CitadelDestination objective, int current, int maximum)
    {
        if (_destroyed || maximum <= 0 || objective?.Available != true) return;
        float criticalAt = maximum * Mathf.Clamp(_hud?.Config?.HealthCriticalThreshold ?? .25f, 0f, 1f);
        if (current > criticalAt) { _criticalLatched.Remove(objective); return; }
        if (current <= 0 || !_criticalLatched.Add(objective) || _alerts == null) return;

        MajorAlertDefinition template = _alerts.Catalog?.Find("citadel_critical");
        if (template == null) return;
        // A separate transient ID preserves the manager's cooldown/priority per objective, not per map.
        // Duplicate the Resource so F7's shared catalog artwork and wording never change.
        MajorAlertDefinition alert = (MajorAlertDefinition)template.Duplicate();
        alert.Id = template.Id + ":" + objective.Number;
        if (Objectives.Count > 1)
        {
            alert.Title = objective.DisplayName + " IN DANGER";
            alert.Description = objective.DisplayName + " integrity has reached a critical level.";
        }
        _alerts.ShowMajor(alert);
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
