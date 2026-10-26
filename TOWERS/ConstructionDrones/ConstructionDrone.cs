// Pooled builder presentation only. The single manager advances flight; this scene never processes on its own.
using Godot;
public partial class ConstructionDrone : Node2D
{
    private ConstructionBeam _beam;
    private Sprite2D _sprite;
    public bool BeamVisible => _beam?.Visible == true && Visible;
    // =========================================================
    // Cached imported SVG and a single lightweight beam child, independent of ammunition couriers.
    public override void _Ready()
    { _beam = GetNode<ConstructionBeam>("Beam"); _sprite = GetNode<Sprite2D>("Sprite"); SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // All endpoints are world-space; no enemy, tower, work clock or Citadel references live in this visual.
    public void Present(Vector2 position, Vector2 target, float halfWidth, float strength, ConstructionDroneMasterConfig config, float beamAge = 0, float heading = -Mathf.Pi / 2)
    {
        GlobalPosition = position; Visible = config.ShowVisuals;
        _sprite.Rotation = heading + Mathf.Pi / 2;
        _beam.Present(target, halfWidth, config.ShowVisuals ? strength : 0, config, beamAge);
    }
    // =========================================================
    // Idle pool entries do no update/drawing and retain no assignment references.
    public void Park() { Hide(); _beam?.Hide(); }
}
