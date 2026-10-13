# CAMERA

Independent RTS-style 2D camera, without a player character.

- `CameraController.cs`: pan, drag, cursor-centered zoom, smooth motion and zoom-aware world boundaries.
- `CameraMasterConfig.cs` and `DefaultCameraConfig.tres`: grouped camera values, editable in Godot's Inspector.
- `CameraRig.tscn`: reusable scene with a Camera2D.

**Keys:** WASD or arrows to move, middle-mouse drag to pan, wheel to zoom. The camera starts at map center.

The rig obtains the map bounds from `../World/Grid` in `Main.tscn`. If scenes are reorganized, update that reference. Keep hits, shakes and other combat visuals in `FEEDBACK/CameraEffects`, not in the movement controller.
