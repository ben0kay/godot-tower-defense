# Boot — Local Profile Picker (implemented)

`BootScreen.tscn` is the normal project startup scene. `BootScreen.cs` builds the heading, scrollable saved-profile list, **New Profile** modal and Exit button using `UIButtonFactory` / `MenuLayout`.

Selecting a saved profile calls `ProfileStore.Select(id)` then opens `MainMenu`. Creating one validates the name and writes `user://Profiles/profiles.json` before navigating. Failed validation and I/O errors are shown without inventing a profile. No profile is automatically selected at startup; switching from Main returns here. Reads display backup recovery notices.

Data model and disk recovery live in `SYSTEMS/Profiles/`, not this scene. This is a profile selector, **not** an account login or campaign load screen. No gameplay World/HUD nodes are loaded until the player enters Level001.
