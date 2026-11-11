// Shared artwork and line geometry for the active construction hologram canvas.
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public partial class ConstructionSiteHologram
{
    private readonly record struct RingArtwork(Texture2D Texture, Rect2 Bounds, Rect2 Region);
    private const int ArtworkPixelsPerUnit = 3;
    private sealed record ScannerArtwork(RingArtwork Outer, RingArtwork Inner);
    private sealed record FootprintLines(Vector2[] Cross, Vector2[] Brackets, Vector2[] ScannerCross);
    private readonly Dictionary<float, ScannerArtwork> _scannerArtwork = new();
    private readonly Dictionary<Vector2, FootprintLines> _footprintLines = new();
    private float _artInset = -1, _artCorner = -1, _telemetryRadius = -1;
    private RingArtwork _telemetryOuter, _telemetryMiddle, _telemetryInner;
    private readonly Vector2[] _telemetryTrack = new Vector2[56];
    private static readonly Vector2[] AcquisitionDirections =
        { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up };

    // =========================================================
    // One set of telemetry textures serves every site and completion readout in this world.
    private void EnsureTelemetryArtwork(float radius)
    {
        if (_telemetryRadius == radius) return;
        _telemetryRadius = radius;
        var outer = new StringBuilder();
        AppendArc(outer, radius + 1, 0, 2.25f, 32, 1.6f, "#c4ffff", .93f);
        for (int i = 0; i < 20; i++)
        {
            float fraction = i / 19f;
            Vector2 point = Vector2.FromAngle(fraction * 2.4f - .25f) * (radius + 3 + (i % 4) * 2.1f);
            outer.Append(CultureInfo.InvariantCulture,
                $"<circle cx=\"{point.X}\" cy=\"{point.Y}\" r=\"{(i % 6 == 0 ? 1.05f : .6f)}\" fill=\"#59f7ff\" opacity=\"{.16f + .54f * (1 - fraction)}\"/>");
        }
        var middle = new StringBuilder();
        var inner = new StringBuilder();
        AppendArc(middle, radius * .75f, 0, 4.4f, 44, 1.55f);
        AppendArc(inner, radius * .59f, 0, 2.85f, 30, 1.25f);
        RingArtwork[] rings = BakeRings((outer, radius + 12), (middle, radius * .75f + 2), (inner, radius * .59f + 2));
        _telemetryOuter = rings[0]; _telemetryMiddle = rings[1]; _telemetryInner = rings[2];
        FillArcPoints(_telemetryTrack, radius * .82f, 0, Mathf.Tau);
    }

    // =========================================================
    // Both broken engineering rings share an atlas baked at the actual footprint radius.
    private ScannerArtwork GetScannerArtwork(float radius)
    {
        if (_scannerArtwork.TryGetValue(radius, out var artwork)) return artwork;
        var outer = new StringBuilder();
        var inner = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.Tau / 10;
            AppendArc(outer, radius, angle, .40f, 5, 1.25f);
            AppendArc(inner, radius * .7f, -angle, .24f, 4, 1f);
        }
        RingArtwork[] rings = BakeRings((outer, radius + 2), (inner, radius * .7f + 2));
        artwork = new ScannerArtwork(rings[0], rings[1]);
        _scannerArtwork.Add(radius, artwork);
        return artwork;
    }

    private FootprintLines GetFootprintLines(Vector2 size, float radius)
    {
        if (_footprintLines.TryGetValue(size, out var lines)) return lines;
        Vector2 half = size * .5f;
        float bracket = Mathf.Min(_artCorner, Mathf.Min(size.X, size.Y) * .3f);
        Vector2 topLeft = -half, topRight = new(half.X, -half.Y), bottomLeft = new(-half.X, half.Y);
        Vector2[] brackets =
        {
            topLeft, topLeft + Vector2.Right * bracket, topLeft, topLeft + Vector2.Down * bracket,
            topRight, topRight + Vector2.Left * bracket, topRight, topRight + Vector2.Down * bracket,
            half, half + Vector2.Left * bracket, half, half + Vector2.Up * bracket,
            bottomLeft, bottomLeft + Vector2.Right * bracket, bottomLeft, bottomLeft + Vector2.Up * bracket
        };
        lines = new FootprintLines(CrossLines(half), brackets, CrossLines(Vector2.One * (radius * .4f)));
        _footprintLines.Add(size, lines);
        return lines;
    }

    private static Vector2[] CrossLines(Vector2 half) =>
        new[] { Vector2.Left * half.X, Vector2.Right * half.X, Vector2.Up * half.Y, Vector2.Down * half.Y };

    // =========================================================
    // SVG rasterization runs once per design at three pixels per world unit.
    // Match the original sampled arc vertices and stroke widths.
    private static void AppendArc(StringBuilder svg, float radius, float start, float sweep, int points,
        float width, string color = "white", float opacity = 1)
    {
        svg.Append(CultureInfo.InvariantCulture,
            $"<polyline fill=\"none\" stroke=\"{color}\" stroke-opacity=\"{opacity}\" stroke-width=\"{width}\" points=\"");
        for (int i = 0; i < points; i++)
        {
            Vector2 point = Vector2.FromAngle(start + sweep * i / (points - 1)) * radius;
            svg.Append(CultureInfo.InvariantCulture, $"{point.X},{point.Y} ");
        }
        svg.Append("\"/>");
    }

    // =========================================================
    // One atlas lets consecutive ring commands share a texture while rotating independently.
    private static RingArtwork[] BakeRings(params (StringBuilder Body, float Radius)[] designs)
    {
        var rings = new RingArtwork[designs.Length];
        var body = new StringBuilder();
        int width = 0, height = 0;
        for (int i = 0; i < designs.Length; i++)
        {
            var design = designs[i];
            int half = Mathf.CeilToInt(design.Radius), size = half * 2;
            body.Append(CultureInfo.InvariantCulture,
                $"<g transform=\"translate({width + half} {half})\">{design.Body}</g>");
            rings[i] = new RingArtwork(null, new Rect2(-half, -half, size, size),
                new Rect2(width * ArtworkPixelsPerUnit, 0, size * ArtworkPixelsPerUnit, size * ArtworkPixelsPerUnit));
            width += size;
            height = Mathf.Max(height, size);
        }
        string svg = FormattableString.Invariant(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\">{body}</svg>");
        using Image image = new();
        if (image.LoadSvgFromString(svg, ArtworkPixelsPerUnit) != Error.Ok) return rings;
        var texture = ImageTexture.CreateFromImage(image);
        for (int i = 0; i < rings.Length; i++) rings[i] = rings[i] with { Texture = texture };
        return rings;
    }

    private void DrawRotatingRing(RingArtwork ring, Vector2 center, float angle, Vector2 scale, Color tint, CanvasItem target = null)
    {
        if (ring.Texture == null) return;
        target ??= this;
        target.DrawSetTransform(center, angle, scale);
        target.DrawTextureRectRegion(ring.Texture, ring.Bounds, ring.Region, tint);
    }

    // =========================================================
    // Progress uses two angle evaluations per change and reuses the same point buffer.
    private static void FillArcPoints(Vector2[] points, float radius, float start, float sweep)
    {
        Vector2 point = Vector2.FromAngle(start) * radius;
        Vector2 step = Vector2.FromAngle(sweep / (points.Length - 1));
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = point;
            point = new Vector2(point.X * step.X - point.Y * step.Y, point.X * step.Y + point.Y * step.X);
        }
    }
}
