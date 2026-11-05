// Compact power status formatting from a typed runtime snapshot; no scene queries or resource mutation.
public static class PowerInspectionText
{
    // =========================================================
    // Received/possible are different quantities; consumption is measured paid energy, not nominal active draw.
    public static string Details(PowerInspectionSnapshot value)
    {
        string distance = value.Distance >= 0 ? $"{value.Distance} cells" : "no route";
        return $"Refill +{value.Received:0.#}/s · Limit +{value.Possible:0.#}/s\n"
            + $"Use −{value.Consumed:0.#}/s · {distance} · {value.Efficiency * 100:0}%\n"
            + Status(value.State) + (value.Recovering ? " · RECOVERING" : "");
    }
    public static string Tooltip(PowerInspectionSnapshot value) =>
        "Refill: actual energy received per second in the latest shared service sample.\n"
        + "Limit: maximum distance-adjusted recharge, before island shortages and buffer headroom.\n"
        + "Use: actual paid attack energy plus idle losses per second in that sample.\n"
        + $"Authored draw: idle {value.IdleDraw:0.##}/s; active {value.ActiveDraw:0.##}/s total (not added to idle).\n"
        + "Distance follows conductive tiles to the nearest live source. Connected does not guarantee supply.\n"
        + $"Recovery needs {value.RestartEnergy:0.##} stored energy. No incoming refill does not erase reserves.\n"
        + "Rates settle on the shared service cadence and freeze during pause.";
    private static string Status(PowerDeliveryState state) => state switch
    {
        PowerDeliveryState.Disconnected => "DISCONNECTED / NO LIVE SOURCE",
        PowerDeliveryState.PausedForWork => "RECHARGE PAUSED FOR WORK",
        PowerDeliveryState.Full => "CONNECTED · BUFFER FULL",
        PowerDeliveryState.Starved => "CONNECTED · STARVED",
        PowerDeliveryState.Limited => "CONNECTED · SUPPLY LIMITED",
        PowerDeliveryState.NoRecharge => "CONNECTED · RECHARGE DISABLED",
        PowerDeliveryState.Charging => "CONNECTED · CHARGING",
        _ => "CONNECTED · AWAITING REFILL"
    };
}
