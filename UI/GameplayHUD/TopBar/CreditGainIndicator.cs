// Inline credit transaction feedback: quick gains/spending stack and pulse, then fade after inactivity.
using Godot;
using System;

public partial class CreditGainIndicator : Label
{
    #region State
    public long AccumulatedReward { get; private set; }
    public bool IsExpense { get; private set; }
    private Color _gainColor, _spendColor;
    private Vector2 _origin;
    private double _elapsed, _pulseElapsed;
    private float _stackSeconds, _fadeSeconds, _risePixels, _pulseSeconds, _pulseScale;
    private bool _active;
    #endregion

    #region Setup
    // =========================================================
    // Never consume mouse input; the popup does not process while hidden.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ResetRewards();
    }

    // =========================================================
    // Read colors and timing from the shared UI config, not per-enemy gameplay rules.
    public void Configure(UIMasterConfig config)
    {
        _stackSeconds = Mathf.Max(0.01f, config.CreditRewardStackSeconds);
        _fadeSeconds = Mathf.Max(0.01f, config.CreditRewardFadeSeconds);
        _risePixels = Mathf.Max(0f, config.CreditRewardRisePixels);
        _pulseSeconds = Mathf.Max(0.01f, config.CreditRewardPulseSeconds);
        _pulseScale = Mathf.Max(1f, config.CreditRewardPulseScale);
        _gainColor = config.CreditRewardColor;
        _spendColor = config.CreditSpendColor;
        AddThemeConstantOverride("outline_size", 2);
    }

    // =========================================================
    // The HUD calculates a text-width anchor adjacent to the displayed credits.
    public void SetOrigin(Vector2 origin)
    {
        _origin = origin;
        RefreshVisuals();
    }
    #endregion

    #region Credit transactions
    // =========================================================
    // Accepted positive wallet grants share one accumulating green popup.
    public void AddReward(int amount) => AddTransaction(amount, false);

    // =========================================================
    // Successful purchases share the same popup but use an amber negative amount.
    public void AddExpense(int amount) => AddTransaction(amount, true);

    // =========================================================
    // A transaction of the opposite type starts a new burst, never cancels actual balances.
    private void AddTransaction(int amount, bool expense)
    {
        if (amount <= 0) return;
        AccumulatedReward = _active && _elapsed < _stackSeconds && IsExpense == expense
            ? AccumulatedReward + amount : amount;
        IsExpense = expense;
        Text = expense ? $"(-{AccumulatedReward:N0})" : $"(+{AccumulatedReward:N0})";
        Color color = expense ? _spendColor : _gainColor;
        AddThemeColorOverride("font_color", color);
        AddThemeColorOverride("font_outline_color", new Color(color.R, color.G, color.B, 0.30f));
        _elapsed = 0d;
        _pulseElapsed = 0d;
        _active = true;
        Show();
        RefreshVisuals();
        SetProcess(true);
    }

    // =========================================================
    // Reset on a new session so initial credits never look like earned income.
    public void ResetRewards()
    {
        _active = false; _elapsed = 0d; _pulseElapsed = 0d;
        AccumulatedReward = 0; IsExpense = false;
        Text = ""; Position = _origin; Scale = Vector2.One;
        SelfModulate = Colors.White;
        Hide(); SetProcess(false);
    }

    // =========================================================
    // Only animate when a popup is visible; repeating rewards restart the pulse.
    public override void _Process(double delta)
    {
        if (!_active) return;
        double dt = Math.Max(0d, delta);
        _elapsed += dt; _pulseElapsed += dt;
        if (_elapsed >= _stackSeconds + _fadeSeconds) { ResetRewards(); return; }
        RefreshVisuals();
    }

    // =========================================================
    // Small brighten/enlarge on gain, then a quiet hold and gentle fade/drift.
    private void RefreshVisuals()
    {
        float fade = Mathf.Clamp((float)((_elapsed - _stackSeconds) / _fadeSeconds), 0f, 1f);
        float pulse = Mathf.Clamp(1f - (float)(_pulseElapsed / _pulseSeconds), 0f, 1f);
        pulse *= pulse;
        Position = _origin + Vector2.Up * (_risePixels * fade);
        Scale = Vector2.One * (1f + (_pulseScale - 1f) * pulse);
        SelfModulate = new Color(1f + pulse * 0.25f, 1f + pulse * 0.25f,
            1f + pulse * 0.25f, 1f - fade);
    }
    #endregion
}
