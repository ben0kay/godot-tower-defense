// Typed root for manually painted maps and their Inspector-editable level settings.
using Godot;

public partial class WorldLevel : Node2D
{
    #region Authored Objectives
    public System.Collections.Generic.IReadOnlyList<Node2D> Citadels => _citadels;
    private readonly System.Collections.Generic.List<Node2D> _citadels = new();
    // =========================================================
    // Explicit map rebuild only: ordered references, legacy adapter, then discover other actual Citadels.
    public void CaptureCitadels()
    {
        var previous = _citadels.ToArray();
        _citadels.Clear();
        if (Settings.CitadelPaths?.Count > 0)
            foreach (NodePath path in Settings.CitadelPaths) Add(GetNodeOrNull<Node2D>(path), true);
        else Add(GetNodeOrNull<Node2D>(Settings.CitadelPath), true);
        Discover(this);
        if (previous.Length > 0 && _citadels.Exists(root => !System.Array.Exists(previous, old => old == root)))
        {
            GD.PushWarning("Changing the Citadel roster requires a level restart; existing destination identities are retained.");
            _citadels.Clear(); _citadels.AddRange(previous);
        }
        void Add(Node2D root, bool required)
        {
            if (root == null || root.GetNodeOrNull<CitadelHealth>("Health") == null || root.GetNodeOrNull<Node>("ApproachPoints") == null)
            { if (required) throw new System.InvalidOperationException("Citadel references need Health and ApproachPoints children."); return; }
            if (!_citadels.Contains(root)) _citadels.Add(root);
        }
        void Discover(Node parent)
        { foreach (Node child in parent.GetChildren()) { if (child is Node2D root) Add(root, false); Discover(child); } }
    }
    #endregion
    [Export] public LevelConfig Settings { get; set; }
    public TileMapLayer Passable => GetNode<TileMapLayer>("Terrain/Passable");
    public TileMapLayer Impassable => GetNode<TileMapLayer>("Terrain/Impassable");
    public TileMapLayer Interactive => GetNodeOrNull<TileMapLayer>("Terrain/Interactive");
    public TileMapLayer Decorations => GetNode<TileMapLayer>("Terrain/Decorations");
    public Node2D Citadel => _citadels.Count > 0 ? _citadels[0] : Settings == null ? null : GetNodeOrNull<Node2D>(Settings.CitadelPath);
}
