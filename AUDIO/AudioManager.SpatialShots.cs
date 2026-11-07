// Viewport-aware range, zoom gain and bounded overlap suppression for positional one-shots.
using Godot;
using System.Collections.Generic;

public partial class AudioManager
{
    private readonly record struct WorldShotMix(float Distance, float ZoomDb, Transform2D Canvas);
    private readonly record struct RecentSpatialShot(string Id, ulong Source, Vector2 Position, ulong TimeMs);
    private readonly Queue<RecentSpatialShot> _recentSpatialShots = new();
    private const int RecentShotLimit = 128;

    // =========================================================
    // Scale native world distance with visible world coverage, with enough headroom to hear the edges.
    private bool TryGetWorldShotMix(AudioDefinition sound, Vector2 position, out WorldShotMix mix)
    {
        mix = default;
        if (!position.IsFinite()) return false;
        RefreshCamera();
        if (!GodotObject.IsInstanceValid(_camera)) return false;

        Viewport viewport = GetViewport();
        Transform2D canvas = viewport.GetCanvasTransform();
        Rect2 screen = viewport.GetVisibleRect().Grow(Mathf.Max(0, Config.ScreenMarginPixels));
        Vector2 screenPoint = canvas * position;
        bool inside = screen.HasPoint(screenPoint);
        if (Config.ScreenCulling && !sound.AllowOffscreen && !inside) return false;

        float zoom = Mathf.Max(.01f, Mathf.Min(_camera.Zoom.X, _camera.Zoom.Y));
        float viewRadius = screen.Size.Length() * .5f / zoom;
        float reach = Mathf.Max(Mathf.Max(1, sound.MaxDistance), viewRadius * Mathf.Max(1.1f, Config.ViewHearingReach));
        if (Config.DistanceCulling && !inside && position.DistanceSquaredTo(_camera.GetScreenCenterPosition()) >= reach * reach)
            return false;

        float zoomDb = Mathf.Clamp(Mathf.Log(Mathf.Min(1, zoom)) / Mathf.Log(2) * Mathf.Max(0, Config.ZoomOutDbPerOctave), -15, 0);
        mix = new WorldShotMix(reach, zoomDb, canvas);
        return true;
    }

    // =========================================================
    // Drop only accepted, almost simultaneous duplicates from nearby emitters. The same gun keeps its cadence.
    private bool IsNearbyDuplicate(string id, ulong source, Vector2 position, Transform2D canvas, ulong now)
    {
        int window = Mathf.Clamp(Config.NearbyShotWindowMs, 0, 150);
        if (window == 0) return false;
        while (_recentSpatialShots.TryPeek(out RecentSpatialShot first) && now - first.TimeMs >= (ulong)window)
            _recentSpatialShots.Dequeue();

        float worldRadius = Mathf.Max(0, Config.NearbyShotWorldRadius);
        float screenRadius = Mathf.Max(0, Config.NearbyShotScreenRadius);
        if (worldRadius == 0 || screenRadius == 0) return false;
        Vector2 screenPosition = canvas * position;
        foreach (RecentSpatialShot recent in _recentSpatialShots)
            if (recent.Id == id && (source == 0 || recent.Source != source) &&
                recent.Position.DistanceSquaredTo(position) <= worldRadius * worldRadius &&
                (canvas * recent.Position).DistanceSquaredTo(screenPosition) <= screenRadius * screenRadius)
                return true;
        return false;
    }

    private void RememberShot(string id, ulong source, Vector2 position, ulong now)
    {
        if (Config.NearbyShotWindowMs <= 0) return;
        if (_recentSpatialShots.Count == RecentShotLimit) _recentSpatialShots.Dequeue();
        _recentSpatialShots.Enqueue(new RecentSpatialShot(id, source, position, now));
    }
}
