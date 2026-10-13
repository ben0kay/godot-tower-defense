# Top Bar

Thin, always-visible battlefield status bar. `TopBar.tscn` defines the control layout and named Wave, Credits and Core labels.

The `GameplayHUD.cs` API will receive updates from future wave, economy and objective managers. Currently labels show `--`, not fabricated live values. Layout colors and height are shared UI master settings. Do not put combat/economy simulation in this UI scene.
