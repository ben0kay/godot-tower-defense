// Event-driven, world-owned construction/upgrade spark controller. No per-tower scripts or idle polling.
// Installs a bounded GPUParticles2D node under each paid work site, including jobs restored from saves.
using Godot;
using System;
using System.Collections.Generic;

public partial class ConstructionSparkController : Node
{
    #region Configuration and State
    [Export] public ConstructionSparkConfig Config { get; set; }
    [Export] public PackedScene SparkScene { get; set; }
    private readonly Dictionary<TowerBase, GpuParticles2D> _emitters = new();
    private readonly Dictionary<TowerBase, Action> _waiting = new();
    private ConstructionWorkManager _work;
    private float _cellSize = 64f;

    public int ActiveEmitters => _emitters.Count;
    public bool HasEmitter(TowerBase tower) => tower != null && _emitters.ContainsKey(tower);
    public bool IsEmitting(TowerBase tower) => tower != null &&
        _emitters.TryGetValue(tower, out GpuParticles2D particles) &&
        GodotObject.IsInstanceValid(particles) && particles.Emitting;
    // True only when the new sharp welding layer is actively emitting.
    public bool IsWelding(TowerBase tower) => tower != null &&
        _emitters.TryGetValue(tower, out GpuParticles2D particles) &&
        GodotObject.IsInstanceValid(particles) &&
        particles.GetNodeOrNull<GpuParticles2D>("WeldingSparks") is { } welding && welding.Emitting;
    #endregion

    #region Lifecycle
    // =========================================================
    // Existing shared work events provide start/end notification for building and upgrades.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _work = GetNodeOrNull<ConstructionWorkManager>("../../ConstructionWork");
        _cellSize = GetNodeOrNull<WorldGrid>("../../Grid")?.Config?.CellSize ?? 64f;
        if (_work == null) return;
        _work.JobRegistered += OnRegistered;
        _work.JobUnregistered += OnUnregistered;
    }

    // =========================================================
    // A tower may register its initial construction before AddChild; attach the emitter to it
    // so the effect enters the scene with its tower and never needs position updates.
    private void OnRegistered(TowerBase tower)
    {
        if (Config?.Enabled != true || SparkScene == null || tower == null ||
            !GodotObject.IsInstanceValid(tower) || !tower.IsBusy || _emitters.ContainsKey(tower) ||
            _emitters.Count >= Mathf.Clamp(Config.MaxConcurrentSites, 1, 64)) return;

        Node node = SparkScene.Instantiate();
        if (node is not GpuParticles2D particles)
        {
            node?.Free();
            GD.PushWarning("Construction spark scene needs a GPUParticles2D root.");
            return;
        }
        GpuParticles2D welding = particles.GetNodeOrNull<GpuParticles2D>("WeldingSparks");
        if (particles.ProcessMaterial is not ParticleProcessMaterial source ||
            welding?.ProcessMaterial is not ParticleProcessMaterial weldingSource)
        {
            particles.Free();
            GD.PushWarning("Construction spark scene needs ParticleProcessMaterial.");
            return;
        }

        // Each active job gets private GPU material bounds; a real annulus leaves the center empty.
        var material = (ParticleProcessMaterial)source.Duplicate();
        particles.ProcessMaterial = material;
        Vector2 area = (Vector2)tower.Footprint * _cellSize;
        ConfigurePerimeterRing(material, area, Config.FootprintCoverage, .86f);
        particles.Amount = Mathf.Clamp(Config.ParticlesPerSite, 1, 32);
        particles.Lifetime = Mathf.Clamp(Config.ParticleLifetime, .1f, 1.5f);
        // SelfModulate preserves the original orange flecks without tinting the new white-hot child.
        particles.SelfModulate = Config.SparkTint;
        particles.Emitting = false;

        // One private GPU material for each layer, sized once to this tower's footprint.
        var hotMaterial = (ParticleProcessMaterial)weldingSource.Duplicate();
        welding.ProcessMaterial = hotMaterial;
        float hotCoverage = Mathf.Clamp(Config.WeldingFootprintCoverage, .15f, 1f);
        // Bright welding streaks begin in a narrow band close to the outer boundary.
        ConfigurePerimeterRing(hotMaterial, area, Config.WeldingFootprintCoverage, .93f);
        welding.Amount = Mathf.Clamp(Config.WeldingParticlesPerSite, 1, 24);
        welding.Lifetime = Mathf.Clamp(Config.WeldingLifetime, .1f, .8f);
        welding.Modulate = Config.WeldingTint;
        welding.Emitting = false;
        tower.AddChild(particles);
        _emitters.Add(tower, particles);

        if (tower.AwaitingConstructionDrone)
        {
            Action release = () => TryStart(tower);
            _waiting.Add(tower, release);
            tower.WorkProgressChanged += release;
        }
        else StartLayers(particles);
    }

    // =========================================================
    // Authoring uses Godot's Ring emission shape (6); scale its XY circumference to each footprint.
    // The inner radius leaves the middle empty, so both spark types actually spawn near the edge.
    // This is configured only on registration; GPU motion stays entirely autonomous.
    private static void ConfigurePerimeterRing(
        ParticleProcessMaterial material, Vector2 area, float coverage, float innerFraction)
    {
        float shorterSide = Mathf.Max(1f, Mathf.Min(area.X, area.Y));
        float outer = Mathf.Max(3f, shorterSide * Mathf.Clamp(coverage, .15f, .98f) * .5f);
        material.EmissionRingAxis = Vector3.Back;
        material.EmissionRingHeight = 0f;
        material.EmissionRingRadius = outer;
        material.EmissionRingInnerRadius = outer * innerFraction;
        material.EmissionShapeScale = new Vector3(
            Mathf.Max(1f, area.X / shorterSide), Mathf.Max(1f, area.Y / shorterSide), 1f);
    }

    // =========================================================
    // This fires when a construction/upgrade drone releases its hold, before work time advances.
    private void TryStart(TowerBase tower)
    {
        if (tower.AwaitingConstructionDrone || !tower.IsBusy) return;
        StopWaiting(tower);
        if (Config?.Enabled == true && _emitters.TryGetValue(tower, out GpuParticles2D particles) &&
            GodotObject.IsInstanceValid(particles)) StartLayers(particles);
    }

    // =========================================================
    // Both layers share the same work start; the welding layer is independently opt-out.
    private void StartLayers(GpuParticles2D particles)
    {
        particles.Emitting = true;
        GpuParticles2D hot = particles.GetNodeOrNull<GpuParticles2D>("WeldingSparks");
        if (hot != null) hot.Emitting = Config?.WeldingEnabled == true;
    }

    // =========================================================
    // Existing cancellation/completion/destroy callbacks stop and retire both emitters.
    private void OnUnregistered(TowerBase tower)
    {
        StopWaiting(tower);
        if (!_emitters.Remove(tower, out GpuParticles2D particles) || !GodotObject.IsInstanceValid(particles)) return;
        particles.Emitting = false;
        if (particles.GetNodeOrNull<GpuParticles2D>("WeldingSparks") is { } welding)
            welding.Emitting = false;
        if (particles.IsInsideTree()) particles.QueueFree(); else particles.Free();
    }

    // =========================================================
    // Remove the temporary hold-release subscription once it is used or the job ends.
    private void StopWaiting(TowerBase tower)
    {
        if (!_waiting.Remove(tower, out Action release)) return;
        if (GodotObject.IsInstanceValid(tower)) tower.WorkProgressChanged -= release;
    }

    // =========================================================
    // No references, particles or handlers survive level exit/retry.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_work))
        {
            _work.JobRegistered -= OnRegistered;
            _work.JobUnregistered -= OnUnregistered;
        }
        foreach (TowerBase tower in new List<TowerBase>(_waiting.Keys)) StopWaiting(tower);
        foreach (GpuParticles2D particles in _emitters.Values)
        {
            if (!GodotObject.IsInstanceValid(particles)) continue;
            particles.Emitting = false;
            if (particles.GetNodeOrNull<GpuParticles2D>("WeldingSparks") is { } welding)
                welding.Emitting = false;
            if (particles.IsInsideTree()) particles.QueueFree(); else particles.Free();
        }
        _emitters.Clear(); _waiting.Clear(); _work = null;
    }
    #endregion
}
