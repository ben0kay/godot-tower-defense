// Test-only pre-spatial implementation for parity and same-machine benchmarks; swept relative motion and hit-once IDs, never repeated instant AoE.
using Godot;
using System;
using System.Collections.Generic;

public partial class BruteForceShockwave : Node2D
{
    private EnemyRegistry _registry;
    private Vector2 _origin;
    private Node2D _effects;
    private ShockwaveStyle _style;
    private DamageInfo _hit;
    private float _maximum, _speed, _edge;
    private bool _initialized, _finished;
    private readonly Dictionary<ulong, Vector2> _positions = new();
    private readonly HashSet<ulong> _damaged = new();
    private readonly List<(Enemy Enemy, DamageInfo Hit)> _recipients = new();
    public float PreviousRadius { get; private set; }
    public float CurrentRadius { get; private set; }
    public int AppliedHits { get; private set; }
    public ShockwaveEffect Visual { get; private set; }
    public bool IsComplete => _finished;

    // =========================================================
    // Snapshot emission inputs; subsequent tower upgrades or style edits cannot change this attack.
    public void Initialize(EnemyRegistry registry, Vector2 origin, DamageInfo hit, float maximumRadius,
        float expansionSpeed, float edgeFraction, Node2D effects = null, ShockwaveStyle style = null)
    {
        if (_initialized) throw new InvalidOperationException("Wave is already initialized.");
        AreaDamage.Validate(maximumRadius, edgeFraction);
        if (!GodotObject.IsInstanceValid(registry) || !origin.IsFinite()) throw new ArgumentException("Invalid wave source.");
        if (!float.IsFinite(expansionSpeed) || expansionSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(expansionSpeed));
        _registry = registry; _origin = origin; _hit = hit; _maximum = maximumRadius; _speed = expansionSpeed; _edge = edgeFraction;
        _initialized = true; _positions.EnsureCapacity(registry.Living.Count);
        foreach (Enemy enemy in registry.Living)
            if (Eligible(enemy) && enemy.GlobalPosition.IsFinite()) _positions[enemy.GetInstanceId()] = enemy.GlobalPosition;
        _effects = effects; _style = style == null ? null : (ShockwaveStyle)style.Duplicate(true);
    }

    // =========================================================
    // Keep one fixed world origin and create optional external presentation only for active scene delivery.
    public override void _Ready()
    {
        if (!_initialized) { SetPhysicsProcess(false); return; }
        GlobalPosition = _origin;
        Visual = ShockwaveEffect.Spawn(_effects, _origin, _style, _maximum, externallyDriven: true);
    }

    // =========================================================
    // Scan once, stage crossings, then dispatch after enumeration so deaths/removals cannot corrupt it.
    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized || _finished || !IsInsideTree() || IsQueuedForDeletion() || !double.IsFinite(delta) || delta <= 0) return;
        if (!GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || _registry.IsQueuedForDeletion())
        { Finish(); return; }
        PreviousRadius = CurrentRadius;
        double advance = Math.Min((double)_maximum - CurrentRadius, _speed * delta);
        CurrentRadius = (float)Math.Min(_maximum, CurrentRadius + advance);
        // The final tick can end mid-delta: sample enemy motion only up to the front's expiration time.
        double activeFraction = _speed * delta > 0 ? Math.Clamp(advance / (_speed * delta), 0, 1) : 0;
        _recipients.Clear();
        foreach (Enemy enemy in _registry.Living)
        {
            if (!Eligible(enemy)) continue;
            ulong id = enemy.GetInstanceId();
            if (_damaged.Contains(id)) continue;
            Vector2 end = enemy.GlobalPosition;
            if (!end.IsFinite()) { _positions.Remove(id); continue; }
            Vector2 start = _positions.TryGetValue(id, out Vector2 old) ? old : end;
            _positions[id] = end;
            Vector2 activeEnd = start.Lerp(end, (float)activeFraction);
            Vector2 relativeStart = start - _origin, relativeEnd = activeEnd - _origin;
            if (!relativeStart.IsFinite() || !relativeEnd.IsFinite()
                || !Crossing(relativeStart, relativeEnd, PreviousRadius, CurrentRadius, out double time)) continue;
            float encounterRadius = (float)(PreviousRadius + (CurrentRadius - PreviousRadius) * time);
            _recipients.Add((enemy, AreaDamage.AtDistance(_hit, encounterRadius, _maximum, _edge)));
        }
        if (GodotObject.IsInstanceValid(Visual) && !Visual.IsQueuedForDeletion())
            Visual.SetWavefront(CurrentRadius, _maximum > 0 ? CurrentRadius / _maximum : 1);
        try
        {
            for (int i = 0; i < _recipients.Count; i++)
            {
                var recipient = _recipients[i];
                if (_finished || !IsInsideTree() || IsQueuedForDeletion()) break;
                if (!Eligible(recipient.Enemy) || !_damaged.Add(recipient.Enemy.GetInstanceId())) continue;
                recipient.Enemy.ApplyDamage(recipient.Hit); AppliedHits++;
            }
        }
        catch { Finish(); throw; }
        if (CurrentRadius >= _maximum) Finish();
    }

    // =========================================================
    // Solve |p0 + (p1-p0)t|² = (r0 + (r1-r0)t)² for the first contact within this active tick.
    // This catches both long radial steps and fast enemies crossing the front between two samples.
    internal static bool Crossing(Vector2 start, Vector2 end, double from, double to, out double time)
    {
        double x = start.X, y = start.Y, vx = (double)end.X - x, vy = (double)end.Y - y, dr = to - from;
        double a = vx * vx + vy * vy - dr * dr;
        double b = 2 * (x * vx + y * vy - from * dr), c = x * x + y * y - from * from;
        time = 0;
        if (c == 0) return true;
        if (Math.Abs(a) < 1e-10)
        {
            if (Math.Abs(b) < 1e-10) return false;
            double root = -c / b;
            if (root < -1e-8 || root > 1 + 1e-8) return false;
            time = Math.Clamp(root, 0, 1); return true;
        }
        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return false;
        // Stable quadratic roots avoid cancellation when movement nearly matches expansion speed.
        double q = -.5 * (b + Math.CopySign(Math.Sqrt(discriminant), b));
        double first = q / a, second = q == 0 ? first : c / q;
        if (first > second) (first, second) = (second, first);
        double candidate = first >= -1e-8 ? first : second;
        if (candidate < -1e-8 || candidate > 1 + 1e-8) return false;
        time = Math.Clamp(candidate, 0, 1); return true;
    }

    // =========================================================
    // Registry membership/native lifetime can change during an earlier recipient's death callback.
    private bool Eligible(Enemy enemy) => GodotObject.IsInstanceValid(_registry) && _registry.IsInsideTree()
        && !_registry.IsQueuedForDeletion() && GodotObject.IsInstanceValid(enemy) && enemy.IsAlive
        && enemy.IsInsideTree() && !enemy.IsQueuedForDeletion() && _registry.Contains(enemy);

    // =========================================================
    // Expiration/unload cancel delivery and release per-emission scratch storage and its optional visual.
    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        if (GodotObject.IsInstanceValid(Visual) && !Visual.IsQueuedForDeletion()) Visual.Complete();
        _positions.Clear(); _damaged.Clear(); _recipients.Clear(); SetPhysicsProcess(false); QueueFree();
    }

    // =========================================================
    // An unloaded emitter cannot resume damage, and the world effect is not left orphaned.
    public override void _ExitTree() => Finish();
}
