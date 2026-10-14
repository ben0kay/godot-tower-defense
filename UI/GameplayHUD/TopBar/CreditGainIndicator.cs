// Display-only reward popup: merge quick credit gains, then drift upward and fade after inactivity.
using Godot;
using System;

public partial class CreditGainIndicator : Label
{
    #region Runtime
    public long AccumulatedReward { get; private set; }
    private Vector2 _origin;
    private double _elapsed;
    private float _stackSeconds = 0.8f, _fadeSeconds = 0.5f, _risePixels = 12f;
    private bool _active;
    #endregion

    #region Setup
    // =========================================================
    // The indicator is inert until gameplay awards credits; it never intercepts UI input.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ResetRewards();
    }

    // =========================================================
    // Read presentation settings from the existing global UI Inspector Resource.
    public void Configure(UIMasterConfig config)
    {
        _stackSeconds = Mathf.Max(0.01f, config.CreditRewardStackSeconds);
        _fadeSeconds = Mathf.Max(0.01f, config.CreditRewardFadeSeconds);
        _risePixels = Mathf.Max(0f, config.CreditRewardRisePixels);
        AddThemeColorOverride("font_color", config.CreditRewardColor);
    }

    // =========================================================
    // Anchor the popup beneath Credits, including when the top bar changes size.
    public void SetOrigin(Vector2 origin)
    {
        _origin = origin;
        if (_active) RefreshVisuals();
        else Position = origin;
    }
    #endregion

    #region Reward feedback
    // =========================================================
    // Only positive, actually awarded amounts are shown; fast consecutive rewards add together.
    public void AddReward(int amount)
    {
        if (amount <= 0) return;
        AccumulatedReward = (_active && _elapsed < _stackSeconds ? AccumulatedReward : 0L) + amount;
        Text = $"+{AccumulatedReward:N0}";
        _elapsed = 0;
        _active = true;
        Show();
        RefreshVisuals();
        SetProcess(true);
    }

    // =========================================================
    // An initialized/reloaded level must not inherit the previous reward popup.
    public void ResetRewards()
    {
        _active = false; _elapsed = 0; AccumulatedReward = 0;
        Text = ""; Position = _origin; Modulate = Colors.White;
        Hide(); SetProcess(false);
    }

    // =========================================================
    // Update only while visible; restart the full hold/fade when another reward arrives.
    public override void _Process(double delta)
    {
        if (!_active) return;
        _elapsed += Math.Max(0d, delta);
        if (_elapsed >= _stackSeconds + _fadeSeconds) { ResetRewards(); return; }
        RefreshVisuals();
    }

    // =========================================================
    // Hold at full opacity, then rise and fade during the quiet period.
    private void RefreshVisuals()
    {
        float progress = Mathf.Clamp((float)((_elapsed - _stackSeconds) / _fadeSeconds), 0f, 1f);
        Position = _origin + Vector2.Up * (_risePixels * progress);
        Modulate = new Color(1f, 1f, 1f, 1f - progress);
    }
    #endregion
}
