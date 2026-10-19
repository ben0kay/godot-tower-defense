# Shared battlefield runtime

`Battlefield.tscn` contains one copy of the camera, gameplay HUD, wallet, Citadel session, waves, placement, pause/results, mission session and F4 overlay. Existing Main/MainLevel002/MainLevel020 are tiny inherited scenes selecting one Mission resource, preserving direct F6 paths. New missions do not require either a Main launcher or a World variant.

`BattlefieldLauncher.PrepareMap` supplies the mission map to `WorldMapLoader` before child Ready callbacks. WorldMapLoader mounts it as the first child named Level. World/Level, World/Grid and all service paths stay stable. WORLD/World.tscn is the shared service composition; its default map supports existing standalone/debug consumers. WorldLevel002/020 are thin inherited compatibility aliases.

Each deployment instantiates a fresh map/session. Preparation is idempotent and may be explicitly called off-tree by fixtures before editing their private map. No runtime map hot-swapping, polling, extra gameplay manager or frame cost is introduced. Raw map F6 still lacks gameplay services; use a legacy launcher or Campaign deployment. To F6 a new mission directly, optionally create an inherited Battlefield scene and set Mission in its Inspector.
