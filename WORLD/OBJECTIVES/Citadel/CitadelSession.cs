// Battlefield connection: arriving enemies damage this level's Citadel and health events update the HUD.
// Keeps objective health logic independent of enemies, waves, and future victory/defeat orchestration.
using Godot;

public partial class CitadelSession : Node
{
    #region Dependencies
    public CitadelHealth Health { get; private set; }
    public System.Collections.Generic.IReadOnlyList<CitadelDestination> Objectives { get; private set; }
    public event System.Action AnyDestroyed;
    public bool AnyIsDestroyed { get { foreach (var objective in Objectives) if (!objective.Available || objective.Health.IsDestroyed) return true; return false; } }
    public CitadelHealth ResultHealth { get { foreach (var objective in Objectives) if (objective.Available && objective.Health.IsDestroyed) return objective.Health; return Health; } }
    private bool _destroyed;

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
        var nav = GetNode<NavigationGrid>("../World/EnemySystems/Navigation"); nav.Refresh();
        Objectives = nav.Destinations.ToArray();
        if (Objectives.Count == 0) throw new System.InvalidOperationException("The active level needs at least one valid Citadel.");
        Health = Objectives[0].Health;
        Health.HealthChanged += UpdateHud;
        _registry.EnemyArrived += OnEnemyArrived;
        Research = ResearchService.CaptureDeployment();
        foreach (var objective in Objectives)
        {
            objective.Health.Initialize(Research.CitadelMaximum(level.Settings.CitadelMaxHealth));
            objective.Health.Destroyed += OnDestroyed;
        }
    }

    // =========================================================
    // Remove both subscriptions on unload, avoiding dangling listeners across levels.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) _registry.EnemyArrived -= OnEnemyArrived;
        if (GodotObject.IsInstanceValid(Health)) Health.HealthChanged -= UpdateHud;
        if (Objectives != null) foreach (var objective in Objectives) if (GodotObject.IsInstanceValid(objective.Health)) objective.Health.Destroyed -= OnDestroyed;
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
    private void OnDestroyed() { if (_destroyed) return; _destroyed = true; AnyDestroyed?.Invoke(); }

    // =========================================================
    // The HUD only displays authoritative HP; the Citadel owns the changing health.
    private void UpdateHud(int current, int maximum)
    {
        if (GodotObject.IsInstanceValid(_hud)) _hud.SetCitadelHealth(current, maximum);
    }
    #endregion
}
