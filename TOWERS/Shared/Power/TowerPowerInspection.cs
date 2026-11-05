// Derived rate samples and typed Inspector state; no UI ownership, saved state, or independent clock.
using System;

public enum PowerDeliveryState { Disconnected, PausedForWork, Full, Waiting, Charging, Limited, Starved, NoRecharge }
public readonly record struct PowerInspectionSnapshot(double Received, double Possible, double Consumed,
    int Distance, double Efficiency, PowerDeliveryState State, bool Recovering, double RestartEnergy,
    double IdleDraw, double ActiveDraw);

public sealed partial class TowerPowerConsumer
{
    private double _energySpent, _lastRequested, _lastCredited, _publishedConsumed = double.NaN;
    private bool _hasInspectionSample;
    private PowerDeliveryState _publishedState;
    public double LastConsumedPerSecond { get; private set; }

    // =========================================================
    // Successful funded shots and actual settled idle losses contribute once; rejected ammo is excluded.
    private void RecordEnergySpent(double amount) => _energySpent += Math.Max(0, amount);
    private void CompleteInspectionSample(double credited, double seconds)
    {
        LastConsumedPerSecond = seconds > 0 ? _energySpent / seconds : 0;
        _energySpent = 0; _lastRequested = DesiredRefill; _lastCredited = credited; _hasInspectionSample = true;
    }
    private void ResetInspectionSample()
    {
        _energySpent = _lastRequested = _lastCredited = LastConsumedPerSecond = 0;
        _hasInspectionSample = false;
    }

    // =========================================================
    // Selected-only reads use cached delivery; no settling, charging, source enumeration or UI-time simulation.
    public PowerInspectionSnapshot Inspect()
    {
        RefreshDelivery();
        PowerDeliveryState state = InspectionState();
        double received = state is PowerDeliveryState.Disconnected or PowerDeliveryState.PausedForWork
            or PowerDeliveryState.Full or PowerDeliveryState.NoRecharge || !_hasInspectionSample ? 0 : LastDeliveredRefillPerSecond;
        var settings = Capacitor.Settings;
        return new(received, settings.Recharge * _efficiency, LastConsumedPerSecond,
            _distance, _efficiency, state, Capacitor.RecoveryRequired,
            Math.Max(settings.Capacity * settings.Restart, Capacitor.RestartEnergy), settings.Idle, settings.Active);
    }

    // =========================================================
    // A positive route and ceiling can still deliver zero; full/work states are not labelled starved.
    private PowerDeliveryState InspectionState()
    {
        if (!_chargeAllowed) return PowerDeliveryState.PausedForWork;
        if (Region <= 0 || _distance < 0) return PowerDeliveryState.Disconnected;
        if (Capacitor.Energy >= Capacitor.Settings.Capacity) return PowerDeliveryState.Full;
        if (Capacitor.Settings.Recharge * _efficiency <= 0) return PowerDeliveryState.NoRecharge;
        if (!_hasInspectionSample || _lastRequested <= 0) return PowerDeliveryState.Waiting;
        if (_lastCredited <= 0) return PowerDeliveryState.Starved;
        if (_lastCredited + 1e-9 < _lastRequested) return PowerDeliveryState.Limited;
        return PowerDeliveryState.Charging;
    }
}
