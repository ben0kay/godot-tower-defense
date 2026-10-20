// Optional burst clock/request producer only; EG2 supplies real world-owned unguided bullet delivery.
using Godot;
using System;

public readonly record struct EnemyShotRequest(Vector2 Origin, Vector2 Direction, float Damage, float Speed);

public partial class EnemyMinigun : EnemyWeaponMount
{
    #region Private Cadence
    private double _burstSeconds, _restSeconds, _burst, _rest, _cooldown;
    public long ShotRequests { get; private set; }
    public event Action<EnemyShotRequest> ShotRequested;
    // =========================================================
    // Capture the specialized source once; untyped or invalid configs cannot start a minigun.
    protected override bool CaptureAdditionalSettings()
        => Config is EnemyMinigunConfig minigun && minigun.TryCaptureBurst(out _burstSeconds, out _restSeconds);
    // =========================================================
    // Losing target/alignment, disable, reuse or removal starts the next engagement with fresh private timing.
    protected override void ResetWeapon() { _burst = _rest = _cooldown = 0; }
    // =========================================================
    // One request at most per tick, no hitch catch-up; absence of an EG2 consumer never creates a fake bullet or hit.
    protected override void OnAimReady(double delta)
    {
        if (_rest > 0) { _rest = Math.Max(0, _rest - delta); return; }
        if (_burst <= 0) _burst = _burstSeconds;
        bool canRequest = _cooldown <= delta;
        _cooldown = Math.Max(0, _cooldown - delta);
        if (canRequest && ShotRequested != null)
        {
            Vector2 direction = -Muzzle.GlobalTransform.Y.Normalized();
            if (direction.IsFinite() && direction.LengthSquared() > .5f)
            {
                ShotRequests++;
                ShotRequested.Invoke(new(Muzzle.GlobalPosition, direction, Settings.Damage, Settings.BulletSpeed));
                _cooldown = 1.0 / Settings.ShotsPerSecond;
            }
        }
        _burst = Math.Max(0, _burst - delta);
        if (_burst == 0) _rest = _restSeconds;
    }
    // =========================================================
    // A world-delivery subscriber must not survive this optional component's lifetime.
    public override void _ExitTree() { ShotRequested = null; base._ExitTree(); }
    #endregion
}
