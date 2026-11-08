// Captures editor preview art and authored combat capabilities from off-tree enemy scenes.
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

internal sealed class WaveEnemyPreview
{
    public Texture2D Texture { get; private set; }
    public double WeaponDps { get; private set; }
    public List<(string Id, int Count, bool Shield)> Payloads { get; } = new();
    public List<string> Omissions { get; } = new();

    // =========================================================
    // Inspect once per catalog refresh. Never attach the enemy or retain its nodes.
    public static WaveEnemyPreview Capture(PackedScene scene)
    {
        var preview = new WaveEnemyPreview();
        Node root = scene.Instantiate();
        try
        {
            var sprite = root.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Texture != null)
            {
                Rect2 region = sprite.RegionEnabled ? sprite.RegionRect : new Rect2(Vector2.Zero, sprite.Texture.GetSize());
                Vector2 frameSize = region.Size / new Vector2(sprite.Hframes, sprite.Vframes);
                preview.Texture = sprite.RegionEnabled || sprite.Hframes > 1 || sprite.Vframes > 1
                    ? new AtlasTexture { Atlas = sprite.Texture, Region = new Rect2(region.Position + (Vector2)sprite.FrameCoords * frameSize, frameSize) }
                    : sprite.Texture;
            }
            preview.CaptureCapabilities(root);
            return preview;
        }
        finally { root.Free(); }
    }

    // =========================================================
    // Each enabled minigun mount fires one projectile per shot, including the four Capital mounts.
    private void CaptureCapabilities(Node node)
    {
        switch (node)
        {
            case EnemyWeaponMount { Enabled: true } mount:
                if (mount is EnemyMinigun && mount.Config is EnemyMinigunConfig config &&
                    config.TryCapture(out var gun) && config.TryCaptureBurst(out double burst, out double rest) &&
                    !mount.PivotPath.IsEmpty && !mount.MuzzlePath.IsEmpty &&
                    mount.GetNodeOrNull<Node2D>(mount.PivotPath) is Node2D pivot &&
                    mount.GetNodeOrNull<Marker2D>(mount.MuzzlePath) is Marker2D muzzle &&
                    mount.IsAncestorOf(pivot) && pivot.IsAncestorOf(muzzle))
                    WeaponDps += (double)gun.Damage * gun.ShotsPerSecond * (config.UseBursts ? burst / (burst + rest) : 1);
                else Omissions.Add("unmodelled weapon " + mount.Name);
                break;
            case DeathSpawn { Enabled: true } release:
                if (release.Config is { IsValid: true } payload)
                    Payloads.Add((payload.ChildEnemyId, payload.ChildCount, payload.SpawnWithShield));
                else Omissions.Add("invalid death-spawn configuration");
                break;
            case ShieldEmitter { Enabled: true } emitter:
                if (emitter.Config is { IsValid: true } shield)
                {
                    if (shield.ShieldPerPulse > 0)
                        Omissions.Add($"shield support ({shield.ShieldPerPulse:0.#} HP per pulse / {shield.PulseIntervalSeconds:0.#}s; depends on nearby eligible allies)");
                }
                else Omissions.Add("invalid shield-emitter configuration");
                break;
            default:
                if (node.GetParent()?.Name.ToString() == "Abilities" && node is not (DeathSpawn or ShieldEmitter))
                    Omissions.Add("unmodelled ability " + node.Name);
                break;
        }
        foreach (Node child in node.GetChildren()) CaptureCapabilities(child);
    }
}
#endif
