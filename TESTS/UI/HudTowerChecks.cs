// Integration coverage for display-only HUD status and opt-in weapon aiming.
using Godot;
using System;

public static class HudTowerChecks
{
    // =========================================================
    // Exercise production scene paths, status APIs, independent panels and shortest-angle aiming.
    public static void Run(Node battle)
    {
        GameplayHUD hud = FindHud(battle);
        Label credits = hud.GetNode<Label>("TopBar/Status/Credits");
        CreditGainIndicator reward = hud.GetNode<CreditGainIndicator>("TopBar/Status/CreditGain");
        Label wave = hud.GetNode<Label>("TopBar/Status/Wave");
        Label health = hud.GetNode<Label>("TopBar/Status/Citadel/Health");
        ProgressBar bar = hud.GetNode<ProgressBar>("TopBar/Status/Citadel/Bar");
        CitadelHealth citadel = battle.GetNode<CitadelSession>("CitadelSession").Health;
        Require(hud.TargetCredits == battle.GetNode<EconomyManager>("Economy").GetBalance(battle.GetNode<EconomyManager>("Economy").CreditsResource) &&
            credits.Text.StartsWith("Credits: ") && wave.Text == "WAVE 00 / 03" &&
            health.Text == $"CITADEL {citadel.CurrentHealth:N0} / {citadel.MaxHealth:N0}" && bar.Visible,
            "HUD shows authoritative live credits and Citadel HP with prepared wave state");
        hud.SetCredits(12345678, true); hud.SetWave(100); hud.SetCitadelHealth(800, 1000, false);
        Require(credits.Text == $"Credits: {12345678:N0}" && wave.Text == "WAVE 100" && bar.Value == 80,
            "Status setters update their respective zones");
        hud.SetCredits(100, true); hud.SetCredits(300);
        Require(hud.TargetCredits == 300 && credits.Text == "Credits: 100",
            "Wallet target updates instantly while the visible credit number is still easing");
        hud._Process(hud.Config.CreditCountSeconds + 0.02);
        Require(credits.Text == "Credits: 300", "Credit balance animation converges to its exact target");
        hud.SetCredits(12345678, true);
        hud.SetWave(101);
        Require(wave.Scale.X > 1f, "A newly advanced wave pulses the number without affecting wave state");

        foreach (int width in new[] {1920, 1366, 640})
        {
            Control status = hud.GetNode<Control>("TopBar/Status");
            status.Size = new Vector2(width, 46);
            Control center = status.GetNode<Control>("Citadel");
            Require(Mathf.IsEqualApprox(center.Position.X + center.Size.X * 0.5f, width * 0.5f),
                "Citadel stays at true center");
            Require(credits.Position.X + credits.Size.X <= center.Position.X &&
                wave.Position.X >= center.Position.X + center.Size.X, "Status zones cannot overlap");
        }
        // Cosmetic feedback does not alter the authoritative credit balance.
        hud.ResetCreditReward();
        hud.ShowCreditReward(5); hud.ShowCreditReward(5);
        Require(reward.Visible && reward.AccumulatedReward == 10 && reward.Text == "(+10)" && reward.Scale.X > 1f,
            "Back-to-back gains stack into one top-left popup");
        reward._Process(hud.Config.CreditRewardStackSeconds + 0.01);
        hud.ShowCreditReward(5);
        Require(reward.AccumulatedReward == 5 && reward.Text == "(+5)",
            "A quiet interval starts a fresh credit gain stack");
        reward._Process(hud.Config.CreditRewardStackSeconds + hud.Config.CreditRewardFadeSeconds + 0.01);
        Require(!reward.Visible && reward.AccumulatedReward == 0,
            "Credit gain popup fades, hides and stops retaining an expired stack");
        hud.ResetCreditReward();
        hud.ShowCreditExpense(200);
        Require(reward.Visible && reward.Text == "(-200)" && reward.IsExpense,
            "Successful spend feedback uses one amber negative popup");
        hud.ResetCreditReward();

        hud.SetCoreHealth(-10, 100);
        Require(bar.Value == 0 && ((StyleBoxFlat)bar.GetThemeStylebox("fill")).BgColor == hud.Config.HealthCriticalColor,
            "Compatibility API clamps health and applies critical color");
        hud.SetCitadelHealth(50, 100, false);
        Require(((StyleBoxFlat)bar.GetThemeStylebox("fill")).BgColor == hud.Config.HealthWarningColor, "Warning threshold is inclusive");
        hud.SetCitadelHealth(200, 100, false); Require(bar.Value == 100, "Over-healing clamps fill");
        hud.SetCitadelHealth(0, 0, false); Require(!bar.Visible && health.Text == "CITADEL --", "Missing maximum is safe");
        hud.SetCitadelHealth(citadel.CurrentHealth, citadel.MaxHealth, false); // Restore live display after synthetic setter tests.
        hud.ShowTowerDetails("Test", "Offensive", "Test");
        hud.SetTowerBarOpen(true, true); hud.SetTowerBarOpen(false, true);
        Require(hud.GetNode<Control>("BottomArea/Inspector").Visible, "Tray close preserves Inspector");
        Node2D root = new(); Node2D pivot = new(); root.AddChild(pivot);
        pivot.Rotation = Mathf.DegToRad(179);
        Vector2 target = Vector2.FromAngle(Mathf.DegToRad(-179) - Mathf.Pi * 0.5f) * 100;
        bool aligned = TurretAiming.Track(pivot, target, 60, 0.1f, true, 1.0 / 60);
        Require(!aligned && Mathf.Abs(Mathf.AngleDifference(Mathf.DegToRad(179), pivot.Rotation)) <= Mathf.DegToRad(1.01f),
            "Aiming respects speed cap across angle wrap");
        Require(TurretAiming.Track(pivot, target, 60, 0.1f, true, 1.0 / 60), "Aiming takes shortest arc");
        Require(root.Rotation == 0, "Only turret pivot rotates");
        pivot.Rotation = 0;
        Require(TurretAiming.Track(pivot, new Vector2(0, -100), 180, 12, false, 1), "Up-facing art aligns at rest");
        Require(!TurretAiming.Track(pivot, new Vector2(100, 0), 180, 12, false, 1) && pivot.Rotation == 0,
            "Disabled tracking retains orientation and prevents unaligned fire");
        root.Free();
        GD.Print("PASS: HUD status layout/API and turret aiming checks");
    }

    // =========================================================
    // Find the existing HUD without depending on its CanvasLayer name.
    private static GameplayHUD FindHud(Node node)
    {
        if (node is GameplayHUD hud) return hud;
        foreach (Node child in node.GetChildren()) { GameplayHUD result = FindHud(child); if (result != null) return result; }
        return null;
    }

    // =========================================================
    // Fail the headless integration run on a behavior regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
