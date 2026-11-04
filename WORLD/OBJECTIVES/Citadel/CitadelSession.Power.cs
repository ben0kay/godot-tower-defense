// Citadel-owned producer bindings; health/lifetime/config/terrain events update the shared accounting without ticking.
using Godot;
using System;
using System.Collections.Generic;

public partial class CitadelSession
{
    [ExportGroup("Citadel Starter Power")]
    [Export] public CitadelPowerConfig PowerConfig { get; set; }
    private PowerGridManager _power;
    private WorldGrid _powerGrid;
    private CitadelPowerConfig _boundPowerConfig;
    private readonly List<PowerBinding> _powerBindings = new();
    private sealed class PowerBinding
    {
        public CitadelDestination Objective;
        public string Id;
        public Action<int, int> HealthChanged;
        public Action Exiting;
        public bool Alive, Retired;
    }

    // =========================================================
    // Bind after every Citadel's health is initialized. Older custom fixtures without power services remain usable.
    private void InitializePower()
    {
        _power = GetNodeOrNull<PowerGridManager>("../World/PowerNetworks");
        if (_power == null) return;
        _powerGrid = _power.Grid;
        _boundPowerConfig = PowerConfig ?? new CitadelPowerConfig();
        _boundPowerConfig.Changed += RefreshPower;
        _powerGrid.TerrainRebuilt += RefreshPower;
        foreach (CitadelDestination objective in Objectives)
        {
            if (objective.Root is not WorldObjectFootprint)
                GD.PushWarning($"{objective.DisplayName}: starter power requires a WorldObjectFootprint root.");
            PowerBinding binding = new() { Objective = objective, Id = "citadel:" + objective.Id };
            binding.HealthChanged = (current, maximum) =>
            {
                bool alive = maximum > 0 && current > 0;
                if (binding.Alive != alive) RefreshPower(binding);
            };
            binding.Exiting = () => { binding.Retired = true; RefreshPower(binding); };
            objective.Health.HealthChanged += binding.HealthChanged;
            objective.Root.TreeExiting += binding.Exiting;
            objective.Health.TreeExiting += binding.Exiting;
            _powerBindings.Add(binding); RefreshPower(binding);
        }
    }

    // =========================================================
    // Explicit terrain rebuilds also refresh authored root positions; config edits update all Citadels once.
    private void RefreshPower()
    { foreach (PowerBinding binding in _powerBindings) RefreshPower(binding); }

    // =========================================================
    // A Citadel root anchors the upper-left footprint cell. Missing conducting ground never creates phantom supply.
    private void RefreshPower(PowerBinding binding)
    {
        CitadelDestination objective = binding.Objective;
        binding.Alive = !binding.Retired && objective.Available && GodotObject.IsInstanceValid(objective.Health)
            && objective.Health.MaxHealth > 0 && objective.Health.CurrentHealth > 0;
        if (!GodotObject.IsInstanceValid(_power)) return;
        if (!binding.Alive || objective.Root is not WorldObjectFootprint footprint)
        { _power.RemoveEndpoint(binding.Id); return; }
        _power.SetProducer(binding.Id, _powerGrid.WorldToCell(footprint.GlobalPosition), footprint.SizeCells, _boundPowerConfig.Output);
    }

    // =========================================================
    // Remove only this session's sources; callbacks never survive retry, Continue, profile switch or level unload.
    private void ClearPower()
    {
        if (GodotObject.IsInstanceValid(_boundPowerConfig)) _boundPowerConfig.Changed -= RefreshPower;
        if (GodotObject.IsInstanceValid(_powerGrid)) _powerGrid.TerrainRebuilt -= RefreshPower;
        foreach (PowerBinding binding in _powerBindings)
        {
            if (GodotObject.IsInstanceValid(_power)) _power.RemoveEndpoint(binding.Id);
            if (GodotObject.IsInstanceValid(binding.Objective.Health))
            {
                binding.Objective.Health.HealthChanged -= binding.HealthChanged;
                binding.Objective.Health.TreeExiting -= binding.Exiting;
            }
            if (GodotObject.IsInstanceValid(binding.Objective.Root)) binding.Objective.Root.TreeExiting -= binding.Exiting;
        }
        _powerBindings.Clear(); _power = null; _powerGrid = null; _boundPowerConfig = null;
    }
}
