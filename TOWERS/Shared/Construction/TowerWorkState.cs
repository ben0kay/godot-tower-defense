// Source-only paid construction/upgrade state, persisted by tower checkpoints without derived stats.
public enum TowerWorkPhase { Operational, Constructing, Upgrading }

public readonly record struct TowerWorkState(TowerWorkPhase Phase, double ElapsedSeconds, double TotalSeconds, int PendingUpgradeLevel);
