// Selected-wave analytics and sprite preview. All values are derived on authoring events.
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WaveEditorPanel
{
    private Dictionary<string, WaveEnemyPreview> _enemyPreviews = new(StringComparer.Ordinal);
    private Label _waveStats, _fleetThreat, _threatBreakdown, _enemyStats;
    private TextureRect _enemySprite;

    // =========================================================
    // Keep the analysis in the selected wave pane and reuse its labels across edits.
    private void BuildAnalysis(VBoxContainer parent)
    {
        var pane = Pane("Wave analysis · authored totals", 0);
        parent.AddChild(pane);
        _waveStats = AnalysisLabel(pane);
        _fleetThreat = AnalysisLabel(pane);
        _fleetThreat.TooltipText = "Relative ship-fleet threat only. Higher is harder. The index has no cap and does not predict wins. Terrain, allowed towers, upgrades, ammunition, economy, spawn blockage and player strategy are excluded.";
        _threatBreakdown = AnalysisLabel(pane);
    }

    private static Label AnalysisLabel(Container parent)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(label);
        return label;
    }

    // =========================================================
    // Render the registered sprite's current authored frame without running its scene.
    private void BuildEnemyPreview(VBoxContainer parent)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);
        _enemySprite = new TextureRect { CustomMinimumSize = new Vector2(72, 72),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        row.AddChild(_enemySprite);
        _enemyStats = AnalysisLabel(row);
    }

    private void RefreshEnemyPreview()
    {
        if (Entry == null || _enemySprite == null) return;
        string id = Entry.EnemyId ?? "";
        var definition = _enemyCatalog?.DefinitionFor(id);
        _enemyPreviews.TryGetValue(id, out var preview);
        _enemySprite.Texture = preview?.Texture;
        _enemySprite.Visible = _enemySprite.Texture != null;
        _enemyStats.Text = definition == null ? "Preview unavailable · unregistered or missing enemy ID."
            : $"{definition.DisplayName}\nHull {definition.MaxHealth:0.#} HP · Max shield {definition.MaxShield:0.#} HP " +
              $"({(Entry.Shield ? definition.MaxShield : 0):0.#} at spawn)\n" +
              $"Speed {definition.MoveSpeed:0.#} px/s · Citadel damage {definition.CitadelDamage}" +
              (_enemySprite.Visible ? "" : "\nSprite unavailable");
    }

    private void AnalysisUnavailable(string message)
    {
        _waveStats.Text = "Analysis unavailable · " + message;
        _fleetThreat.Text = "FLEET THREAT · UNAVAILABLE";
        _threatBreakdown.Text = "";
    }

    // =========================================================
    // Share quota and timing rows with the existing timeline, with no per-ship allocation.
    private void RefreshWaveAnalysis(List<ScheduledBatch> rows)
    {
        if (rows.Count == 0 || Wave.Number <= 0 || _source.Waves.Count(w => w?.Number == Wave.Number) != 1
            || !WaveCatalog.ValidNextWaveDelay(Wave.NextWaveDelaySeconds))
        { AnalysisUnavailable("A valid wave needs a unique positive number, a valid next-wave delay and at least one batch."); return; }

        var types = new Dictionary<string, long>(StringComparer.Ordinal);
        var omissions = new SortedSet<string>(StringComparer.Ordinal);
        var threat = new ThreatParts();
        double hull = 0, shields = 0;
        long ships = 0, shieldOnly = 0, stealthOnly = 0, both = 0, breach = 0, children = 0;
        foreach (var row in rows)
        {
            ships += row.Batch.Count;
            if (ships > int.MaxValue) { AnalysisUnavailable("Wave exceeds the supported ship count."); return; }
            for (int i = 0; i < row.Quotas.Length; i++)
            {
                var entry = row.Batch.Enemies[i];
                string id = entry.EnemyId ?? "";
                var definition = _enemyCatalog?.DefinitionFor(id);
                if (definition == null)
                { AnalysisUnavailable("Missing or unregistered enemy ID " + (id.Length == 0 ? "(empty)" : id)); return; }

                int count = row.Quotas[i];
                if (count == 0) continue;
                types[id] = types.GetValueOrDefault(id) + count;
                bool shielded = entry.Shield && definition.MaxShield > 0;
                if (shielded && entry.Stealth) both += count;
                else if (shielded) shieldOnly += count;
                else if (entry.Stealth) stealthOnly += count;
                hull += (double)definition.MaxHealth * count;
                if (shielded) shields += (double)definition.MaxShield * count;
                breach += (long)definition.CitadelDamage * count;

                threat.AddShip(definition, shielded, entry.Stealth, count, WeaponDps(id, omissions));
                if (!_enemyPreviews.TryGetValue(id, out var preview)) continue;
                foreach (var payload in preview.Payloads)
                {
                    long childCount = (long)count * payload.Count;
                    children += childCount;
                    var child = _enemyCatalog.DefinitionFor(payload.Id);
                    if (child == null)
                    { omissions.Add(id + ": unregistered death-spawn child " + payload.Id); continue; }
                    var payloadThreat = new ThreatParts();
                    payloadThreat.AddShip(child, payload.Shield, false, childCount, WeaponDps(payload.Id, omissions));
                    threat.Payload += .5 * payloadThreat.Total;
                }
            }
        }

        double first = rows.Min(row => row.Start), last = rows.Max(row => row.End);
        double pace = Math.Clamp(Math.Sqrt(ships / Math.Max(1, last - first + 1)), .8, 1.45);
        double overlap = OpeningPressure(rows);
        double index = Math.Round(threat.Total * pace * overlap, 1, MidpointRounding.AwayFromZero);
        string mix = string.Join(" · ", types.Select(pair => $"{pair.Value} × {EnemyLabel(pair.Key)}"));
        _waveStats.Text = $"{ships} ships · {rows.Count} batches · spawn window {first:0.##}–{last:0.##}s ({last - first:0.##}s span)\n" +
            $"Hull {hull:0.#} HP · Starting shield {shields:0.#} HP · Potential Citadel damage {breach}\n" +
            $"Shield only {shieldOnly} · Stealth only {stealthOnly} · Both {both} · Neither {ships - shieldOnly - stealthOnly - both}\n" +
            mix + (children > 0 ? $"\nUp to {children} death-spawn children outside these authored totals." : "");
        _waveStats.TooltipText = "Theoretical authored totals. Blocked exits, portal opening and spawn budgets may delay spawns. Movement, combat and death-spawn children can extend the live wave.";
        _fleetThreat.Text = $"FLEET THREAT · {index:0.0}" + (omissions.Count == 0 ? "" : " · PARTIAL");
        _threatBreakdown.Text = $"v1 points · Durability {threat.Durability:0.#} · Leak {threat.Leak:0.#} · Speed {threat.Speed:0.#} · " +
            $"Weapons {threat.Weapons:0.#} · Stealth {threat.Stealth:0.#} · Payload {threat.Payload:0.#}\n" +
            $"Cadence ×{pace:0.00} · Parallel openings ×{overlap:0.00}" +
            (omissions.Count == 0 ? "" : "\nExcluded: " + string.Join("; ", omissions));
    }

    private double WeaponDps(string id, SortedSet<string> omissions)
    {
        var definition = _enemyCatalog.DefinitionFor(id);
        if (definition.StatusImmunities?.Count > 0) omissions.Add(id + ": status immunities");
        if (!_enemyPreviews.TryGetValue(id, out var preview))
        { omissions.Add(id + ": weapon and ability data unavailable"); return 0; }
        foreach (string omission in preview.Omissions) omissions.Add(id + ": " + omission);
        return preview.WeaponDps;
    }

    // =========================================================
    // Parallel starts within one second add pressure independently of group duration.
    // Shortening a group's interval cannot lower this multiplier by shortening its overlap.
    private double OpeningPressure(List<ScheduledBatch> rows)
    {
        if (Wave.Schedule != WaveScheduleMode.Parallel || rows.Count < 2) return 1;
        double[] starts = rows.Select(row => row.Start).OrderBy(start => start).ToArray();
        int left = 0, peak = 1;
        for (int right = 1; right < starts.Length; right++)
        {
            while (starts[right] - starts[left] > 1) left++;
            peak = Math.Max(peak, right - left + 1);
        }
        return 1 + Math.Min(.25, .08 * (peak - 1));
    }

    private struct ThreatParts
    {
        public double Durability, Leak, Speed, Weapons, Stealth, Payload;
        public readonly double Total => Durability + Leak + Speed + Weapons + Stealth + Payload;

        // =========================================================
        // Fixed v1 calibration. Enemy Armor is metadata until the runtime damage path uses it.
        public void AddShip(EnemyDefinition definition, bool shield, bool stealth, long count, double dps)
        {
            double basePoints = 5.0 * count;
            double durability = .45 * Math.Sqrt(((double)definition.MaxHealth + (shield ? definition.MaxShield : 0)) / 60);
            double leak = .25 * Math.Sqrt((1.0 + definition.CitadelDamage) / 2);
            double speed = .15 * Math.Clamp(Math.Sqrt(definition.MoveSpeed / 100.0), .5, 2);
            double weapons = .15 * (1 + Math.Min(3, dps / 15));
            Durability += basePoints * durability;
            Leak += basePoints * leak;
            Speed += basePoints * speed;
            Weapons += basePoints * weapons;
            if (stealth) Stealth += .1 * basePoints * (durability + leak + speed + weapons);
        }
    }
}
#endif
