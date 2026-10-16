# World routing markers

Freely positioned level guidance nodes live here. They are not terrain tiles, structures or build purchases. They have no WorldObjectFootprint, collision areas, cell reservations or per-node processing.

`RouteCheckpoint/` implements optional portal-linked **required circular regions**. Unlinked portals and map-edge sources retain ordinary Citadel routing. See its README for authoring and restrictions.

`CheckpointDesignOverlay.cs` is the shared editor/F3 diagram owner. Existing maps/templates instance it under Debug. Required checkpoints support acyclic chains, equal-probability branches and merges through Next Checkpoints; their scene labels do not drive routing. See RouteCheckpoint/README.md and TESTS/CheckpointChains.md.
