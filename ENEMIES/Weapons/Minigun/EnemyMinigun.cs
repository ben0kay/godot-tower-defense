// Optional continuous/burst clock delivers real unguided shots to the shared world system; emitted bullets outlive this gun.
using Godot;
using System;

public partial class EnemyMinigun : EnemyWeaponMount
{
    #region Private Cadence
    private double _burstSeconds, _restSeconds, _burst, _rest, _cooldown, _lifetime;
    private bool _useBursts;
    private ProjectileVisualSnapshot? _visual;
    private EnemyBulletSystem _bullets;
    private WeaponVisualRenderer _visuals;
    private MuzzleFlashSnapshot? _flash;
    public long ShotRequests { get; private set; }
    public event Action<EnemyShotRequest> ShotRequested;
    // =========================================================
    // Capture the specialized source once; untyped or invalid configs cannot start a minigun.
    protected override bool CaptureAdditionalSettings()
    {
        _bullets = null; _visual = null; _flash = null; _visuals = null;
        if (Config is not EnemyMinigunConfig minigun || !minigun.TryCaptureBurst(out _burstSeconds, out _restSeconds)) return false;
        _useBursts = minigun.UseBursts;
        _lifetime = minigun.BulletLifetimeSeconds; _visual = minigun.ProjectileVisual?.Capture();
        _flash = minigun.MuzzleFlash?.Capture();
        _visuals = GetNodeOrNull<WeaponVisualRenderer>("WeaponVisuals");
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
        if (_useBursts && _rest > 0) { _rest = Math.Max(0, _rest - delta); return; }
        if (_useBursts && _burst <= 0) _burst = _burstSeconds;
        // Carry only a bounded fractional-frame remainder: steady cadence, one attempt maximum after a hitch.
        _cooldown = Math.Max(-Math.Min(delta, .05), _cooldown - delta);
        bool canRequest = _cooldown <= 1e-9;
        if (canRequest && GodotObject.IsInstanceValid(_bullets))
        {
            Vector2 direction = -Muzzle.GlobalTransform.Y.Normalized();
            if (direction.IsFinite() && direction.LengthSquared() > .5f)
            {
                EnemyShotRequest request = new(Muzzle.GlobalPosition, direction, Settings.Damage, Settings.BulletSpeed, _lifetime, _visual);
                if (_bullets.TrySpawn(request))
                {
                    if (_flash.HasValue && GodotObject.IsInstanceValid(_visuals)) _visuals.FlashAt(Muzzle, _flash.Value);
                    ShotRequests++; ShotRequested?.Invoke(request);
                }
                _cooldown = Math.Max(0, 1.0 / Settings.ShotsPerSecond + _cooldown);
            }
        }
        if (_useBursts)
        {
            _burst = Math.Max(0, _burst - delta);
            if (_burst == 0) _rest = _restSeconds;
        }
    }
    // =========================================================
    // Stop only the gun's transient presentation; world-owned bullets must keep travelling.
    protected override void OnMountStopped()
    { if (GodotObject.IsInstanceValid(_visuals)) _visuals.ClearFlashes(); }
    // =========================================================
    // A world-delivery subscriber must not survive this optional component's lifetime.
    public override void _ExitTree() { ShotRequested = null; _bullets = null; base._ExitTree(); _visuals = null; _flash = null; }
    #endregion
}
