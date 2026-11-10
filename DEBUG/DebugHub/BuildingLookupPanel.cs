// Off-tree catalogue artwork browser. The live world never owns the inspected scene.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BuildingLookupPanel : PanelContainer
{
    private TowerCatalog _catalog;
    private LineEdit _search;
    private ItemList _list;
    private OptionButton _tier;
    private Label _metadata, _artStatus;
    private ScrollContainer _scroll;
    private EnemySpritePreview _art;
    private readonly Dictionary<int, TowerDefinition> _rows = new();
    private TowerDefinition _selected;
    private bool _fit = true;

    internal void Initialize(TowerCatalog catalog, UIMasterConfig theme)
    {
        _catalog = catalog;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        OffsetLeft = 18; OffsetTop = 72; OffsetRight = -18; OffsetBottom = -30;
        AddThemeStyleboxOverride("panel", UIButtonFactory.Box(theme.PanelColor, theme.AccentColor));
        var content = new VBoxContainer(); AddChild(content);
        var header = new HBoxContainer(); content.AddChild(header);
        var heading = UIButtonFactory.Label("BUILDING LOOKUP", 20, theme);
        heading.SizeFlagsHorizontal = SizeFlags.ExpandFill; header.AddChild(heading);
        var close = UIButtonFactory.Create("Close · Esc", Close, UIButtonStyle.Compact, 32, theme);
        close.SizeFlagsHorizontal = SizeFlags.ShrinkEnd; header.AddChild(close);

        var columns = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; content.AddChild(columns);
        var browser = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) }; columns.AddChild(browser);
        _search = new LineEdit { PlaceholderText = "Search name or building ID" }; browser.AddChild(_search);
        _list = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, FixedIconSize = new Vector2I(32, 32) }; browser.AddChild(_list);
        _search.TextChanged += _ => RefreshList();
        _list.ItemSelected += index => { if (_rows.TryGetValue((int)index, out var definition)) Select(definition); };

        var viewer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; columns.AddChild(viewer);
        _metadata = UIButtonFactory.Label("", 16, theme); viewer.AddChild(_metadata);
        _metadata.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        var options = new HBoxContainer(); viewer.AddChild(options);
        _tier = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill }; options.AddChild(_tier);
        _tier.ItemSelected += _ => RefreshArtwork();
        options.AddChild(UIButtonFactory.Create("Actual Size (1:1)", () => SetFit(false), UIButtonStyle.Compact, 32, theme));
        options.AddChild(UIButtonFactory.Create("Fit Preview", () => SetFit(true), UIButtonStyle.Compact, 32, theme));
        _artStatus = UIButtonFactory.Label("", 13, theme); viewer.AddChild(_artStatus);
        _artStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; viewer.AddChild(_scroll);
        _art = new EnemySpritePreview { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _scroll.AddChild(_art);
        SetFit(true); Hide();
    }

    internal void Open() { Show(); RefreshList(); _search.GrabFocus(); }

    internal void Close()
    {
        if (!Visible) return;
        _tier.GetPopup().Hide();
        Hide(); _art.ShowArtwork(null, _fit);
    }

    private void SetFit(bool fit)
    {
        _fit = fit;
        _scroll.HorizontalScrollMode = fit ? ScrollContainer.ScrollMode.Disabled : ScrollContainer.ScrollMode.Auto;
        _scroll.VerticalScrollMode = fit ? ScrollContainer.ScrollMode.Disabled : ScrollContainer.ScrollMode.Auto;
        _scroll.ScrollHorizontal = _scroll.ScrollVertical = 0;
        _art.SetFit(fit);
    }

    private void RefreshList()
    {
        string query = _search.Text.Trim();
        var entries = _catalog.Definitions.Where(d => d != null &&
            (d.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || d.TowerId.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(d => d.Category).ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase);
        _list.Clear(); _rows.Clear();
        foreach (var group in entries.GroupBy(d => d.Category))
        {
            int header = _list.ItemCount;
            _list.AddItem(group.Key.ToString().ToUpperInvariant());
            _list.SetItemSelectable(header, false);
            foreach (var definition in group)
            {
                int row = _list.ItemCount;
                _list.AddItem($"{definition.DisplayName}  ·  {definition.Footprint.X}×{definition.Footprint.Y}  ·  {definition.TowerId}", definition.Icon);
                _rows.Add(row, definition);
            }
        }
        if (_rows.Count == 0)
        {
            _selected = null; _tier.Clear(); _tier.Disabled = true; _metadata.Text = "";
            _artStatus.Text = "No matching buildings."; _art.ShowArtwork(null, _fit); return;
        }
        int index = _rows.FirstOrDefault(pair => pair.Value == _selected, _rows.First()).Key;
        _list.Select(index); Select(_rows[index]);
    }

    private void Select(TowerDefinition definition)
    {
        int tier = _selected == definition ? _tier.Selected : 0;
        _selected = definition;
        _metadata.Text = $"{definition.DisplayName}  ·  {definition.Category}  ·  {definition.Footprint.X}×{definition.Footprint.Y}  ·  {definition.TowerId}";
        _tier.Clear(); _tier.Disabled = false;
        int count = definition.DefaultConfig?.Levels?.Count ?? 0;
        for (int i = 1; i <= Math.Max(1, count); i++) _tier.AddItem(count == 0 ? "Base / Level 1" : $"Level {i}");
        _tier.Select(Mathf.Clamp(tier, 0, _tier.ItemCount - 1));
        RefreshArtwork();
    }

    // =========================================================
    // Resolve exactly the same finished textures as the construction presenter on detached nodes only.
    private void RefreshArtwork()
    {
        Node source = _selected?.Scene?.Instantiate();
        int level = _tier.Selected + 1;
        bool prior = false;
        try
        {
            if (source is Node2D root)
            {
                var presenter = root.GetNodeOrNull<TowerConstructionVisuals>("ConstructionVisuals");
                if (presenter != null) prior = ResolveArtwork(presenter, _selected.DefaultConfig?.ConstructionArt, level);
                else prior = level > 1;
                _art.ShowArtwork(root, _fit);
            }
            else _art.ShowArtwork(null, _fit);
        }
        finally { source?.Free(); }

        Vector2 size = _art.ArtworkBounds.Size;
        _artStatus.Text = _art.SpriteCount == 0 ? "Finished artwork is missing." :
            $"Native composite: {size.X:0.#} × {size.Y:0.#} px" + (prior ? "  ·  Uses prior level artwork" : "");
        _scroll.ScrollHorizontal = _scroll.ScrollVertical = 0;
    }

    private static bool ResolveArtwork(TowerConstructionVisuals presenter, TowerConstructionArt art, int level)
    {
        Sprite2D sprite = FindSprite(presenter, presenter.SpritePath);
        Sprite2D baseSprite = FindSprite(presenter, presenter.FinishedBaseSpritePath);
        Sprite2D gun = FindSprite(presenter, presenter.FinishedGunSpritePath);
        if (sprite == null) return level > 1;
        Texture2D original = sprite.Texture, originalBase = baseSprite?.Texture, originalGun = gun?.Texture;
        Texture2D finished = art?.FinishedTexture(level, original) ?? original;
        Texture2D finishedBase = art?.FinishedBaseTexture(level, originalBase) ?? originalBase;
        Texture2D finishedGun = art?.FinishedGunTexture(level, originalGun) ?? originalGun;
        sprite.Texture = finished;
        if (baseSprite != null) baseSprite.Texture = finishedBase;
        if (gun != null) gun.Texture = finishedGun;
        if (presenter.FinishedVisualPaths?.Count > 0)
        {
            bool split = finished == original;
            sprite.Visible = !split;
            foreach (NodePath path in presenter.FinishedVisualPaths)
                if (!path.IsEmpty && presenter.GetNodeOrNull<CanvasItem>(path) is { } layer && layer != sprite)
                    layer.Visible = split;
        }
        return level > 1 && finished == (art?.FinishedTexture(level - 1, original) ?? original)
            && finishedBase == (art?.FinishedBaseTexture(level - 1, originalBase) ?? originalBase)
            && finishedGun == (art?.FinishedGunTexture(level - 1, originalGun) ?? originalGun);
    }

    private static Sprite2D FindSprite(Node root, NodePath path) =>
        path.IsEmpty ? null : root.GetNodeOrNull<Sprite2D>(path);
}
