# Music

Store global (non-spatial) title, campaign, battle and intermission music here when assets exist. Register each AudioDefinition in `AUDIO/DefaultAudioCatalog.tres` with `Category = Music`, `Mode = Loop`, `Positional = false`. Import the stream with looping enabled.

The persistent `/root/GameAudio` autoload supports `StartEmitter("music_id")` / `StopEmitter("music_id")`. Scene-driven soundtrack changes and crossfades are **future work**; do not attach a separate soundtrack player to every menu scene.
