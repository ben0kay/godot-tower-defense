// CP1A headless checks: reusable buffers, real wave gating, independent Citadels and safe wallet collection.
using Godot;
using System;
using static WaveBuilder;

public partial class CitadelCreditChecks : Node
{
    // =========================================================
    // Standalone runner leaves gameplay maps untouched and never exercises CP1B click/UI surfaces.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            BufferAndTimer();
            SingleCitadel();
            MultipleCitadels();
            GD.Print("PASS: CP1A credit buffers, wave gating, pause, wallets, one/two Citadels and reset");
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Paused = false;
            GetTree().Quit(1);
        }
    }

    // =========================================================
    // Plain helper works without a scene, emits only on mutations and never banks inactive time.
    private static void BufferAndTimer()
    {
        CreditBuffer buffer = new(80);
        CreditProductionTimer clock = new(10, 2);
        int notifications = 0;
        buffer.Changed += (_, _) => notifications++;
        Require(buffer.Current == 0 && buffer.Capacity == 80, "New buffer starts empty");
        clock.Advance(8, false, buffer);
        Require(buffer.Current == 0 && clock.ElapsedSeconds == 0, "Inactive wave cannot generate or bank time");
        clock.Advance(1.5, true, buffer);
        Require(buffer.Current == 0 && notifications == 0, "Partial seconds produce nothing");
        clock.Advance(.5, true, buffer);
        Require(buffer.Current == 10 && notifications == 1, "Two seconds produce ten Credits");
        clock.Advance(15, true, buffer);
        Require(buffer.Current == 80 && notifications == 2, "Production batches and clamps at capacity");
        clock.Advance(100, true, buffer);
        Require(buffer.Current == 80 && clock.ElapsedSeconds == 0, "Full buffer cannot accumulate a future burst");
        Require(!buffer.TryCollect(null, null), "Null wallet cannot consume Credits");
        Require(buffer.Produce(999) == 0, "Full buffer refuses additional Credits");
        buffer.Reset(); clock.Reset();
        Require(buffer.Current == 0 && notifications == 3, "Reset publishes an empty buffer");
        clock.Advance(1, true, buffer); clock.Advance(99, false, buffer); clock.Advance(1, true, buffer);
        Require(buffer.Current == 10, "Between-wave pause freezes fractional production progress");
    }

    // =========================================================
    // Real Main's wallet, CitadelSession and WaveManager share one authoritative collection path.
    private void SingleCitadel()
    {
        using MissionFixture fixture = new(this);
        CitadelSession citadels = fixture.Main.GetNode<CitadelSession>("CitadelSession");
        EconomyManager wallet = fixture.Main.GetNode<EconomyManager>("Economy");
        WaveManager waves = fixture.Waves;
        CitadelDestination goal = citadels.Objectives[0];
        CreditBuffer buffer = citadels.CreditsFor(goal);
        Require(citadels.Objectives.Count == 1 && buffer != null && buffer.Capacity == 80
            && citadels.CreditsPerCycle == 10 && citadels.CreditCycleSeconds == 2, "Real scene uses old coded 80/10/2 defaults");
        Require(!citadels.IsPhysicsProcessing(), "Idle Citadel clock sleeps without wave activity");
        citadels._PhysicsProcess(100);
        Require(buffer.Current == 0 && !citadels.TryCollectCredits(goal), "No wave means no income or empty collection");
        Enemy owned = fixture.Start();
        Require(waves.State is WaveManager.WaveState.Spawning or WaveManager.WaveState.WaitingForClear,
            "Real wave start enables production");
        Require(citadels.IsPhysicsProcessing(), "Wave event wakes Citadel clock");
        citadels._PhysicsProcess(1);
        Require(buffer.Current == 0, "Production respects actual seconds");
        GetTree().Paused = true;
        citadels._PhysicsProcess(20);
        Require(buffer.Current == 0, "SceneTree pause freezes income even when called explicitly");
        GetTree().Paused = false;
        citadels._PhysicsProcess(1);
        Require(buffer.Current == 10, "Resume preserves previous partial wave second");
        citadels._PhysicsProcess(14);
        Require(buffer.Current == 80, "Real Citadel fills to eighty Credits");
        int before = wallet.GetBalance(wallet.CreditsResource);
        EconomyMasterConfig original = wallet.Config;
        wallet.Config = (EconomyMasterConfig)wallet.Config.Duplicate(true);
        wallet.Config.MaxBalancePerResource = before + 40;
        Require(!citadels.TryCollectCredits(goal) && buffer.Current == 80
            && wallet.GetBalance(wallet.CreditsResource) == before, "Rejected wallet cap leaves buffer untouched");
        wallet.Config.MaxBalancePerResource = 1000000000;
        bool nested = true;
        void Reenter(string id, int value) { if (id == wallet.CreditsResource.ResourceId) nested = citadels.TryCollectCredits(goal); }
        wallet.BalanceChanged += Reenter;
        Require(citadels.TryCollectCredits(goal), "Real wallet accepts Citadel collection");
        wallet.BalanceChanged -= Reenter;
        Require(!nested && buffer.Current == 0 && wallet.GetBalance(wallet.CreditsResource) == before + 80
            && !citadels.TryCollectCredits(goal), "Only one grant occurs even during nested callbacks or double click");
        wallet.Config = original;
        citadels._PhysicsProcess(2);
        Require(buffer.Current == 10, "Collection restarts production at next elapsed cycle");
        owned.TakeDamage(99999);
        Require(waves.State == WaveManager.WaveState.AllWavesComplete && !citadels.IsPhysicsProcessing(),
            "Completing the final wave stops production immediately");
        citadels._PhysicsProcess(100);
        Require(buffer.Current == 10, "Stored Credits remain after wave completion; no extra generation");
        Require(citadels.TryCollectCredits(goal) && buffer.Current == 0,
            "Completed-wave Credits remain collectible before result transition");
    }

    // =========================================================
    // Two genuinely distinct authored Citadels each own their own independent balance and timer.
    private void MultipleCitadels()
    {
        using MultiCitadelChecks.Fixture fixture = new(this);
        CitadelSession citadels = fixture.Session;
        EconomyManager wallet = fixture.Main.GetNode<EconomyManager>("Economy");
        WaveManager waves = fixture.Main.GetNode<WaveManager>("Waves");
        var first = citadels.Objectives[0]; var second = citadels.Objectives[1];
        CreditBuffer a = citadels.CreditsFor(first), b = citadels.CreditsFor(second);
        Require(a != null && b != null && !ReferenceEquals(a, b), "Citadels never share a buffer");
        waves.Configure(new[] { Wave(1, Batch(1, 1, 0, Enemy("basic_mk1"))) }, fixture.Spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Two-Citadel fixture can start real wave");
        waves.SetPhysicsProcess(false);
        citadels._PhysicsProcess(6);
        Require(a.Current == 30 && b.Current == 30, "Both Citadels accrue 10 per 2 seconds independently");
        int before = wallet.GetBalance(wallet.CreditsResource);
        Require(citadels.TryCollectCredits(first) && a.Current == 0 && b.Current == 30
            && wallet.GetBalance(wallet.CreditsResource) == before + 30, "Collecting first Citadel never drains second");
        citadels._PhysicsProcess(2);
        Require(a.Current == 10 && b.Current == 40, "After collection, separate timers continue independently");
        Require(citadels.TryCollectCredits(second) && a.Current == 10 && b.Current == 0,
            "Collecting second Citadel leaves first alone");
        Require(!citadels.TryCollectCredits(null) && !citadels.TryCollectCredits(second),
            "Invalid and empty collections are rejected");
        waves.Configure(new[] { Wave(1, Batch(1, 1, 0, Enemy("basic_mk1"))) }, fixture.Spawns.Catalog.ToSceneMap());
        citadels._PhysicsProcess(100);
        Require(a.Current == 10 && b.Current == 0, "Between waves neither independent buffer grows");
        first.Health.TakeDamage(int.MaxValue);
        Require(!citadels.TryCollectCredits(first), "Destroyed Citadel cannot be collected");
        Require(!citadels.IsPhysicsProcessing(), "Mission-ending objective destruction sleeps production");
    }

    // =========================================================
    // Stop the suite on incorrect income or lifetime behavior.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
