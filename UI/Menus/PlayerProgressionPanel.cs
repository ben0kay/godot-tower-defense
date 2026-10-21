// Shared menu-only progression display; listens to committed changes and never polls.
using Godot;
using System;

public partial class PlayerProgressionPanel : VBoxContainer
{
    public bool AllowDebugGrant { get; set; }
    private Label _summary, _points, _status;
    private ProgressBar _bar;

    // =========================================================
    // Build a compact shared view; optional development grant is absent in release exports.
    public override void _Ready()
    {
        _summary = UIButtonFactory.Label("", 17);
        _points = UIButtonFactory.Label("", 14);
        _status = UIButtonFactory.Label("", 13);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _bar = new ProgressBar { CustomMinimumSize = new Vector2(0, 14), ShowPercentage = false };
        AddChild(_summary); AddChild(_bar); AddChild(_points); AddChild(_status);
        AttachDebugGrant();
        ProgressionService.Changed += OnChanged;
        Refresh();
    }

    // =========================================================
    // Release static subscriptions whenever menu scenes retire.
    public override void _ExitTree() => ProgressionService.Changed -= OnChanged;

    // =========================================================
    // Ignore changes for another identity; normal profile switching creates a fresh menu.
    private void OnChanged(string id)
    {
        if (ProfileStore.Selected?.Id == id) Refresh();
    }

    // =========================================================
    // A load failure stays visible and disables development writes; never display fake level 1.
    private void Refresh()
    {
        try
        {
            PlayerProgression state = ProgressionService.GetCurrent();
            _summary.Text = $"PLAYER LEVEL {state.Level}  —  {state.CurrentXP:N0} / {state.RequiredXP:N0} XP";
            _bar.MaxValue = state.RequiredXP; _bar.Value = state.CurrentXP;
            _bar.Visible = true;
            _points.Text = $"RESEARCH POINTS: {state.UnspentResearchPoints:N0}";
            _status.Text = ProgressionService.RecoveryMessage;
            _status.Visible = _status.Text.Length > 0;
            if (_grant != null) _grant.Disabled = false;
        }
        catch (Exception error)
        {
            _summary.Text = "PLAYER PROGRESSION UNAVAILABLE";
            _bar.Visible = false; _points.Text = "";
            _status.Text = error.Message; _status.Visible = true;
            if (_grant != null) _grant.Disabled = true;
        }
    }

}
