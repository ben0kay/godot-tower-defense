// Campaign-only Research Room: selection/info and guarded purchase requests; progression owns payment/save.
using Godot;
using System;
using System.Linq;

public partial class ResearchMenu : Control
{
    private ResearchGraph _graph;
    private Label _name, _description, _effect, _prerequisites, _cost, _state, _message;
    private Button _purchase;
    private PlayerProgression _player;
    private string _profileId;
    private bool _leaving;

    // =========================================================
    // Menus use the existing palette/button factory while the research canvas gets flexible space.
    public override void _Ready()
    {
        GetTree().Paused = false; Input.MouseMode = Input.MouseModeEnum.Visible;
        if (ProfileStore.Selected == null) { MenuNavigation.ReturnToBoot(this); return; }
        _profileId = ProfileStore.Selected.Id;
        UIMasterConfig palette = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new();
        ColorRect background = new() { Color = new Color("#08141f"), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MarginContainer margin = new(); AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 20);
        VBoxContainer page = new(); page.AddThemeConstantOverride("separation", 10); margin.AddChild(page);
        page.AddChild(UIButtonFactory.Label("RESEARCH ROOM", 28));
        page.AddChild(UIButtonFactory.Label("Commander " + ProfileStore.Selected.Name + "  /  Permanent profile upgrades", 14));
        page.AddChild(new PlayerProgressionPanel { Name = "PlayerProgression" });
        HSplitContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        page.AddChild(body);
        _graph = new ResearchGraph { Name = "ResearchGraph", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddChild(_graph);
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(290, 0) };
        panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(palette.PanelColor, palette.AccentColor));
        body.AddChild(panel);
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        VBoxContainer info = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddThemeConstantOverride("separation", 12); scroll.AddChild(info);
        _name = InfoLabel(info, 22); _state = InfoLabel(info, 15);
        info.AddChild(new HSeparator());
        _description = InfoLabel(info, 15); _effect = InfoLabel(info, 16);
        _prerequisites = InfoLabel(info, 14); _cost = InfoLabel(info, 16);
        _purchase = UIButtonFactory.Create("PURCHASE", Purchase, UIButtonStyle.Action, 46);
        _purchase.Name = "PurchaseResearch"; info.AddChild(_purchase);
        _message = InfoLabel(info, 14);
        Label hint = UIButtonFactory.Label("Drag empty space / middle-drag: pan   ·   Wheel: zoom   ·   Arrows: browse   ·   Home: reset", 12);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart; page.AddChild(hint);
        HBoxContainer footer = new(); page.AddChild(footer);
        footer.AddChild(UIButtonFactory.Create("BACK TO CAMPAIGN", Back, UIButtonStyle.Action, 42));
        footer.AddChild(UIButtonFactory.Create("RESET VIEW", () => _graph.ResetView(), UIButtonStyle.Compact, 42));
        _graph.SelectionChanged += _ => ShowSelection();
        ProgressionService.Changed += OnProgressionChanged;
        try { _graph.Nodes = ResearchService.Nodes; Refresh(); _graph.ResetView(); _graph.GrabFocus(); }
        catch (Exception error) { ShowFailure(error); }
    }

    // =========================================================
    // Labels wrap inside the fixed info column; long authored descriptions scroll vertically.
    private static Label InfoLabel(VBoxContainer parent, int size)
    {
        Label label = UIButtonFactory.Label("", size); label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label); return label;
    }

    // =========================================================
    // Refresh only on committed progression changes and initial entry, never every frame.
    private void OnProgressionChanged(string id) { if (id == _profileId) Refresh(); }
    private void Refresh()
    {
        if (ProfileStore.Selected?.Id != _profileId) throw new InvalidOperationException("Profile changed; return to Campaign.");
        _player = ProgressionService.GetCurrent(); _graph.Player = _player; _graph.QueueRedraw(); ShowSelection();
        _message.Text = ProgressionService.RecoveryMessage;
    }

    // =========================================================
    // The panel shows actual prerequisites, currency, effect and ownership rather than cosmetic placeholders.
    private void ShowSelection()
    {
        if (_player == null || _graph.Nodes == null) return;
        ResearchNode node = _graph.Nodes[_graph.SelectedId];
        ResearchState state = ResearchRules.State(_player, node);
        _name.Text = node.Name; _description.Text = node.Description;
        _effect.Text = node.Effect == ResearchEffectKind.CitadelMaxHealthPercent ?
            $"EFFECT: +{node.Percent:0.##}% Citadel maximum HP\nApplies from the next deployment." : "EFFECT: Origin of the research network.";
        _prerequisites.Text = "PREREQUISITES: " + (node.Prerequisites.Length == 0 ? "None" :
            string.Join(", ", node.Prerequisites.Select(id => _graph.Nodes[id].Name)));
        _cost.Text = node.Cost == 0 ? "COST: FREE" : $"COST: {node.Cost} RP   /   BALANCE: {_player.UnspentResearchPoints} RP";
        _state.Text = state switch { ResearchState.Researched => "RESEARCHED", ResearchState.Locked => "LOCKED",
            ResearchState.InsufficientPoints => "INSUFFICIENT RP", _ => $"AVAILABLE ({node.Cost} RP)" };
        _purchase.Disabled = state != ResearchState.Available;
        _purchase.Text = state == ResearchState.Researched ? "RESEARCHED" : "PURCHASE UPGRADE";
        _purchase.TooltipText = state == ResearchState.Locked ? "Research all prerequisites first." : _state.Text;
    }

    // =========================================================
    // Service rechecks profile, scene, ownership, prerequisites and RP before the atomic payment.
    private void Purchase()
    {
        try
        {
            if (_leaving || ProfileStore.Selected?.Id != _profileId) throw new InvalidOperationException("Research is unavailable.");
            ResearchService.Purchase(this, _graph.SelectedId); Refresh(); _message.Text = "Research saved. Active from your next deployment.";
        }
        catch (Exception error) { _message.Text = error.Message; }
    }

    // =========================================================
    // Storage/catalog failures remain visible, with purchases disabled and Back still available.
    private void ShowFailure(Exception error)
    { _player = null; _graph.Player = null; _purchase.Disabled = true; _message.Text = error.Message; }

    // =========================================================
    // Research has one exit destination; there is no link from Results or battlefield UI.
    private void Back()
    {
        if (_leaving) return; _leaving = true;
        try { MenuNavigation.Open(this, MenuNavigation.CampaignScene); }
        catch (Exception error) { _leaving = false; _message.Text = error.Message; }
    }
    public override void _ExitTree() { _leaving = true; ProgressionService.Changed -= OnProgressionChanged; }
}
