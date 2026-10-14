// Placed Battle Tower with staggered reusable targeting and lightweight guided test bullets.
using Godot;

public partial class BattleTower : Node2D
{
    #region Settings and State
    [Export] public BattleTowerConfig Config { get; set; }
    public Vector2I GridCell { get; private set; }
    private Sprite2D _sprite;
    private EnemyRegistry _registry;
    private Node2D _projectiles;
    private Enemy _target;
    private float _searchTimer, _shotTimer;
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache visuals and world services once; stagger periodic target acquisition.
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        SetProcess(false);
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _projectiles = GetNodeOrNull<Node2D>("../../Projectiles");
        _searchTimer = (GetInstanceId() % 97) / 97f * Mathf.Max(0.05f, Config.TargetSearchInterval);
        SetPhysicsProcess(_registry != null && _projectiles != null);
    }
    // =========================================================
    // Refresh First periodically to allow overtaking, checking cached range between searches.
    public override void _PhysicsProcess(double delta)
    {
        _searchTimer -= (float)delta; _shotTimer = Mathf.Max(0f, _shotTimer - (float)delta);
        if (_searchTimer <= 0f)
        {
            _target = TowerTargeting.Find(_registry, GlobalPosition, Mathf.Max(0f, Config.RangePixels), Config.TargetPriority);
            _searchTimer = Mathf.Max(0.05f, Config.TargetSearchInterval);
        }
        if (_shotTimer > 0f || Config.ShotsPerSecond <= 0f
            || !TowerTargeting.Valid(_target, GlobalPosition, Mathf.Max(0f, Config.RangePixels))) return;
        BasicProjectile bullet = new();
        bullet.Initialize(_target, Config.ProjectileSpeed, Config.DamagePerShot);
        _projectiles.AddChild(bullet); bullet.GlobalPosition = GlobalPosition;
        _shotTimer = 1f / Config.ShotsPerSecond;
    }
    #endregion

    #region Placement and Selection
    // =========================================================
    // Initialize a placed tower's logical cell and world position.
    public void SetPlacement(Vector2I cell, Vector2 center)
    {
        GridCell = cell;
        GlobalPosition = center;
    }

    // =========================================================
    // Modulate the existing sprite instead of instantiating selection effects.
    public void SetSelected(bool selected) =>
        _sprite.Modulate = selected ? new Color(1f, 0.9f, 0.68f) : Colors.White;
    #endregion
}
