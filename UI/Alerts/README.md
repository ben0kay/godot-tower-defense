## NP2 narrator alongside NP1 wave banners (2026-10-20)

The existing `NotificationManager` still owns all major wave banners and their priority queue; no new alert engine was added. The shared `LEVELS/Waves/Presentation/WavePresentationController.cs` now also routes wave narration to the **separate** `UI/Narration/NarrationDisplay.tscn` when text is authored. It sits top-right beneath the wave HUD; on narrower screens it moves below the major alert area. No audio, countdowns or changes to ShowMajor APIs. See [Narration README](../Narration/README.md).

---

## Live wave banners — first integration (2026-10-20; runtime verification pending)

The shared `LEVELS/Waves/Presentation/WavePresentationController.cs` now requests **one transient `WAVE NN INCOMING` major alert per accepted wave start**. `NotificationManager.ShowMajor(MajorAlertDefinition)` routes these temporary titles through the existing queue, priority, dedupe and pause-aware displays, without changing the existing `ShowMajor(string id)` catalog path. The standard 3-second banner has no audio, narrator/typewriter text or pre-wave delay. See [wave presentation](../../LEVELS/Waves/Presentation/README.md). Godot runtime visual verification remains pending.

---

## Planned wave-start banners and tactical narration (2026-10-20; not implemented)

A shared presentation handoff lives at [LEVELS/Waves/Presentation/README.md](../../LEVELS/Waves/Presentation/README.md). It proposes default numeric wave banners plus optional warning/final/boss labels and a separate `UI/Narration/` typewriter panel; narration is authored alongside wave batches. **Reuse this NotificationManager's current major-alert queue/priority display** through a new validated one-off alert path; `ShowMajor(string id)` currently requires a registered catalog entry and cannot produce dynamic numeric wave titles by itself. Audio identifiers remain placeholders. No alert runtime/code changed in this documentation pass.

---

# Alerts — shared sci-fi notifications

**Foundation implemented 2026-10-17, based on `ben0kay/isometric-new-2d/UI/Notifications/`.** No gameplay events are wired by this pass. No health, ammo, wave, economy, combat, sound, research or balance code is changed.

## Purpose

One reusable event-driven UI subsystem, not separate toast scripts in every tower:

- **Toasts:** top-left, compact hexagonal slide-in notices. Up to three visible and five pending by default. Same `Key + Title + Tone` merges for visible/pending notices; quantities combine without overflow, refreshing lifetime when visible. New pending notices replace the oldest pending notice when the queue is full.
- **Major alerts:** one top-centre hexagonal banner. Pending queue is bounded, ordered by priority, with equal-priority arrival order retained. Critical alerts may interrupt lower-priority active banners. Duplicate IDs are suppressed while active/pending, with per-definition repeat cooldowns, stale-queue expiration and optional once-per-session behavior.
- Translucent dark cyan/vector frame and semantic accent tones. Tweens, queue sizes, margins and global pause policy come from one Godot Inspector resource.
- The manager is a `CanvasLayer` (layer 22) installed once by **`UI/GameplayHUD/GameplayHUD.tscn`**. Both `Main.tscn` and `MainLevel020.tscn` already instance this shared HUD, so they automatically receive the foundation; the manager does not need a global Autoload. It remains above gameplay UI and below PauseMenu (layer 100) and Results (layer 150).
- Both displays ignore mouse input; no changes to tower targeting, Ctrl tray, inspector or gameplay camera. No display element exists until requested. Only the manager clock runs when mounted; there is no world/enemy polling.

## Folder structure

```text
UI/Alerts/
  README.md
  NotificationManager.cs
  NotificationManager.tscn
  NotificationSettings.cs
  NotificationSettings.tres        # edit this resource in the Godot Inspector
  NotificationFrame.cs
  Toasts/
    ToastNotification.cs
    ToastDisplay.cs
  MajorAlerts/
    MajorAlertDefinition.cs
    MajorAlertCatalog.cs
    MajorAlertCatalog.tres
    MajorAlertDisplay.cs
    Definitions/
      CitadelCritical.tres
      LargeWaveIncoming.tres
      BossDetected.tres
  Debug/
    AlertPreview.cs                # DEBUG-only F7 examples; not gameplay triggers
```

The three sample major IDs are `citadel_critical`, `large_wave_incoming` and `boss_detected`. Their definitions are **not automatically fired by gameplay**; rename/tune/add definitions through `MajorAlerts/Definitions/` and register them in `MajorAlertCatalog.tres`.

## API for later gameplay integration

```csharp
NotificationManager alerts = NotificationManager.Find(this);
alerts?.ShowToast("ammo_low", "AMMUNITION LOW", "Battle Tower needs resupply", ToastTone.Warning);
alerts?.ShowItem("resource_scrap", "Scrap", 5); // merges identical item keys
alerts?.ShowCrafted("example_item", "Example Item", 1);
alerts?.ShowMajor("citadel_critical");
```

`NotificationManager.Find(context)` looks up the matching active viewport only. `ShowMajor(id)` returns **accepted/queued**, not proof the banner appeared: queued entries can expire or be displaced. Producers should send **state transition events** (e.g. Citadel crosses a critical threshold), not fire a warning every physics frame. Give toasts stable message/item IDs instead of relying on localized text. Normal toasts require no preconfigured catalogue entry.

## Defaults & tuning

Open `res://UI/Alerts/NotificationSettings.tres` in Godot:

| Setting | Default |
| --- | --- |
| Pause notification timers when game pauses | Yes |
| Major alerts visible / pending | 1 / 8 |
| Stale major queue lifetime; critical threshold | 20s; priority 90 |
| Major alert size, top margin | 600×128; **64px** |
| Major enter/exit | 0.34s / 0.25s |
| Toasts visible / pending | 3 / 5 |
| Toast size, top/left margins, gap | 368×76; **64px** / 18px; 10px |
| Toast duration, enter/exit | 3.2s; 0.26s / 0.22s |

64px top margins keep the overlay beneath the existing roughly 46px high TopBar, avoiding default overlap with Citadel and Credits. The major alert's own title, description, badge, priority, duration, repeat cooldown and once-per-session flag belong to its `.tres` definition, not the shared settings. Tuning takes effect on the next HUD instantiation.

## Testing and current boundaries

With a **DEBUG build** of a gameplay scene, press **F7** to cycle six harmless demonstrations: a toast, merging +5 and +3 Scrap, low-ammo warning, wave alert, critical/boss interruption, and a bounded toast burst. This uses the `AlertPreview` node already installed in the shared HUD; disable it by unchecking `Enabled` on that node. The sample data does not affect actual tower ammo, waves, wallets or Citadel health. Release builds do not register the preview input handler.

Desktop verification checklist: display both alert forms, check positions at 1920×1080 and 960×540, pause/unpause lifetime behavior, F7 duplicate merge/priority interrupt, bounded queues, scene switching/unload and absence of interference with tower selection, pause/results overlays. **Not yet executed in this GitHub-only environment:** Godot C# build, automated/headless suites or desktop rendered QA.

**Deferred:** gameplay event producers, sound/voice cues, notification history, per-profile saved discovery state, localization and player options. These should reuse this one manager rather than fork UI code.
