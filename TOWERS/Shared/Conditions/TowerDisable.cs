// Reasons are extensible; a handle identifies one application, not every effect sharing that reason.
public enum TowerDisableReason { Overheated, EMP, NoPower, Other, Upgrading }
public readonly record struct TowerDisableHandle(ulong TowerId, long Token);
