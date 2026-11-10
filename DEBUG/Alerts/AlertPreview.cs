// Debug-only F4 samples for the gameplay alert foundation; no gameplay events are emitted.
// Release builds do not register input handlers or show preview notifications.
using Godot;

public partial class AlertPreview : Node
{
    [ExportGroup("Development Preview")]
    [Export] public bool Enabled { get; set; } = true;

#if DEBUG
    private int _step;
#endif

    // =========================================================
    // Manually cycle toast, merge, queued alerts and priority interruption while playing.
    public void CycleSample()
    {
#if DEBUG
        if (!Enabled || !OS.IsDebugBuild() || GetTree().Paused) return;

        NotificationManager manager = NotificationManager.Find(this);
        if (manager == null) { GD.PushWarning("Alert preview: manager missing."); return; }

        switch (_step++ % 6)
        {
            case 0:
                manager.ShowToast("wave_ready", "WAVE READY", "Start the next wave when prepared.");
                break;
            case 1:
                manager.ShowItem("sample_scrap", "Scrap", 5);
                manager.ShowItem("sample_scrap", "Scrap", 3);
                break;
            case 2:
                manager.ShowToast("supply_warning", "AMMUNITION LOW", "Selected tower is running low.", ToastTone.Warning);
                break;
            case 3:
                manager.ShowMajor("large_wave_incoming");
                break;
            case 4:
                manager.ShowMajor("citadel_critical");
                manager.ShowMajor("boss_detected");
                break;
            case 5:
                for (int i = 0; i < 12; i++)
                    manager.ShowItem("sample_" + (i % 5), "Sample Resource " + (i % 5), 1);
                break;
        }
#endif
    }
}
