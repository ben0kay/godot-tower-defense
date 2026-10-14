// Battlefield connection: arriving enemies damage this level's Citadel and health events update the HUD.
// Keeps objective health logic independent of enemies, waves, and future victory/defeat orchestration.
using Godot;

public partial class CitadelSession : Node
{
    #region Dependencies
    public CitadelHealth Health { get; private set; }
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
        Health = level.Citadel?.GetNodeOrNull<CitadelHealth>("Health");
        if (Health == null || level.Settings == null)
            throw new System.InvalidOperationException("The active level needs a Citadel/Health child and LevelConfig.");

        Health.HealthChanged += UpdateHud;
        _registry.EnemyArrived += OnEnemyArrived;
        Health.Initialize(level.Settings.CitadelMaxHealth);
    }

    // =========================================================
    // Remove both subscriptions on unload, avoiding dangling listeners across levels.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) _registry.EnemyArrived -= OnEnemyArrived;
        if (GodotObject.IsInstanceValid(Health)) Health.HealthChanged -= UpdateHud;
    }
    #endregion

    #region Arrival and Display
    // =========================================================
    // CitadelDamage is per enemy type; killed enemies never emit EnemyArrived.
    private void OnEnemyArrived(Enemy enemy)
    {
        if (GodotObject.IsInstanceValid(Health))
            Health.TakeDamage(enemy?.Definition?.CitadelDamage ?? 0);
    }

    // =========================================================
    // The HUD only displays authoritative HP; the Citadel owns the changing health.
    private void UpdateHud(int current, int maximum)
    {
        if (GodotObject.IsInstanceValid(_hud)) _hud.SetCitadelHealth(current, maximum);
    }
    #endregion
}
