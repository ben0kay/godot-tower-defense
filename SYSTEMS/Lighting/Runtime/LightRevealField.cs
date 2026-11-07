// World-space reveal disks shared by combat queries and the bounded visual mask raster.
using Godot;
using System;
using System.Collections.Generic;

internal sealed class LightRevealField
{
    internal readonly record struct Disk(Vector2 Centre, float Radius, bool LiftsGround)
    {
        internal float Alpha(float distanceSquared)
        {
            float feather = Mathf.Min(32, Radius * .4f);
            float t = Mathf.Clamp((Radius - Mathf.Sqrt(distanceSquared)) / feather, 0, 1);
            return t * t * (3 - 2 * t);
        }
    }

    private const int BucketSize = 256;
    private readonly Dictionary<Vector2I, List<Disk>> _buckets = new();
    private readonly List<Disk> _wide = new(), _disks = new();

    internal void Clear()
    {
        foreach (var bucket in _buckets.Values) bucket.Clear();
        _wide.Clear();
        _disks.Clear();
    }

    // =========================================================
    // Static updates rebuild on source events. Large disks avoid an unbounded bucket expansion.
    internal void Add(Vector2 centre, float radius, bool liftsGround = false)
    {
        if (!centre.IsFinite() || !float.IsFinite(radius) || radius <= 0) return;
        Disk disk = new(centre, radius, liftsGround);
        _disks.Add(disk);
        if (radius > 2048) { _wide.Add(disk); return; }
        Vector2I min = Bucket(centre - Vector2.One * radius), max = Bucket(centre + Vector2.One * radius);
        for (int y = min.Y; y <= max.Y; y++)
            for (int x = min.X; x <= max.X; x++)
            {
                Vector2I key = new(x, y);
                if (!_buckets.TryGetValue(key, out var bucket)) _buckets.Add(key, bucket = new());
                bucket.Add(disk);
            }
    }

    internal bool Contains(Vector2 point) => point.IsFinite() &&
        (Contains(_wide, point) || (_buckets.TryGetValue(Bucket(point), out var bucket) && Contains(bucket, point)));

    internal float AlphaAt(Vector2 point)
    {
        if (!point.IsFinite()) return 0;
        float alpha = AlphaAt(_wide, point);
        if (alpha < 1 && _buckets.TryGetValue(Bucket(point), out var bucket))
            alpha = Mathf.Max(alpha, AlphaAt(bucket, point));
        return alpha;
    }

    private static bool Contains(List<Disk> disks, Vector2 point)
    {
        foreach (Disk disk in disks)
            if (disk.Centre.DistanceSquaredTo(point) <= disk.Radius * disk.Radius) return true;
        return false;
    }

    private static float AlphaAt(List<Disk> disks, Vector2 point)
    {
        float alpha = 0;
        foreach (Disk disk in disks)
        {
            float distance = disk.Centre.DistanceSquaredTo(point);
            if (distance >= disk.Radius * disk.Radius) continue;
            alpha = Mathf.Max(alpha, disk.Alpha(distance));
            if (alpha >= 1) break;
        }
        return alpha;
    }

    // =========================================================
    // Red stores reveal and green stores tower range lift. Max compositing prevents overlapping brightness buildup.
    internal void Rasterize(Rect2 bounds, int width, int height, byte[] pixels)
    {
        Array.Clear(pixels);
        Vector2 step = bounds.Size / new Vector2(width, height);
        foreach (Disk disk in _disks)
        {
            Vector2 centre = disk.Centre - bounds.Position;
            float radius = disk.Radius, squared = radius * radius;
            int left = Mathf.Clamp(Mathf.FloorToInt((centre.X - radius) / step.X), 0, width - 1);
            int right = Mathf.Clamp(Mathf.CeilToInt((centre.X + radius) / step.X), 0, width - 1);
            int top = Mathf.Clamp(Mathf.FloorToInt((centre.Y - radius) / step.Y), 0, height - 1);
            int bottom = Mathf.Clamp(Mathf.CeilToInt((centre.Y + radius) / step.Y), 0, height - 1);
            for (int y = top; y <= bottom; y++)
            {
                float dy = (y + .5f) * step.Y - centre.Y;
                for (int x = left; x <= right; x++)
                {
                    int index = (y * width + x) * 2;
                    if (pixels[index] == 255 && (!disk.LiftsGround || pixels[index + 1] == 255)) continue;
                    float dx = (x + .5f) * step.X - centre.X;
                    float distance = dx * dx + dy * dy;
                    if (distance >= squared) continue;
                    byte value = (byte)(disk.Alpha(distance) * 255);
                    if (value > pixels[index]) pixels[index] = value;
                    if (disk.LiftsGround && value > pixels[index + 1]) pixels[index + 1] = value;
                }
            }
        }
    }

    private static Vector2I Bucket(Vector2 point) =>
        new(Mathf.FloorToInt(point.X / BucketSize), Mathf.FloorToInt(point.Y / BucketSize));
}
