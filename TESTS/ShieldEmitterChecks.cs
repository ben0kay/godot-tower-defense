// Optional aura tests: safe capacity restoration, bounded registry pulses and independent source lifecycle.
using Godot;
using System;
public static class ShieldEmitterChecks
{
    // =========================================================
    // Real registered generators operate only on eligible live recipients and never alter authored data.
    public static void Run(Node main, SpawnManager spawns)
    {
        Enemy first = spawns.Spawn("shieldgenerator", true), second = spawns.Spawn("shieldgenerator", true);
        Enemy ally = spawns.Spawn("enemy001", true), fast = spawns.Spawn("enemy002", true), outside = spawns.Spawn("enemy001", true);
        Enemy zero = spawns.Spawn("enemy002", true);
        Enemy[] ships = { first, second, ally, fast, outside, zero };
        foreach (Enemy ship in ships) ship.Movement.SetPhysicsProcess(false);
        Vector2 origin = ally.GlobalPosition;
        first.GlobalPosition = origin; second.GlobalPosition = origin + new Vector2(8, 0);
        fast.GlobalPosition = origin + new Vector2(0, 16); zero.GlobalPosition = origin + new Vector2(16, 0);
        outside.GlobalPosition = origin + new Vector2(500, 0);
        zero.Definition = (EnemyDefinition)zero.Definition.Duplicate(); zero.Definition.MaxShield = 0;
        ShieldEmitter a = first.GetNode<ShieldEmitter>("Abilities/ShieldEmitter"), b = second.GetNode<ShieldEmitter>("Abilities/ShieldEmitter");
        a.SetPhysicsProcess(false); b.SetPhysicsProcess(false);
        ShieldEmitterConfig source = GD.Load<ShieldEmitterConfig>("res://ENEMIES/Abilities/ShieldEmitter/DefaultShieldEmitterConfig.tres");
        Require(a.Config != b.Config && a.Config != source && first.CurrentShield == 0 && ally.CurrentShield == 0 &&
            ally.GetNodeOrNull<Node>("Abilities") == null && fast.GetNodeOrNull<Node>("Abilities") == null,
            "Only generator scenes carry optional nodes; plain types remain zero-cost with unshielded starts");
        Require(ally.RestoreShield(-1) == 0 && ally.RestoreShield(float.NaN) == 0 && ally.RestoreShield(float.PositiveInfinity) == 0 && zero.RestoreShield(10) == 0,
            "Safe recipient API rejects invalid amounts and zero capacity");
        a._PhysicsProcess(0); b._PhysicsProcess(0);
        Require(a.Pulses == 0 && b.Pulses == 0, "Initialization staggers without an immediate pulse");
        a._PhysicsProcess(1);
        Require(a.Pulses == 1 && ally.CurrentShield == 20 && fast.CurrentShield == 20 && outside.CurrentShield == 0 && zero.CurrentShield == 0 &&
            first.CurrentShield == 0 && second.CurrentShield == 20 && !ally.GetNode<DefenseBar>("DefenseBar").Visible,
            "Pulse restores nearby eligible allies, respects self/range/capacity and leaves hull bar alone");
        Require(b.Pulses == 0, "Each source owns its own timer");
        a._PhysicsProcess(0.5);
        Require(a.Pulses == 1 && ally.CurrentShield == 20, "No query/restoration between configured intervals");
        a._PhysicsProcess(0.5); b._PhysicsProcess(1);
        Require(a.Pulses == 2 && b.Pulses == 1 && ally.CurrentShield == 60 && fast.CurrentShield == 40 && first.CurrentShield == 20,
            "Two emitters combine through per-recipient caps without shared cooldown state");
        ally.RestoreShield(float.MaxValue);
        Require(ally.CurrentShield == 100 && ally.Health == 60 && ally.Definition.MaxShield == 100 && source.ShieldPerPulse == 20,
            "Restoration cannot overflow capacity or mutate hull/definition/source settings");
        ally.TakeDamage(100); fast.TakeDamage(20);
        a.Config.Eligibility = ShieldEmitterConfig.TargetEligibility.AlreadyShieldedOnly;
        a._PhysicsProcess(1);
        Require(ally.CurrentShield == 0 && fast.CurrentShield == 40, "Eligibility can require existing shields without granting capacity");
        a.Config.AffectSelf = true; first.TakeDamage(first.CurrentShield); a._PhysicsProcess(1);
        // Already-shielded policy intentionally excludes a zero-shield host even with AffectSelf enabled.
        Require(first.CurrentShield == 0, "Self policy still respects target eligibility");
        a.Config.Eligibility = ShieldEmitterConfig.TargetEligibility.AnyShieldCapacity; a._PhysicsProcess(1);
        Require(first.CurrentShield == 20 && ally.CurrentShield == 20, "Explicit self targeting and zero-shield eligible allies work");
        a.Config.MaxCandidatesPerPulse = 2; a.Config.MaxRecipientsPerPulse = 1;
        a._PhysicsProcess(1);
        Require(a.LastCandidatesChecked <= 2 && a.LastRecipientsRestored <= 1, "Candidate and recipient budgets bound each pulse");
        int pulses = a.Pulses; a._PhysicsProcess(20);
        Require(a.Pulses == pulses + 1, "Lag cannot create a catch-up burst");
        a.Enabled = false; pulses = a.Pulses; float preserved = ally.CurrentShield;
        a._PhysicsProcess(10);
        Require(a.Pulses == pulses && !a.IsPhysicsProcessing() && ally.CurrentShield == preserved, "Disabled source does no work");
        a.Enabled = true; a.Config = null; a._PhysicsProcess(10);
        Require(a.Pulses == pulses && !a.IsPhysicsProcessing(), "Null config is a safe inert component");
        a.Config = (ShieldEmitterConfig)source.Duplicate(); a.Config.PulseIntervalSeconds = 0; a._PhysicsProcess(10);
        Require(a.Pulses == pulses && !a.IsPhysicsProcessing(), "Invalid interval cannot create unbounded queries");
        a.Config.PulseIntervalSeconds = 1; a.Config.MaxCandidatesPerPulse = 512; a._PhysicsProcess(1);
        preserved = ally.CurrentShield; first.TakeDamage(10000); a._PhysicsProcess(10);
        Require(a.Pulses == pulses + 1 && ally.CurrentShield == preserved, "Source death stops future pulses without erasing allies' shields");
        int otherPulses = b.Pulses; second.Arrive(); b._PhysicsProcess(10);
        Require(b.Pulses == otherPulses && ally.CurrentShield == preserved, "Source arrival stops its schedule independently");
        Enemy unloaded = spawns.Spawn("shieldgenerator", true); unloaded.Movement.SetPhysicsProcess(false);
        ShieldEmitter component = unloaded.GetNode<ShieldEmitter>("Abilities/ShieldEmitter");
        main.GetNode("World/Enemies").RemoveChild(unloaded);
        Require(!component.IsPhysicsProcessing(), "External source unload cleans its optional component");
        int stopped = component.Pulses; component._PhysicsProcess(10);
        Require(component.Pulses == stopped && ally.CurrentShield == preserved, "No pulse survives unload/restart");
        unloaded.Free();
        Enemy queued = spawns.Spawn("shieldgenerator", true); queued.Movement.SetPhysicsProcess(false);
        ShieldEmitter queuedAbility = queued.GetNode<ShieldEmitter>("Abilities/ShieldEmitter");
        queued.QueueFree(); queuedAbility._PhysicsProcess(10);
        Require(queuedAbility.Pulses == 0 && !queuedAbility.IsPhysicsProcessing(), "Queued-for-deletion source cannot pulse");
        queued.Free();
        ShieldEmitter noHost = new() { Config = (ShieldEmitterConfig)source.Duplicate() };
        main.AddChild(noHost); noHost._PhysicsProcess(10);
        Require(noHost.Pulses == 0 && !noHost.IsPhysicsProcessing(), "Unattached optional component is inert");
        noHost.Free();
        foreach (Enemy ship in ships) if (ship.IsAlive) ship.Arrive();
        main.GetNode<EconomyManager>("Economy").Initialize(spawns.Paths.Navigation.Grid.Level.Settings);
        var health = main.GetNode<CitadelSession>("CitadelSession").Health; health.Initialize(health.MaxHealth);
        Require(source.PulseIntervalSeconds == 1 && source.MaxCandidatesPerPulse == 128, "Ability tuning remains isolated from shared defaults");
        GD.Print("PASS: optional ShieldEmitter staggering/intervals, two-source isolation, recipient caps/range/eligibility, bounded queries and lifecycle cleanup");
    }
    // =========================================================
    // Fail immediately on aura safety/lifecycle regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
