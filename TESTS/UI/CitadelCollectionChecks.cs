// CP1B integration checks: world-footprint and top-centre HUD clicks collect one objective's CreditBuffer.
using Godot;
using System;

public partial class CitadelCollectionChecks : Node
{
    // =========================================================
    // Exercise actual Main UI controls and the real placement click handler without editing playable maps.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            Single();
            foreach (int count in new[] { 2, 4 }) Multi(count);
            GD.Print("PASS: CP1B world and HUD collection, one/two/four Citadels, wallet cap and visible buffers");
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GetTree().Quit(0);
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // The untouched legacy health label/bar remain functional alongside new Credits widgets.
    private void Single()
    {
        using MissionFixture fixture = new(this);
        CitadelSession session = fixture.Main.GetNode<CitadelSession>("CitadelSession");
        TowerPlacementController placement = fixture.Main.GetNode<TowerPlacementController>("TowerPlacement");
        GameplayHUD hud = fixture.Hud;
        EconomyManager wallet = fixture.Main.GetNode<EconomyManager>("Economy");
        var destination = session.Objectives[0];
        var buffer = session.CreditsFor(destination);
        var panel = hud.GetNode<Control>("TopBar/Status/Citadel");
        var label = panel.GetNode<Label>("Credits");
        var bar = panel.GetNode<ProgressBar>("CreditBar");
        var health = panel.GetNode<Label>("Health");
        var world = destination.Root.GetNode<CitadelCreditDisplay>("CreditDisplay");
        var worldText = world.GetNode<Label>("Amount");
        var worldBar = world.GetNode<ProgressBar>("Bar");
        Require(panel.Visible && hud.GetNodeOrNull("TopBar/Status/CitadelRow") == null
            && panel.MouseFilter == Control.MouseFilterEnum.Stop, "Single-objective health panel remains intact and clickable");
        buffer.Produce(30);
        Require(label.Text.Contains("30 / 80") && worldText.Text.Contains("30 / 80")
            && Math.Abs(bar.Value - 37.5) < .01 && Math.Abs(worldBar.Value - 37.5) < .01,
            "One production event updates world and HUD green meters, not just wallet");
        Require(health.Text.Contains("CITADEL"), "Existing health label still shows current HP");
        int before = wallet.GetBalance(wallet.CreditsResource);
        var cell = fixture.Main.GetNode<WorldGrid>("World/Grid").WorldToCell(destination.Root.GlobalPosition);
        Require(placement.TryCollectCitadelAt(cell) && buffer.Current == 0
            && wallet.GetBalance(wallet.CreditsResource) == before + 30,
            "Clicking real Citadel footprint grants exactly its stored amount");
        Require(worldText.Text.Contains("0 / 80") && label.Text.Contains("0 / 80"),
            "Collection immediately empties both presentations");
        int after = wallet.GetBalance(wallet.CreditsResource);
        Click(panel);
        Require(wallet.GetBalance(wallet.CreditsResource) == after && buffer.Current == 0,
            "Clicking HUD after world collection never pays twice");
        buffer.Produce(20);
        Click(panel);
        Require(wallet.GetBalance(wallet.CreditsResource) == after + 20 && buffer.Current == 0,
            "Clicking legacy top-centre health cell collects the same buffer");
        before = wallet.GetBalance(wallet.CreditsResource);
        buffer.Produce(40);
        var old = wallet.Config;
        wallet.Config = (EconomyMasterConfig)old.Duplicate(true);
        wallet.Config.MaxBalancePerResource = before + 10;
        Click(panel);
        Require(buffer.Current == 40 && wallet.GetBalance(wallet.CreditsResource) == before,
            "Rejected collection retains stored Credits and never fakes wallet gain");
        wallet.Config = old;
        Click(panel);
        Require(buffer.Current == 0 && wallet.GetBalance(wallet.CreditsResource) == before + 40,
            "Retry after wallet cap is removed transfers pending Credits exactly once");
        Require(!placement.TryCollectCitadelAt(cell + new Vector2I(8,8)), "No invisible large world click region");
    }

    // =========================================================
    // Real two/four-Citadel HUD cells keep matching objective identities through rebind and clicks.
    private void Multi(int count)
    {
        using MultiCitadelChecks.Fixture fixture = new(this, citadelCount: count);
        var session = fixture.Session;
        var hud = fixture.Main.GetNode<GameplayHUD>("UI/GameplayHUD");
        var wallet = fixture.Main.GetNode<EconomyManager>("Economy");
        var placement = fixture.Main.GetNode<TowerPlacementController>("TowerPlacement");
        var row = hud.GetNode<CitadelHudRow>("TopBar/Status/CitadelRow");
        Require(row.GetChildCount() == count && !hud.GetNode<Control>("TopBar/Status/Citadel").Visible,
            "Single horizontal multi-Citadel health presentation retained");
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            var model = session.CreditsFor(session.Objectives[i]);
            model.Produce((i + 1) * 10); sum += (i + 1) * 10;
            var cell = row.GetChild<Control>(i);
            Require(cell.MouseFilter == Control.MouseFilterEnum.Stop && cell.GetNode<Label>("Credits").Text.Contains($"{(i+1)*10} / 80")
                && cell.GetNode<ProgressBar>("CreditBar").Value > 0 && cell.GetNode<Label>("Name").Text == $"CITADEL {i+1}",
                "Each objective gets its own click-ready green buffer beside original health meter");
            var world = session.Objectives[i].Root.GetNode<CitadelCreditDisplay>("CreditDisplay");
            Require(world.GetNode<Label>("Amount").Text.Contains($"{(i+1)*10} / 80"),
                "Each physical Citadel has its own independent world-space indicator");
        }
        int before = wallet.GetBalance(wallet.CreditsResource);
        Click(row.GetChild<Control>(1));
        Require(session.CreditsFor(session.Objectives[1]).Current == 0 && session.CreditsFor(session.Objectives[0]).Current == 10
            && wallet.GetBalance(wallet.CreditsResource) == before + 20,
            "HUD Citadel 2 collects only second buffer; first retains Credits");
        var firstCell = fixture.Grid.WorldToCell(session.Objectives[0].Root.GlobalPosition);
        Require(placement.TryCollectCitadelAt(firstCell) && session.CreditsFor(session.Objectives[0]).Current == 0
            && wallet.GetBalance(wallet.CreditsResource) == before + 30,
            "Click physical Citadel 1 to collect only that stored buffer");
        Click(row.GetChild<Control>(1));
        Require(wallet.GetBalance(wallet.CreditsResource) == before + 30, "Repeated HUD press cannot double pay");
        hud.BindCitadels(session.Objectives);
        var rebound = hud.GetNode<CitadelHudRow>("TopBar/Status/CitadelRow");
        Require(rebound != row && rebound.GetChildCount() == count,
            "HUD rebind replaces old cells without losing session/buffer ownership");
        int beforeLast = session.CreditsFor(session.Objectives[count-1]).Current;
        session.CreditsFor(session.Objectives[count-1]).Produce(10);
        var last = rebound.GetChild<Control>(count-1);
        Require(last.GetNode<Label>("Credits").Text.Contains($"{beforeLast+10} / 80"),
            "Rebound HUD reflects continuing authoritative model events");
        int lastValue = session.CreditsFor(session.Objectives[count-1]).Current;
        Click(last);
        Require(wallet.GetBalance(wallet.CreditsResource) == before + 30 + lastValue
            && session.CreditsFor(session.Objectives[count-1]).Current == 0,
            "Last Citadel remains independently collectable after HUD rebind");
        Require(session.CreditsFor(session.Objectives[1]).Current == 0,
            "Collecting another cell never refills or drains previously collected objectives");
    }

    // =========================================================
    // Emit the actual Godot GuiInput signal that production cells subscribe to.
    private static void Click(Control control) => control.EmitSignal(Control.SignalName.GuiInput,
        new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });

    // =========================================================
    // Fail this standalone runner immediately on incorrect rewards or presentation.
    private static void Require(bool condition, string description)
    { if (!condition) throw new InvalidOperationException(description); }
}
