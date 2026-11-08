# Camera Effects

Each battlefield owns a `CameraShakeManager`. Future effects can cache that instance during setup and call `RequestShake(worldPosition, intensity, duration, radius)`. Intensity uses screen pixels, duration uses seconds and radius uses world pixels beyond the visible camera rectangle. Omitted arguments use the shared `DefaultCameraShakeSettings.tres`.

On-screen hits get full strength. Offscreen hits fade to zero over 400 world pixels by default. Within 80 ms of the first accepted request, only a strictly stronger attenuated hit replaces it. One damped offset is capped at 8 screen pixels, and the manager sleeps after the shake. Pause, mission outcome, checkpoint restore and unload clear the offset.

Citadel arrival damage is the sole trigger. Tune its `Impact Shake` group on the battlefield's `CitadelSession`. The defaults scale from 1.5 px / 0.12 s at 1 HP lost to 6 px / 0.22 s at 20 HP lost.
