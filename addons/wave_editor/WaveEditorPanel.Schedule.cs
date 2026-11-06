// Event-driven timing and source preview; estimates exclude blocking and portal opening delays.
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WaveEditorPanel
{
    // =========================================================
    // Rebuild only on authoring edits, retaining no game timers or map instances.
    private void RefreshTimeline()
    {
        if (_timeline == null) return;
        // Retain the pane heading.
        foreach (Node child in _timeline.GetChildren().Skip(1).ToArray())
        { _timeline.RemoveChild(child); child.QueueFree(); }
        if (Wave == null) return;
        try
        {
            if (!Enum.IsDefined(Wave.Schedule)) throw new ArgumentException("Invalid schedule mode.");
            var rows = new List<(double Start, double End, WaveBatchResource Batch)>();
            double cursor = 0, horizon = 0;
            foreach (var batch in Wave.Batches)
            {
                if (batch == null || batch.Count < 1 || !WaveCatalog.ValidStartOffset(batch.StartOffsetSeconds) ||
                    !double.IsFinite(batch.Interval) || batch.Interval < 0 || !double.IsFinite(batch.After) || batch.After < 0)
                    throw new ArgumentException("Invalid batch timing.");
                double start = Wave.Schedule == WaveScheduleMode.Parallel ? batch.StartOffsetSeconds : cursor;
                double end = start + (batch.Count - 1) * batch.Interval;
                if (!double.IsFinite(end)) throw new ArgumentException("Spawn duration is too large.");
                rows.Add((start, end, batch)); horizon = Math.Max(horizon, end);
                cursor = end + batch.After;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                _timeline.AddChild(new Label { Text = $"Group {i + 1}: {row.Start:0.##}–{row.End:0.##}s · {row.Batch.Count} ships",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart });
                _timeline.AddChild(TimelineBar(row.Start, row.End, Math.Max(1, horizon)));
                _timeline.AddChild(new Label { Text = SourceDistribution(row.Batch),
                    AutowrapMode = TextServer.AutowrapMode.WordSmart });
            }
            _timeline.AddChild(new Label { Text = "Estimated first–last spawn times; blocked exits, opening portals and frame budgets can delay groups. Source selection also respects the loaded map.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }
        catch (ArgumentException error)
        { _timeline.AddChild(new Label { Text = "Preview unavailable: " + error.Message, AutowrapMode = TextServer.AutowrapMode.WordSmart }); }
    }

    // =========================================================
    // Relative bars resize through layout events; single-ship groups use a small start marker.
    private static Control TimelineBar(double start, double end, double horizon)
    {
        var track = new Control { CustomMinimumSize = new Vector2(0, 9), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var bar = new ColorRect { Color = new Color(0.2f, 0.75f, 0.9f, 0.65f), MouseFilter = MouseFilterEnum.Ignore };
        track.AddChild(bar);
        track.Resized += () =>
        {
            float width = Math.Max(3, track.Size.X);
            float left = Math.Min(width - 3, (float)(start / horizon) * width);
            bar.Position = new Vector2(left, 1);
            bar.Size = new Vector2(Math.Min(width - left, Math.Max(3, (float)((end - start) / horizon) * width)), 7);
        };
        return track;
    }

    // =========================================================
    // Exact ship quotas per selector; random physical portal/side splits are expectations, not guarantees.
    private string SourceDistribution(WaveBatchResource batch)
    {
        var entries = batch.Enemies?.Select(e => e == null ? null :
            WaveBuilder.Enemy(e.EnemyId, e.Weight, e.Portal, e.Shield, e.Stealth)).ToArray();
        int[] counts = WaveCatalog.CompositionCounts(WaveBuilder.Batch(batch.Count, batch.Interval, batch.After,
            entries ?? Array.Empty<WaveEnemyEntry>()));
        var totals = new Dictionary<string, int>();
        for (int i = 0; i < counts.Length; i++)
        {
            var entry = batch.Enemies[i];
            var source = entry.Source ?? batch.Source ?? Wave.Source;
            var options = source?.Capture() ?? SpawnSourceOptions.Legacy;
            options.Validate(entry.Portal);
            if (counts[i] == 0) continue;
            string label = !string.IsNullOrEmpty(entry.Portal) ? entry.Portal : options.Policy.ToString();
            if (options.Policy == SpawnSourcePolicy.EdgesOnly || options.Policy == SpawnSourcePolicy.AnyEligible)
                label += (string.IsNullOrEmpty(options.EdgeSector) ? "" : " / " + options.EdgeSector) +
                    $" (T/B/L/R weights {options.TopWeight:0.#}/{options.BottomWeight:0.#}/{options.LeftWeight:0.#}/{options.RightWeight:0.#})";
            totals[label] = totals.GetValueOrDefault(label) + counts[i];
        }
        return string.Join("; ", totals.Select(pair => $"{pair.Value} × {pair.Key}"));
    }
}
#endif
