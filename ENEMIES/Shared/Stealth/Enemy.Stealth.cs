// Enemy-owned stealth state bridge; combat detection reads cached booleans and visuals update only on transitions.
using Godot;

public partial class Enemy
{
    #region Stealth
    public EnemyStealthState Stealth { get; private set; }
    public bool HasStealth => Stealth != null;
    public bool IsEffectivelyStealthed => Stealth != null && !Stealth.IsRevealed;
    private StealthVisual _stealthVisual;
    // =========================================================
    // Spawn flags are instance-local; ordinary spawns never inherit a previous instance's reveal state.
    private void InitializeStealth(bool enabled)
    {
        ClearStealth(); if (!enabled) return;
        Stealth = new(); Stealth.Changed += RefreshStealthVisual;
        _stealthVisual = new(GetNodeOrNull<Sprite2D>("Sprite"), _defenseBar); RefreshStealthVisual();
    }
    // =========================================================
    // Reapply when a reveal changes or a shield aura is attached later.
    private void RefreshStealthVisual() => _stealthVisual?.Apply(IsEffectivelyStealthed, _shieldVisual);
    // =========================================================
    // Clear native references and subscription before scene reuse/unload.
    private void ClearStealth()
    {
        if (Stealth != null) { Stealth.Changed -= RefreshStealthVisual; Stealth.Clear(); }
        _stealthVisual?.Reset(); _stealthVisual = null; Stealth = null;
    }
    #endregion
}
