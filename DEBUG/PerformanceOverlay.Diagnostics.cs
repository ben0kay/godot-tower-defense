// Lightweight overlay-only frame distribution, renderer measurements and existing visual counters.
using Godot;
using System;

public partial class PerformanceOverlay
{
    private const int FrameHistoryCapacity = 600;
    private readonly double[] _frameHistory = new double[FrameHistoryCapacity];
    private readonly double[] _frameScratch = new double[FrameHistoryCapacity];
    private int _frameHistoryCount, _frameHistoryCursor, _pathBudgetTicks, _observedPathTicks;
    private double _frameP95, _frameP99, _historySeconds, _renderCpuMs, _renderGpuMs, _renderSetupMs;
    private WorldLightingController _lighting;
    private Rid _measuredViewport;
    private bool _renderTimingEnabled;
    private string _renderingMethod = "N/A";

    // =========================================================
    // Measurement follows overlay visibility and can be disabled in the Inspector for comparison.
    private void SetRenderingMeasurement(bool visible)
    {
        bool enabled = visible && MeasureRendering && !_headless;
        if (_renderTimingEnabled == enabled) return;
        if (enabled)
        {
            _measuredViewport = GetViewport().GetViewportRid();
            RenderingServer.ViewportSetMeasureRenderTime(_measuredViewport, true);
        }
        else
        {
            RenderingServer.ViewportSetMeasureRenderTime(_measuredViewport, false);
        }
        _renderTimingEnabled = enabled;
    }

    private void ResetDiagnostics()
    {
        _frameHistoryCount = _frameHistoryCursor = 0;
        _frameP95 = _frameP99 = _historySeconds = 0;
        _renderCpuMs = _renderGpuMs = _renderSetupMs = 0;
    }

    private void RecordDiagnosticFrame(double milliseconds)
    {
        _frameHistory[_frameHistoryCursor] = milliseconds;
        _frameHistoryCursor = (_frameHistoryCursor + 1) % FrameHistoryCapacity;
        _frameHistoryCount = Math.Min(FrameHistoryCapacity, _frameHistoryCount + 1);
    }

    // =========================================================
    // Sort fixed scratch storage only at UI refresh/report time. No per-frame allocation or scene scans.
    private void ReadDiagnostics()
    {
        SetRenderingMeasurement(Visible);
        _historySeconds = 0;
        if (_frameHistoryCount > 0)
        {
            Array.Copy(_frameHistory, _frameScratch, _frameHistoryCount);
            for (int i = 0; i < _frameHistoryCount; i++) _historySeconds += _frameScratch[i] / 1000;
            Array.Sort(_frameScratch, 0, _frameHistoryCount);
            _frameP95 = _frameScratch[Math.Max(0, (int)Math.Ceiling(_frameHistoryCount * .95) - 1)];
            _frameP99 = _frameScratch[Math.Max(0, (int)Math.Ceiling(_frameHistoryCount * .99) - 1)];
        }
        _renderCpuMs = _renderTimingEnabled ? RenderingServer.ViewportGetMeasuredRenderTimeCpu(_measuredViewport) : 0;
        _renderGpuMs = _renderTimingEnabled ? RenderingServer.ViewportGetMeasuredRenderTimeGpu(_measuredViewport) : 0;
        _renderSetupMs = !_headless ? RenderingServer.GetFrameSetupTimeCpu() : 0;
    }

    private static string Timing(double milliseconds) =>
        double.IsFinite(milliseconds) && milliseconds > 0 ? milliseconds.ToString("0.00") + "ms" : "N/A";

    private string VisualModes()
    {
        string mode = (_projectiles as FriendlyProjectileSystem)?.RenderSettings.TexturedTracers == true ? "textured" : "original";
        string explosions = _enemies == null ? "N/A" : _enemies.ExplosionFxEnabled ? "on" : "off";
        string bars = _enemies == null ? "N/A" : _enemies.HealthBarsEnabled ? "on" : "off";
        return $"Tracers {mode} · enemy explosions {explosions} · bars {bars}";
    }

    // =========================================================
    // These rows describe different scopes. Renderer timings are latest-frame readings, not percentiles.
    private void WriteDiagnostics()
    {
        ReadDiagnostics();
        Write(25, $"Frame p95 {_frameP95:0.0} · p99 {_frameP99:0.0}ms · n={_frameHistoryCount}/{_historySeconds:0.0}s", Pressure(_frameP99, FrameWarningMs, FrameCriticalMs));
        Write(26, $"A* peak {_astarPeak:0.00}ms (inside path work) · over-budget {_pathBudgetTicks}/{_observedPathTicks} ticks", Normal);
        Write(27, $"Render last CPU {Timing(_renderCpuMs)} · GPU {Timing(_renderGpuMs)} · setup {Timing(_renderSetupMs)}", Normal);
        Write(28, $"Managed heap {GC.GetTotalMemory(false) / 1048576.0:0.0}MB · GC totals {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}", Normal);
        Write(29, GodotObject.IsInstanceValid(_lighting) ? _lighting.DebugVisualSummary() : "Fog/lights N/A", Normal);
        Write(30, $"Global exhaust emitters {EnemyThrusterVisual.ActiveGpuEmitters} · explosions {DeathExplosion.ActiveCount} · tracer cache {TexturedProjectileDrawing.CachedStyleCount}/64", Normal);
        Write(31, VisualModes(), Normal);
        Write(32, $"{_renderingMethod} · viewport timings can overlap engine time · zero = N/A", new Color("#9aafbd"));
    }

    private string DiagnosticReport()
    {
        ReadDiagnostics();
        return $"frameP95Ms={_frameP95:0.000}; frameP99Ms={_frameP99:0.000}; frameSamples={_frameHistoryCount}; frameWindowSeconds={_historySeconds:0.000}; " +
            $"pathPeakMs={_pathPeak:0.000}; aStarPeakMs={_astarPeak:0.000}; pathSoftBudgetMs={_paths?.Config?.MaxPathfindingMillisecondsPerFrame}; pathOverBudgetTicks={_pathBudgetTicks}/{_observedPathTicks}; peakWindowSeconds={PeakWindowSeconds}; " +
            $"renderer={_renderingMethod}; renderMeasurement={_renderTimingEnabled}; viewportCpu={Timing(_renderCpuMs)}; viewportGpu={Timing(_renderGpuMs)}; renderSetupCpu={Timing(_renderSetupMs)}; " +
            $"managedHeapBytes={GC.GetTotalMemory(false)}; gcTotals={GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}; " +
            $"globalExhaustEmitters={EnemyThrusterVisual.ActiveGpuEmitters}; globalExplosions={DeathExplosion.ActiveCount}; cachedTracerStyles={TexturedProjectileDrawing.CachedStyleCount}; " +
            $"{VisualModes()}; {(GodotObject.IsInstanceValid(_lighting) ? _lighting.DebugVisualSummary() : "fog/lights=N/A")}";
    }
}
