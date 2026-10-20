// Compact world-owned hostile shot state; no shooter/target node reference, resource mutation or per-bullet scene object.
using Godot;

public struct EnemyBulletData
{
    public Vector2 Position, Direction;
    public float Speed;
    public double RemainingSeconds;
    public DamageInfo Hit;
    public ProjectileVisualSnapshot Visual;
}
