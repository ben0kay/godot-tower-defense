// Read-only summary for the throttled development overlay. No enemy scans or gameplay callbacks.
public partial class WorldLightingController
{
    internal string DebugVisualSummary()
    {
        int enabled = 0;
        foreach (Slot slot in _slots)
            if (slot.Source != null && slot.Light.Enabled) enabled++;
        return $"Lighting {(_lightingEnabled ? "on" : "off")} · fog contacts {_darkContacts.Count}/{_enemyAlpha.Count} tracked · lights {enabled}/{_slots.Count} pooled · shot leases {_projectileSources.Count}";
    }
}
