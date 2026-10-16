// Source-only work state for inspection and future save integration; no persistence or derived stats.
public enum TowerWorkPhase { Operational, Constructing, Upgrading }

public readonly record struct TowerWorkState(TowerWorkPhase Phase, double ElapsedSeconds, double TotalSeconds, int PendingUpgradeLevel);
