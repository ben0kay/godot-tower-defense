// Immutable launch request shared by enemy weapons and centralized hostile delivery; retains no enemy/tower nodes.
using Godot;

public readonly record struct EnemyShotRequest(Vector2 Origin, Vector2 Direction, float Damage, float Speed,
    double LifetimeSeconds = 0, ProjectileVisualSnapshot? Visual = null);
