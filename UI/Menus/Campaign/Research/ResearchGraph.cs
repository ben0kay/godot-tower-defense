// Lightweight hex graph: one drawing surface, shared axial transform for paint/hit tests, pan and cursor zoom.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ResearchGraph : Control
{
    public event Action<string> SelectionChanged;
    public IReadOnlyDictionary<string, ResearchNode> Nodes { get; set; }
    public PlayerProgression Player { get; set; }
    public string SelectedId { get; private set; } = "research_core";
    public float Zoom { get; private set; } = 1f;
    public Vector2 Pan { get; private set; }
    private bool _dragging;
    private Vector2 _pressPosition;
    private bool _leftBackground;
    private const float Radius = 55;
    public static readonly Vector2I[] Directions = { new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1) };

    // =========================================================
    // A focusable graph supports keyboard browsing without requiring precise mouse clicks.
    public override void _Ready()
    {
        FocusMode = FocusModeEnum.All; MouseFilter = MouseFilterEnum.Stop; ClipContents = true;
        CustomMinimumSize = new Vector2(240, 240); MouseDefaultCursorShape = CursorShape.Cross;
        Resized += QueueRedraw; FocusEntered += QueueRedraw; FocusExited += QueueRedraw;
        TooltipText = "Select a hex. Middle-drag or drag empty space to pan; wheel to zoom. Arrow keys browse; Home resets.";
    }

    // =========================================================
    // Pointy hex axial coordinates provide six equally spaced expansion directions.
    public static Vector2 AxialPosition(Vector2I axial) => new(
        100 * Mathf.Sqrt(3) * (axial.X + axial.Y / 2f), 150 * axial.Y);
    public Vector2 GraphToScreen(Vector2 point) => Size / 2 + Pan + point * Zoom;
    public Vector2 ScreenToGraph(Vector2 point) => (point - Size / 2 - Pan) / Zoom;

    // =========================================================
    // Use identical node geometry for drawing and selection at every zoom/pan value.
    private static Vector2[] Hex(Vector2 center, float radius) => Enumerable.Range(0, 6)
        .Select(i => center + Vector2.FromAngle(Mathf.Pi / 3 * i - Mathf.Pi / 2) * radius).ToArray();
    public string HitNode(Vector2 screen)
    {
        if (Nodes == null) return null;
        Vector2 point = ScreenToGraph(screen);
        foreach (ResearchNode node in Nodes.Values)
            if (Geometry2D.IsPointInPolygon(point, Hex(AxialPosition(node.Axial), Radius))) return node.Id;
        return null;
    }

    // =========================================================
    // Zoom anchors the graph point under the cursor; clamp prevents unusably tiny/huge nodes.
    public void ZoomAt(Vector2 screen, float factor)
    {
        Vector2 before = ScreenToGraph(screen);
        Zoom = Mathf.Clamp(Zoom * factor, .45f, 1.8f);
        Pan = screen - Size / 2 - before * Zoom;
        QueueRedraw();
    }
    public void ResetView() { Pan = new Vector2(-80, 0); Zoom = 1; QueueRedraw(); }
    public void Select(string id)
    {
        if (Nodes == null || !Nodes.ContainsKey(id)) return;
        SelectedId = id; SelectionChanged?.Invoke(id); QueueRedraw();
    }

    // =========================================================
    // Panning never purchases; mouse/keyboard selection only updates the information panel.
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton button)
        {
            if (button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            { ZoomAt(button.Position, button.ButtonIndex == MouseButton.WheelUp ? 1.12f : 1 / 1.12f); AcceptEvent(); return; }
            if (button.ButtonIndex == MouseButton.Middle)
            { _dragging = button.Pressed; GrabFocus(); AcceptEvent(); }
            if (button.ButtonIndex == MouseButton.Left)
            {
                GrabFocus();
                if (button.Pressed)
                { _pressPosition = button.Position; _leftBackground = HitNode(button.Position) == null; _dragging = _leftBackground; }
                else
                {
                    if (!_leftBackground && button.Position.DistanceTo(_pressPosition) < 6)
                    { string id = HitNode(button.Position); if (id != null) Select(id); }
                    _dragging = false;
                }
                AcceptEvent();
            }
        }
        if (input is InputEventMouseMotion motion)
        {
            if (_dragging) { Pan += motion.Relative; QueueRedraw(); }
            string id = HitNode(motion.Position);
            TooltipText = id != null ? Nodes[id].Name + " — " + StatusText(Nodes[id]) :
                "Drag empty space to pan · Scroll to zoom · Arrows browse · Home resets";
        }
        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Home) ResetView();
            else if (key.Keycode is Key.Plus or Key.Equal or Key.KpAdd) ZoomAt(Size / 2, 1.12f);
            else if (key.Keycode is Key.Minus or Key.KpSubtract) ZoomAt(Size / 2, 1 / 1.12f);
            else if (key.Keycode is Key.Left or Key.Up or Key.Right or Key.Down)
            {
                if (Nodes == null) return;
                var ids = Nodes.Keys.ToArray(); int index = Array.IndexOf(ids, SelectedId);
                Select(ids[(index + (key.Keycode is Key.Left or Key.Up ? ids.Length - 1 : 1)) % ids.Length]);
                Pan = -AxialPosition(Nodes[SelectedId].Axial) * Zoom;
            }
            else return;
            AcceptEvent();
        }
    }

    // =========================================================
    // Explicit states remain readable without relying solely on color.
    private string StatusText(ResearchNode node) => Player == null ? "UNAVAILABLE" :
        ResearchRules.State(Player, node) switch
        {
            ResearchState.Researched => node.Id == "research_core" ? "CORE · FREE" : "RESEARCHED",
            ResearchState.Locked => "LOCKED",
            ResearchState.InsufficientPoints => "NEED " + node.Cost + " RP",
            _ => node.Cost + " RP · AVAILABLE"
        };

    // =========================================================
    // Connectors and hexes share one clipped canvas; only authored real nodes are drawn.
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#091722"));
        if (Nodes == null) return;
        foreach (ResearchNode node in Nodes.Values)
            foreach (string prerequisite in node.Prerequisites)
                DrawLine(GraphToScreen(AxialPosition(Nodes[prerequisite].Axial)), GraphToScreen(AxialPosition(node.Axial)),
                    new Color("#3b6577"), 2 * Zoom, true);
        Font font = ThemeDB.FallbackFont;
        foreach (ResearchNode node in Nodes.Values)
        {
            ResearchState state = Player == null ? ResearchState.Locked : ResearchRules.State(Player, node);
            Color accent = state == ResearchState.Researched ? new Color("#76e3bd") :
                state == ResearchState.Available ? new Color("#79c9ee") : new Color("#8494a8");
            Vector2 center = GraphToScreen(AxialPosition(node.Axial));
            Vector2[] polygon = Hex(center, Radius * Zoom);
            DrawColoredPolygon(polygon, state == ResearchState.Researched ? new Color("#173b3c") : new Color("#142637"));
            DrawPolyline(polygon.Append(polygon[0]).ToArray(), accent, 2, true);
            if (node.Id == SelectedId)
            {
                var ring = Hex(center, (Radius + 6) * Zoom);
                DrawPolyline(ring.Append(ring[0]).ToArray(), HasFocus() ? Colors.White : new Color("#e4c887"), 2, true);
            }
            string[] words = node.Name.Split(' ');
            string line1 = string.Join(" ", words.Take(Math.Max(1, words.Length - 1)));
            string line2 = words.Length > 1 ? words[^1] : "";
            DrawCentered(font, center + new Vector2(0, -10 * Zoom), line1, accent, (int)(13 * Zoom));
            DrawCentered(font, center + new Vector2(0, 7 * Zoom), line2, accent, (int)(13 * Zoom));
            DrawCentered(font, center + new Vector2(0, 29 * Zoom), StatusText(node), accent, (int)(9 * Zoom));
        }
    }

    // =========================================================
    // Font measurement keeps labels attached to their hex center when the view transforms.
    private void DrawCentered(Font font, Vector2 baseline, string text, Color color, int size)
    {
        size = Math.Max(6, size);
        DrawString(font, baseline - new Vector2(font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X / 2, 0),
            text, HorizontalAlignment.Left, -1, size, color);
    }
}
