// Per-battlefield visual selection. Existing render surfaces redraw only when the mode changes.
using System;

internal sealed class ProjectileRenderSettings
{
    internal bool TexturedTracers { get; private set; }
    internal event Action Changed;

    internal void SetTexturedTracers(bool enabled)
    {
        if (TexturedTracers == enabled) return;
        TexturedTracers = enabled;
        Changed?.Invoke();
    }
}
