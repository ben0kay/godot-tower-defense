// Bounded corridor sampling over accepted logical waypoints; no A*, shared weights or per-tick randomness.
using Godot;
using System;

public static class CorridorRouteRefinement
{
    #region Sparse Preference Anchors
    // =========================================================
    // Preserve baseline/endpoints; sparse width probes and stable spatial noise produce coherent individual deviations.
    public static bool TryBuild(Vector2[] logical, Vector2[] baseline, Vector2 start, NavigationGrid nav,
        float radius, uint seed, float strength, int spacing, Vector2 forward, out Vector2[] result, out int samples)
    {
        result = baseline; samples = 0;
        if (logical.Length < 4 || strength <= 0) return false;
        float cell = nav.Grid.Config.CellSize;
        float[] offsets = new float[logical.Length];
        int previous = 0; float previousOffset = 0; bool wide = false;
        for (int i = 1; i < logical.Length - 1; i += spacing)
        {
            Vector2 lateral = Lateral(logical, i);
            float preferred = (baseline[i] - logical[i]).Dot(lateral);
            if (lateral != Vector2.Zero)
            {
                float positive = Width(logical[i], lateral, nav, radius, cell, ref samples);
                float negative = Width(logical[i], -lateral, nav, radius, cell, ref samples);
                // Less than 1.5 physical cells is a narrow passage: retain the existing tiny bias.
                if (positive + negative + radius * 2 >= cell * 1.5f)
                {
                    wide = true;
                    float preference = Preference(logical[i], seed, cell * spacing);
                    float extent = preference >= 0 ? positive : negative;
                    preferred = preference * extent * strength;
                }
            }
            Fill(offsets, previous, i, previousOffset, preferred);
            previous = i; previousOffset = preferred;
        }
        if (!wide) return false;
        Fill(offsets, previous, logical.Length - 1, previousOffset, 0);
        result = (Vector2[])baseline.Clone();
        bool changed = false;
        for (int i = 1; i < logical.Length - 1; i++)
        {
            // Taper the start/goal and avoid modifying short handoff elbows.
            float incoming = logical[i].DistanceTo(logical[i - 1]);
            if (incoming < cell * 0.25f) continue;
            Vector2 lateral = Lateral(logical, i);
            float envelope = Mathf.Min(1, Mathf.Min(i, logical.Length - 1 - i) / 2f);
            Vector2 shift = lateral * offsets[i] * envelope;
            Vector2 logicalForward = logical[i] - logical[i - 1];
            for (float scale = 1; scale >= 0.125f; scale *= 0.5f)
            {
                samples++;
                Vector2 candidate = logical[i] + shift * scale;
                if (i == 1 && forward != Vector2.Zero && (candidate - start).Dot(forward) < -0.01f) continue;
                Vector2 link = candidate - result[i - 1];
                if (link.Dot(logicalForward) < logicalForward.LengthSquared() * 0.25f || link.Length() > incoming * 1.35f) continue;
                if (!nav.HasClearance(candidate, radius) || !CorridorClearance.ConnectionClear(nav, result[i - 1], candidate, radius) ||
                    !CorridorClearance.ConnectionClear(nav, candidate, baseline[i + 1], radius)) continue;
                result[i] = candidate; changed |= candidate.DistanceSquaredTo(baseline[i]) > 0.01f; break;
            }
        }
        // Candidate lookahead makes fallback safe; verify the final complete chain, including untouched elbows/endpoints.
        Vector2 from = start;
        for (int i = 0; i < result.Length; i++)
        {
            samples++;
            if (!CorridorClearance.ConnectionClear(nav, from, result[i], radius)) { result = baseline; return false; }
            from = result[i];
        }
        return changed;
    }

    // =========================================================
    // Seven probes maximum per side, up to two cells; never skip through an intervening solid wall.
    private static float Width(Vector2 point, Vector2 side, NavigationGrid nav, float radius, float cell, ref int samples)
    {
        float low = 0, high = cell * 2;
        bool blocked = false;
        for (int step = 1; step <= 4; step++)
        {
            float distance = cell * 0.5f * step; samples++;
            if (!CorridorClearance.ConnectionClear(nav, point, point + side * distance, radius)) { high = distance; blocked = true; break; }
            low = distance;
        }
        if (!blocked) return low;
        for (int step = 0; step < 3; step++)
        {
            float middle = (low + high) * 0.5f; samples++;
            if (CorridorClearance.ConnectionClear(nav, point, point + side * middle, radius)) low = middle; else high = middle;
        }
        return low;
    }
    // =========================================================
    // Interpolate anchor choices, rather than generating independent per-cell/per-frame noise.
    private static void Fill(float[] values, int from, int to, float start, float end)
    { for (int i = from; i <= to; i++) values[i] = Mathf.Lerp(start, end, to == from ? 0 : (float)(i - from) / (to - from)); }
    // =========================================================
    // Existing incoming/outgoing bisector gives a smooth local sideways basis through turns.
    private static Vector2 Lateral(Vector2[] logical, int index)
    {
        Vector2 direction = (logical[index] - logical[index - 1]).Normalized() + (logical[index + 1] - logical[index]).Normalized();
        return direction.LengthSquared() < 0.01f ? Vector2.Zero : direction.Normalized().Orthogonal();
    }
    #endregion

    #region Stable Spatial Noise
    // =========================================================
    // A stable ship bias plus smooth world-space noise persists across repaths without a fixed lane count.
    private static float Preference(Vector2 point, uint seed, float patch)
    {
        float x = point.X / patch, y = point.Y / patch;
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float tx = Mathf.SmoothStep(0, 1, x - ix), ty = Mathf.SmoothStep(0, 1, y - iy);
        float noise = Mathf.Lerp(Mathf.Lerp(Noise(ix, iy, seed), Noise(ix + 1, iy, seed), tx),
            Mathf.Lerp(Noise(ix, iy + 1, seed), Noise(ix + 1, iy + 1, seed), tx), ty);
        float bias = Signed(CorridorPreference.Hash(seed ^ 0x91e10da5u));
        return Mathf.Clamp(bias * 0.7f + noise * 0.3f, -1, 1);
    }
    // =========================================================
    // Broad deterministic values depend on the ship's own seed and physical region, not route array index.
    private static float Noise(int x, int y, uint seed) => Signed(CorridorPreference.Hash(seed ^
        unchecked((uint)x * 374761393u) ^ unchecked((uint)y * 668265263u)));
    // =========================================================
    // Map the mixed integer into a continuous signed preference.
    private static float Signed(uint value) => (value & 0x00ffffffu) / 16777215f * 2 - 1;
    #endregion
}
