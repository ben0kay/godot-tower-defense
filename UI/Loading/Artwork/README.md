# Optional loading artwork

Drop a 1920×1080 or higher, preferably 16:9 image at
`res://UI/Loading/Artwork/LoadingBackdrop.png`.
The loading screen checks for this file at runtime and center-crops it to fill
the screen. The project works without it and displays a quiet dark fallback.
You can change the name/path through the root LevelLoadingScreen scene's
`BackdropPath` export. No code edits are needed to supply an image.
