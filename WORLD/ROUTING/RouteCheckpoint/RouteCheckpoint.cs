// Freely placed required routing region: editor-only circle, no footprint, physics or per-frame processing.
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class RouteCheckpoint : Node2D
{
	[Export] public Godot.Collections.Array<NodePath> NextCheckpoints { get; set; } = new();
	internal bool DrawnByDesignOverlay { get; set; }
	public int DesignStamp
	{
		get
		{
			HashCode hash = new(); hash.Add(GeometryStamp); hash.Add(Name.ToString());
			if (NextCheckpoints != null) foreach (NodePath path in NextCheckpoints) hash.Add(path?.ToString());
			return hash.ToHashCode();
		}
	}
	// =========================================================
	// Editor warns about cycles/broken links; runtime captures the same validated link snapshot.
	public override string[] _GetConfigurationWarnings()
	{
		if (!IsInsideTree()) return System.Array.Empty<string>();
		string error = CheckpointNetwork.Capture(this).Error;
		return error.Length == 0 ? System.Array.Empty<string>() : new[] { error };
	}
	public event System.Action GeometryChanged;
	private float _radius = 128;
	[Export(PropertyHint.Range, "1,4096,1,or_greater")]
	public float RadiusPixels
	{ get => _radius; set { _radius = float.IsFinite(value) ? Mathf.Max(1, value) : 128; _targets.Clear(); QueueRedraw(); GeometryChanged?.Invoke(); } }
	public int GeometryStamp => HashCode.Combine(GlobalPosition, RadiusPixels);
	public int TargetBuildCount { get; private set; }
	private sealed record Cache(int Revision, int Geometry, Vector2[] Points);
	private readonly Dictionary<(WorldGrid, float), Cache> _targets = new();

	// =========================================================
	// Redraw only on editor transforms; gameplay never needs a checkpoint processing loop.
	public override void _Ready() { SetNotifyTransform(true); QueueRedraw(); }
	// =========================================================
	// Global transforms may change through a parent; the gameplay circle remains world-space and circular.
	public override void _Notification(int what)
	{ if (what == NotificationTransformChanged) { if (Engine.IsEditorHint()) QueueRedraw(); else GeometryChanged?.Invoke(); } }
	// =========================================================
	// Center-entry semantics: physical ship center must enter a clearance-valid part of the region.
	public bool Contains(Vector2 position) => position.DistanceSquaredTo(GlobalPosition) <= (RadiusPixels + 0.01f) * (RadiusPixels + 0.01f);

	// =========================================================
	// Analytic swept segment entry prevents a fast enemy crossing the whole circle between physics updates.
	public bool Entry(Vector2 from, Vector2 to, out float fraction)
	{
		fraction = 0;
		if (Contains(from)) return true;
		Vector2 offset = from - GlobalPosition, step = to - from;
		double a = step.LengthSquared(), b = 2.0 * offset.Dot(step), c = offset.LengthSquared() - (double)RadiusPixels * RadiusPixels;
		double discriminant = b * b - 4 * a * c;
		if (a <= 0 || discriminant < 0) return false;
		double t = (-b - Math.Sqrt(discriminant)) / (2 * a);
		if (t < 0 || t > 1) return false;
		fraction = (float)t; return true;
	}

	// =========================================================
	// Bounded radius-aware targets: at most 33 deterministic probes, cached per world/radius/revision/geometry.
	// Large circles never enumerate every enclosed tile, and preview pools share this geometry-only cache.
	public Vector2[] Targets(NavigationGrid nav, float enemyRadius)
	{
		var key = (nav.Grid, enemyRadius); int stamp = GeometryStamp;
		if (_targets.TryGetValue(key, out Cache old) && old.Revision == nav.Revision && old.Geometry == stamp) return old.Points;
		List<Vector2> points = new(); TargetBuildCount++;
		Add(GlobalPosition);
		for (int ring = 1; ring <= 2; ring++)
			for (int i = 0; i < 16; i++)
			{
				Vector2 point = GlobalPosition + Vector2.FromAngle(i * Mathf.Tau / 16) * RadiusPixels * (ring == 1 ? 0.45f : 0.85f);
				Add(nav.Grid.CellToWorldCenter(nav.Grid.WorldToCell(point)));
			}
		Vector2[] result = points.ToArray(); _targets[key] = new(nav.Revision, stamp, result); return result;
		void Add(Vector2 point)
		{ if (Contains(point) && nav.HasClearance(point, enemyRadius) && !points.Contains(point)) points.Add(point); }
	}

	// =========================================================
	// Editor visualization only; F3 draws its own runtime checkpoint circles and labels.
	public override void _Draw()
	{
		if (!Engine.IsEditorHint() || DrawnByDesignOverlay) return;
		Vector2[] ring = new Vector2[65];
		for (int i = 0; i < ring.Length; i++) ring[i] = ToLocal(GlobalPosition + Vector2.FromAngle(i * Mathf.Tau / 64) * RadiusPixels);
		DrawColoredPolygon(ring, new Color(0.75f, 0.4f, 1, 0.06f));
		DrawPolyline(ring, new Color(0.75f, 0.4f, 1, 0.9f), 2, true);
		DrawCircle(Vector2.Zero, 5, new Color(0.75f, 0.4f, 1));
	}
}
