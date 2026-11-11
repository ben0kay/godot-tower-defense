// Keeps impact timing on the existing movement tick and displaces only eligible ship artwork.
using Godot;
using System;

public partial class Enemy
{
    private Sprite2D _staggerHull;
    private double _staggerCooldown, _staggerSpeedRemaining, _staggerVisualRemaining, _staggerVisualDuration;
    private float _staggerStrength, _staggerSlow;
    private Vector2 _staggerOffset, _staggerHullOrigin, _staggerThrusterOrigin;

    internal void ApplyImpactStagger(float strength, Vector2 away)
    {
        if (strength <= 0 || !_registry.ImpactStaggerEnabled ||
            (_staggerCooldown > 0 && strength <= _staggerStrength)) return;

        FeedbackMasterConfig settings = ImpactStagger.Settings;
        _staggerStrength = strength;
        _staggerCooldown = WeaponVisualMath.Safe(settings.StaggerCooldownSeconds, 0, .5f);
        _staggerSpeedRemaining = WeaponVisualMath.Safe(settings.StaggerSlowSeconds, 0, .3f);
        _staggerSlow = _staggerSpeedRemaining > 0
            ? strength * WeaponVisualMath.Safe(settings.StaggerMaximumSlow, 0, .5f) : 0;

        float distance = strength * WeaponVisualMath.Safe(settings.StaggerShovePixels, 0, 12);
        float duration = WeaponVisualMath.Safe(settings.StaggerReturnSeconds, 0, .5f);
        if (distance <= 0 || duration <= 0 || _fogConcealed || HasHullMountedWeapon ||
            !GodotObject.IsInstanceValid(_staggerHull) || !_staggerHull.IsVisibleInTree()) return;

        Transform2D toScreen = _staggerHull.GetGlobalTransformWithCanvas();
        Rect2 hull = _staggerHull.GetRect();
        Vector2 half = hull.Size.Abs() * .5f;
        Vector2 extent = toScreen.X.Abs() * half.X + toScreen.Y.Abs() * half.Y;
        Vector2 center = toScreen * hull.GetCenter();
        if (!GetViewportRect().Intersects(new Rect2(center - extent, extent * 2), true)) return;

        Transform2D transform = GlobalTransform;
        if (transform.Determinant() == 0) return;
        if (_staggerVisualRemaining <= 0)
        {
            if (!_registry.TryReserveStaggerVisual()) return;
            _staggerHullOrigin = _staggerHull.Position;
            if (GodotObject.IsInstanceValid(_thruster)) _staggerThrusterOrigin = _thruster.Position;
        }
        _staggerOffset = transform.AffineInverse().BasisXform(away * distance);
        _staggerVisualRemaining = _staggerVisualDuration = duration;
        SetStaggerOffset(_staggerOffset);
    }

    // =========================================================
    // Integrate the speed dip over its remaining fraction of the current movement step.
    internal float AdvanceImpactStagger(double delta)
    {
        if (_staggerCooldown > 0) _staggerCooldown = Math.Max(0, _staggerCooldown - delta);
        float speedScale = 1f;
        if (_staggerSpeedRemaining > 0 && delta > 0)
        {
            speedScale -= _staggerSlow * (float)(Math.Min(_staggerSpeedRemaining, delta) / delta);
            _staggerSpeedRemaining = Math.Max(0, _staggerSpeedRemaining - delta);
        }
        if (_staggerVisualRemaining > 0)
        {
            if (_staggerVisualRemaining <= delta) ClearStaggerVisual();
            else
            {
                _staggerVisualRemaining -= delta;
                float remaining = (float)(_staggerVisualRemaining / _staggerVisualDuration);
                SetStaggerOffset(_staggerOffset * (remaining * remaining));
            }
        }
        return speedScale;
    }

    private void SetStaggerOffset(Vector2 offset)
    {
        if (GodotObject.IsInstanceValid(_staggerHull)) _staggerHull.Position = _staggerHullOrigin + offset;
        if (GodotObject.IsInstanceValid(_thruster)) _thruster.Position = _staggerThrusterOrigin + offset;
    }

    private void ClearStaggerVisual()
    {
        if (_staggerVisualRemaining <= 0) return;
        _staggerVisualRemaining = 0;
        SetStaggerOffset(Vector2.Zero);
        if (GodotObject.IsInstanceValid(_registry)) _registry.ReleaseStaggerVisual();
    }

    internal void ClearImpactStagger()
    {
        ClearStaggerVisual();
        _staggerSpeedRemaining = _staggerCooldown = 0;
        _staggerSlow = _staggerStrength = 0;
    }
}
