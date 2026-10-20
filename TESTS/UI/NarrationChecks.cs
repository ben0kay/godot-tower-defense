// Focused NP2 UI checks for the live shared-HUD narrator; no enemy scheduling mutations.
using Godot;
using System;
using System.Linq;

public static class NarrationChecks
{
    // =========================================================
    // Assert initialization, typing, replacement, opt-out and active-only processing using actual scene nodes.
    public static void Run(Node battlefield)
    {
        NarrationDisplay display = battlefield.GetNode<NarrationDisplay>("UI/GameplayHUD/NarrationDisplay");
        Require(display != null && !display.IsActive && !display.IsProcessing(),
            "Shared narrator is initially hidden and consumes no idle frames");
        Require(display.Config != null && display.Config.TopMargin >= 48 && display.Config.RightMargin >= 0,
            "Inspector config defaults to the top-right below the top status bar");
        display.ShowMessage("Commander, prepare your defenses.", "TACTICAL COMMAND");
        Require(display.IsActive && display.CurrentText == "Commander, prepare your defenses."
            && display.RevealedCharacters == 0 && display.IsProcessing(),
            "A transmission starts hidden-character typewriting in the existing shared scene");
        display._Process(.3);
        display._Process(.5);
        Require(display.RevealedCharacters > 0 && display.RevealedCharacters < display.CurrentText.Length,
            "Active text progressively reveals instead of immediately appearing");
        display.FinishTyping();
        Require(display.RevealedCharacters == display.CurrentText.Length,
            "FinishTyping reveals the complete message without changing wave state");
        display.ShowMessage("Second transmission", "COMMAND");
        Require(display.CurrentText == "Second transmission" && display.RevealedCharacters == 0,
            "New messages replace the previous transmission rather than stacking nodes");
        display.ShowMessage("   ");
        Require(!display.IsActive && !display.IsProcessing(), "Missing text immediately clears and sleeps");

        WaveDefinition[] waves = Level020Waves.Create();
        Require(waves.Length == 7 && waves.Where(w => !string.IsNullOrWhiteSpace(w.Presentation?.Text))
            .Select(w => w.Number).SequenceEqual(new[] { 1, 4, 6, 7 }),
            "Only Level020 waves 1,4,6,7 author optional tactical messages");
        Require(waves[3].Presentation.Alert == WaveAlertKind.Heavy
            && waves[5].Presentation.Alert == WaveAlertKind.Heavy
            && waves[6].Presentation.Alert == WaveAlertKind.Final
            && waves[6].Presentation.VoiceCueId == "level020_wave07",
            "Escalating alert types and future voice cue IDs are authored but not played");
        Require(Level001Waves.Create().All(w => w.Presentation == null)
            && Level002Waves.Create().All(w => w.Presentation == null),
            "Existing test/tutorial levels retain their standard NP1 banner with no narrator");
        GD.Print("PASS: NP2 narrator typewriter, top-right config, message replacement and Level020 opt-in authoring");
    }

    // =========================================================
    private static void Require(bool result, string message)
    { if (!result) throw new InvalidOperationException(message); }
}
