# Headless editor/runtime checks against the real Portal/Citadel C# tool scripts.
extends SceneTree

#region Runner
# =========================================================
# Run as an editor SceneTree script to exercise deferred transform notifications, then without --editor.
func _initialize() -> void:
	call_deferred("run_checks")

# =========================================================
# Build a disposable map; never open/save or rewrite an authored level during these checks.
func run_checks() -> void:
	var level := Node2D.new()
	level.name = "SnapFixture"
	root.add_child(level)
	var terrain := Node2D.new()
	terrain.name = "Terrain"
	level.add_child(terrain)
	var layer := TileMapLayer.new()
	layer.name = "Passable"
	layer.tile_set = TileSet.new()
	layer.tile_set.tile_size = Vector2i(64, 64)
	terrain.add_child(layer)
	var objects := Node2D.new()
	objects.name = "Objects"
	objects.position = Vector2(13, 19)
	level.add_child(objects)
	for path in ["res://WORLD/PORTALS/EnemyPortal/EnemyPortal.tscn", "res://WORLD/OBJECTIVES/Citadel/Citadel.tscn"]:
		var object: Node2D = load(path).instantiate()
		objects.add_child(object)
		var marker: Node2D = object.get_node("SpawnExit" if object.has_node("SpawnExit") else "ApproachPoints/Approach01")
		var marker_local := marker.position
		var footprint = object.get("SizeCells")
		object.global_position = Vector2(91, 120)
		await settle()
		if not require_position(object, Vector2(96, 96) if Engine.is_editor_hint() else Vector2(91, 120), "initial placement"): return
		object.global_position = Vector2(-7, -70)
		await settle()
		if not require_position(object, Vector2(-32, -96) if Engine.is_editor_hint() else Vector2(-7, -70), "negative cells"): return
		object.set("SnapToGridInEditor", false)
		object.global_position = Vector2(17, 25)
		await settle()
		if not require_position(object, Vector2(17, 25), "disabled snapping"): return
		object.set("SnapToGridInEditor", true)
		await settle()
		if not require_position(object, Vector2(32, 32) if Engine.is_editor_hint() else Vector2(17, 25), "re-enable"): return
		# Parent/grid transforms and non-default tile dimensions must use map-space conversion.
		layer.tile_set.tile_size = Vector2i(80, 80)
		level.position = Vector2(123, -41)
		level.rotation = 0.17
		level.scale = Vector2(1.2, 0.8)
		object.global_position = layer.to_global(Vector2(111, 185))
		var before := object.global_position
		await settle()
		if not require_position(object, layer.to_global(Vector2(120, 200)) if Engine.is_editor_hint() else before, "transformed 80px grid"): return
		if marker.position != marker_local or object.get("SizeCells") != footprint:
			fail("Child marker offsets and footprint size must remain unchanged")
			return
		var duplicate := object.duplicate() as Node2D
		objects.add_child(duplicate)
		duplicate.global_position = layer.to_global(Vector2(251, 265))
		before = duplicate.global_position
		await settle()
		if not require_position(duplicate, layer.to_global(Vector2(280, 280)) if Engine.is_editor_hint() else before, "duplicate placement"): return
		duplicate.free()
		object.free()
		layer.tile_set.tile_size = Vector2i(64, 64)
		level.transform = Transform2D.IDENTITY
	level.free()
	# Reusable assets edited alone have no Terrain/Passable ancestor and must stay at their origin.
	for path in ["res://WORLD/PORTALS/EnemyPortal/EnemyPortal.tscn", "res://WORLD/OBJECTIVES/Citadel/Citadel.tscn"]:
		var standalone: Node2D = load(path).instantiate()
		root.add_child(standalone)
		await settle()
		if not require_position(standalone, Vector2.ZERO, "standalone asset origin"): return
		standalone.free()
	print("PASS: Portal/Citadel placement, negative cells, toggle, parent transforms, 80px tiles, duplication, child offsets and standalone origin; editor=", Engine.is_editor_hint())
	quit(0)
#endregion

#region Assertions
# =========================================================
# Allow editor-deferred corrections and any resulting transform notification to settle.
func settle() -> void:
	await process_frame
	await process_frame
	await process_frame

# =========================================================
# Report actual transforms so editor regressions are diagnosable without a desktop session.
func require_position(object: Node2D, expected: Vector2, context: String) -> bool:
	if object.global_position.is_equal_approx(expected): return true
	fail(context + ": expected " + str(expected) + ", got " + str(object.global_position))
	return false

# =========================================================
# Fail the smoke command with a nonzero exit code.
func fail(message: String) -> void:
	push_error(message)
	quit(1)
#endregion
