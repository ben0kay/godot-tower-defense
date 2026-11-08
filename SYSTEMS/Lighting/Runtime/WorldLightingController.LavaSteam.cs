// Sparse lava wisps borrow world-owned GPU emitters, selected on the existing lighting refresh.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private sealed class SteamSlot(GpuParticles2D emitter)
    {
        public readonly GpuParticles2D Emitter = emitter;
        public Vector2? Anchor;
    }

    private readonly List<Vector2> _lavaAnchors = new();
    private readonly List<Vector2> _steamSelection = new();
    private readonly List<SteamSlot> _steamSlots = new();
    private Texture2D _steamTexture;
    private ParticleProcessMaterial _steamProcess;
    private CanvasItemMaterial _steamMaterial;

    // =========================================================
    // Keep emitters at admitted anchors until they leave view or the terrain cache changes.
    private bool RefreshLavaSteam()
    {
        int cap = _lightingEnabled && Config.LavaSteamEnabled && Config.LavaSteamOpacity > 0f
            ? Mathf.Clamp(Config.MaxLavaSteamEmitters, 0, 8) : 0;
        _steamSelection.Clear();
        if (cap > 0 && _lavaAnchors.Count > 0)
        {
            Transform2D canvas = GetCanvasTransform();
            Rect2 viewport = GetViewportRect().Grow(Mathf.Max(0f, Config.ScreenMarginPixels));
            foreach (Vector2 anchor in _lavaAnchors)
            {
                if (!OverlapsLight(anchor, 150f, canvas, viewport)) continue;
                _steamSelection.Add(anchor);
                if (_steamSelection.Count >= cap) break;
            }
        }

        foreach (SteamSlot slot in _steamSlots)
        {
            if (!slot.Anchor.HasValue || _steamSelection.Remove(slot.Anchor.Value)) continue;
            slot.Anchor = null;
            slot.Emitter.Emitting = false;
            slot.Emitter.Hide();
        }
        foreach (Vector2 anchor in _steamSelection)
        {
            SteamSlot slot = AcquireSteamSlot();
            if (slot == null) break;
            slot.Anchor = anchor;
            slot.Emitter.GlobalPosition = anchor;
            slot.Emitter.Show();
            slot.Emitter.Emitting = true;
            slot.Emitter.Restart();
        }
        return cap > 0 && _lavaAnchors.Count > 0;
    }

    // =========================================================
    // Share the wisp texture, fade and motion across all emitters. Allocate only when needed.
    private SteamSlot AcquireSteamSlot()
    {
        foreach (SteamSlot slot in _steamSlots)
            if (!slot.Anchor.HasValue) return slot;
        _steamTexture ??= GD.Load<Texture2D>("res://WORLD/TERRAIN/Tiles/Interactive/Lava/SteamWisp.svg");
        if (_steamTexture == null) return null;

        float speed = Mathf.Clamp(Config.LavaSteamSpeed, 1f, 32f);
        _steamProcess ??= new ParticleProcessMaterial
        {
            Direction = new Vector3(.22f, -1f, 0f),
            Spread = 18f,
            Gravity = Vector3.Zero,
            InitialVelocityMin = speed * .65f,
            InitialVelocityMax = speed * 1.25f,
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(19f, 13f, 0f),
            ScaleMin = .5f,
            ScaleMax = 1f,
            ColorRamp = new GradientTexture1D
            {
                Gradient = new Gradient
                {
                    Offsets = new[] { 0f, .28f, .6f, 1f },
                    Colors = new[] { new Color(1, 1, 1, 0), Colors.White,
                        new Color(1, 1, 1, .65f), new Color(1, 1, 1, 0) }
                }
            }
        };
        _steamProcess.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        _steamMaterial ??= new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        var emitter = new GpuParticles2D
        {
            Emitting = false,
            Amount = Mathf.Clamp(Config.LavaSteamDensity, 1, 12),
            Lifetime = 2.4,
            Randomness = .55f,
            FixedFps = 24,
            LocalCoords = true,
            Texture = _steamTexture,
            Material = _steamMaterial,
            ProcessMaterial = _steamProcess,
            Modulate = new Color(1, 1, 1, Mathf.Clamp(Config.LavaSteamOpacity, 0f, .5f)),
            VisibilityRect = new Rect2(-150f, -150f, 300f, 300f),
            ZIndex = 2
        };
        AddChild(emitter);
        var created = new SteamSlot(emitter);
        _steamSlots.Add(created);
        return created;
    }
}
