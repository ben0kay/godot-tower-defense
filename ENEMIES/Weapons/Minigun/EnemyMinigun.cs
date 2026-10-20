// Optional burst clock delivers real unguided shots to the shared world system; emitted bullets outlive this gun.
using Godot;
using System;

public partial class EnemyMinigun : EnemyWeaponMount
{
    #region Private Cadence
    private double _burstSeconds, _restSeconds, _burst, _rest, _cooldown, _lifetime;
    private ProjectileVisualSnapshot? _visual;
    private EnemyBulletSystem _bullets;
    public long ShotRequests { get; private set; }
    public event Action<EnemyShotRequest> ShotRequested;
    // =========================================================
    // Capture the specialized source once; untyped or invalid configs cannot start a minigun.
    protected override bool CaptureAdditionalSettings()
    {
        _bullets = null; _visual = null;
        if (Config is not EnemyMinigunConfig minigun || !minigun.TryCaptureBurst(out _burstSeconds, out _restSeconds)) return false;
        _lifetime = minigun.BulletLifetimeSeconds; _visual = minigun.ProjectileVisual?.Capture();
        _bullets = OwnerEnemy.Registry.GetNodeOrNull<EnemyBulletSystem>("../../EnemyBullets");
        return true;
    }
    // =========================================================
    // Losing target/alignment, disable, reuse or removal starts the next engagement with fresh private timing.
    protected override void ResetWeapon() { _burst = _rest = _cooldown = 0; }
    // =========================================================
    // One launch attempt at most per tick; overflow consumes its cadence opportunity and never queues catch-up fire.
    protected override void OnAimReady(double delta)
    {
        if (_rest > 0) { _rest = Math.Max(0, _rest - delta); return; }
        if (_burst <= 0) _burst = _burstSeconds;
        bool canRequest = _cooldown <= delta;
        _cooldown = Math.Max(0, _cooldown - delta);
        if (canRequest && GodotObject.IsInstanceValid(_bullets))
        {
            Vector2 direction = -Muzzle.GlobalTransform.Y.Normalized();
            if (direction.IsFinite() && direction.LengthSquared() > .5f)
            {
                EnemyShotRequest request = new(Muzzle.GlobalPosition, direction, Settings.Damage, Settings.BulletSpeed, _lifetime, _visual);
                if (_bullets.TrySpawn(request)) { ShotRequests++; ShotRequested?.Invoke(request); }
                _cooldown = 1.0 / Settings.ShotsPerSecond;
            }
        }
        _burst = Math.Max(0, _burst - delta);
        if (_burst == 0) _rest = _restSeconds;
    }
    // =========================================================
    // A world-delivery subscriber must not survive this optional component's lifetime.
    public override void _ExitTree() { ShotRequested = null; _bullets = null; base._ExitTree(); }
    #endregion
}
