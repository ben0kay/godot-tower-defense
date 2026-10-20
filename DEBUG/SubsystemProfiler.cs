// PF2: opt-in world-scoped CPU observations, fixed storage and stack scopes; never a gameplay scheduler.
using Godot;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

public enum ProfileCategory { Movement, Clearance, Registry, Towers, Targeting, EnemyGuns, Projectiles, AreaDamage, PathWork, AStar, Refinement }
public readonly record struct ProfileReading(double Mean, double P95, double Peak, long Calls, int Samples);

public partial class SubsystemProfiler : Node
{
    #region Capture State
    public const int CategoryCount = 11;
    public static readonly string[] Names = { "Enemy movement", "  Clearance (nested)", "  Registry (nested)", "Friendly towers", "  Target searches (nested)", "Enemy guns", "Projectiles / waves", "  Area damage (nested)", "Path work + audits", "  A* (nested)", "  Route refinement (nested)" };
    private static SubsystemProfiler _active;
    private Node _world;
    private bool _running, _collecting;
    private int _generation, _cursor, _count, _tick, _capacity;
    private readonly long[] _current = new long[CategoryCount], _calls = new long[CategoryCount];
    private double[,] _history;
    private long[,] _callHistory;
    private double[] _scratch, _overheadHistory;
    private long _overhead;
    private double _overheadTotal;
    private sealed class Binding { public bool Belongs; }
    private ConditionalWeakTable<Node, Binding> _owners = new();
    public bool Running => _running;
    public int SampleEveryTicks { get; set; } = 1;
    public int Samples => _count;
    public double BookkeepingMeanMs { get; private set; }
    #endregion

    #region Lifecycle and Timing
    // =========================================================
    // The early sampler and late F4 panel bracket normal-priority gameplay physics callbacks.
    public void Initialize(Node world, int capacity = 300)
    {
        _world = world; _capacity = Math.Clamp(capacity, 30, 1200);
        _history = new double[CategoryCount, _capacity]; _callHistory = new long[CategoryCount, _capacity];
        _scratch = new double[_capacity]; _overheadHistory = new double[_capacity];
        ProcessPhysicsPriority = int.MinValue; ProcessMode = ProcessModeEnum.Pausable; SetPhysicsProcess(false);
    }
    // =========================================================
    // Only one battlefield capture is active; fresh captures cannot inherit another world's timings or node wrappers.
    public void Start()
    {
        if (!OS.IsDebugBuild() || _world == null) return;
        _active?.Stop(); _active = this; _running = true; _generation++; _cursor = _count = _tick = 0;
        _owners = new(); Array.Clear(_current); Array.Clear(_calls); Array.Clear(_history); Array.Clear(_callHistory);
        Array.Clear(_overheadHistory); _overheadTotal = BookkeepingMeanMs = 0; SetPhysicsProcess(true);
    }
    // =========================================================
    // Hide, scene unload or capture toggle stops hooks and releases the weak owner cache.
    public void Stop()
    { _running = _collecting = false; _generation++; SetPhysicsProcess(false); if (_active == this) _active = null; _owners.Clear(); }
    // =========================================================
    // Selected ticks include zero-work categories; unsampled ticks do not dilute means/p95.
    public override void _PhysicsProcess(double delta)
    {
        _collecting = _running && !GetTree().Paused && _tick++ % Math.Clamp(SampleEveryTicks, 1, 60) == 0;
        Array.Clear(_current); Array.Clear(_calls); _overhead = 0;
    }
    // =========================================================
    // World filtering is resolved once per live node/capture; weak keys do not retain expired bullet nodes.
    public static Scope Measure(Node owner, ProfileCategory category)
    {
        var session = _active;
        if (session == null || !session._collecting || owner == null) return default;
        long start = Stopwatch.GetTimestamp();
        if (!session._owners.TryGetValue(owner, out Binding binding))
        {
            if (!GodotObject.IsInstanceValid(owner)) return default;
            binding = new() { Belongs = owner == session._world || session._world.IsAncestorOf(owner) };
            session._owners.Add(owner, binding);
        }
        if (!binding.Belongs) return default;
        long body = Stopwatch.GetTimestamp(); session._overhead += body - start;
        return new(session, category, body, session._generation);
    }
    public readonly struct Scope : IDisposable
    {
        private readonly SubsystemProfiler _session;
        private readonly ProfileCategory _category;
        private readonly long _start;
        private readonly int _generation;
        internal Scope(SubsystemProfiler session, ProfileCategory category, long start, int generation)
        { _session = session; _category = category; _start = start; _generation = generation; }
        // =========================================================
        // Struct disposal records exits/returns/exceptions without delegates, heap scopes or Stopwatch objects.
        public void Dispose()
        {
            if (_session == null || !_session._collecting || _session._generation != _generation) return;
            long end = Stopwatch.GetTimestamp();
            _session._current[(int)_category] += end - _start; _session._calls[(int)_category]++;
            _session._overhead += Stopwatch.GetTimestamp() - end;
        }
    }
    // =========================================================
    // Commit once after gameplay, then close collection so render callbacks cannot contaminate physics rows.
    public void EndTick()
    {
        if (!_collecting) return;
        _collecting = false; double scale = 1000.0 / Stopwatch.Frequency;
        for (int i = 0; i < CategoryCount; i++)
        { _history[i, _cursor] = _current[i] * scale; _callHistory[i, _cursor] = _calls[i]; }
        _overheadTotal -= _overheadHistory[_cursor];
        _overheadHistory[_cursor] = _overhead * scale; _overheadTotal += _overheadHistory[_cursor];
        _cursor = (_cursor + 1) % _capacity; _count = Math.Min(_count + 1, _capacity);
        BookkeepingMeanMs = _overheadTotal / _count;
    }
    // =========================================================
    // Native teardown clears the static active pointer before another mission starts.
    public override void _ExitTree() => Stop();
    #endregion

    #region Readout and Capture
    // =========================================================
    // Fixed scratch sorting happens only at display refresh/report time, never in an enemy/projectile hook.
    public ProfileReading Read(ProfileCategory category)
    {
        if (_count == 0) return default;
        double sum = 0, peak = 0; long calls = 0;
        for (int i = 0; i < _count; i++)
        { double ms = _history[(int)category, i]; _scratch[i] = ms; sum += ms; peak = Math.Max(peak, ms); calls += _callHistory[(int)category, i]; }
        Array.Sort(_scratch, 0, _count);
        return new(sum / _count, _scratch[Math.Max(0, (int)Math.Ceiling(_count * .95) - 1)], peak, calls, _count);
    }
    // =========================================================
    // Explicit dump preserves workload/build metadata and limitations; does not auto-write profiles or campaign saves.
    public string Report(string workload)
    {
        StringBuilder text = new();
        text.AppendLine($"PF2 {DateTime.UtcNow:O} | {workload} | debug={OS.IsDebugBuild()} | Godot={Engine.GetVersionInfo()["string"]}");
        text.AppendLine($"PhysicsHz={Engine.PhysicsTicksPerSecond}; sampleEveryTicks={SampleEveryTicks}; retainedTicks={_count}/{_capacity}; running={_running}");
        text.AppendLine("CPU ms per sampled physics tick: mean / p95 / peak / calls. Nested/inclusive rows overlap: DO NOT SUM. Not GPU/FPS timings.");
        for (int i = 0; i < CategoryCount; i++)
        { var r = Read((ProfileCategory)i); text.AppendLine($"{Names[i]}: {r.Mean:0.000} / {r.P95:0.000} / {r.Peak:0.000} / {r.Calls}"); }
        text.AppendLine($"Observed hook bookkeeping mean={BookkeepingMeanMs:0.000}ms/tick (partial overhead; compare profiling OFF/ON on target hardware).");
        return text.ToString();
    }
    #endregion
}
