# Level deployment loading screen

Campaign PLAY LEVEL buttons route through `MenuNavigation.OpenLevel` into
`LevelLoadingScreen.tscn` before the mission opens. It requests the mission
resource and shared Battlefield PackedScene using Godot's threaded loader;
the cyan bottom rail and circular percentage reflect **actual asset-load status**
rather than a simulated timer. A short 0.6-second minimum keeps very fast loads
visible. After resources load, the battlefield is instantiated on the main
thread and its existing preparation sequence takes over; that synchronous
final initialization is **not** claimed as independently measured progress.

Presentation: one canvas, bottom segmented cyan bar, animated counter-rotating
rings/particles and numeric percent near the lower-right. The middle of the
screen is deliberately clear for `Artwork/LoadingBackdrop.png`; an optional
full-screen image is center-cropped and darkened for UI readability. No image is
required to run the game. Change `MinimumDisplaySeconds`, `BackdropPath`
and `ArtworkDarken` on the scene root in the Inspector.

The regular `MenuNavigation.Open` remains unchanged for direct F6 test launch,
saved checkpoint Continue and existing synchronous navigation fixtures.
Campaign and Levels PLAY buttons alone use the new loading transition.
On threaded-load or mission errors, the loader shows a failure message and
a BACK TO LEVELS button; it never displays a permanent loading spinner.
The scene frees with its loading visuals after launch, leaving no per-frame
gameplay work. Owner Godot menu, layout and level-launch visual checks pending.
