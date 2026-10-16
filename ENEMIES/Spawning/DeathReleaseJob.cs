// One bounded release snapshot, independent of the freed parent; only its cursor/countdown advance.
using Godot;

internal sealed class DeathReleaseJob
{
    internal readonly Vector2 Position;
    internal readonly PackedScene Scene;
    internal readonly EnemyDefinition Definition;
    internal readonly int Count, Attempts, Generation;
    internal readonly float Interval, Radius;
    internal readonly bool Shield;
    internal readonly uint Seed;
    internal readonly ulong FirstPhysicsFrame;
    internal readonly EnemyRouteHandoff Route;
    internal readonly DeathSpawnReservation Reservation;
    internal bool Cancelled;
    internal int Index;
    internal double Delay;
    internal DeathReleaseJob(Enemy parent, DeathSpawnConfig config, PackedScene scene, EnemyDefinition definition, uint seed, DeathSpawnReservation reservation)
    {
        FirstPhysicsFrame = Engine.GetPhysicsFrames() + 1;
        Position = parent.GlobalPosition; Scene = scene; Definition = definition; Count = config.ChildCount;
        Attempts = config.MaxPositionAttemptsPerChild; Interval = config.SpawnIntervalSeconds; Radius = config.ScatterRadiusPixels;
        Shield = config.SpawnWithShield; Seed = seed; Generation = parent.SpawnGeneration + 1; Reservation = reservation;
        Route = config.InheritCheckpointProgress ? parent.Movement.CaptureRouteHandoff() : null;
    }
}
