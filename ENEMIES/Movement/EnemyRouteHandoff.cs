// Immutable destination obligations only; children retain independent physical paths and movement permissions.
public sealed record EnemyRouteHandoff(RouteCheckpoint Checkpoint, bool Required, bool Complete, int CompletedCount,
    CheckpointNetwork Network, string Error, CitadelDestination Destination = null);
