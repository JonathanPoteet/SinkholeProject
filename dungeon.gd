extends Node3D

@export var width: int = 100
@export var height: int = 100
@export var steps: int = 100
@export var wall_scene: PackedScene
@export var minimap_scene: PackedScene
var minimap_node: Node
var dungeon_map = [] # 0 = Wall, 1 = Floor/Path

# --- MAIN EXECUTION FLOW ---
func _ready():
	# 1. Initialize the Player's position
	initialize_player_position()
	
	# 2. Initialize the Floor's collision structure (one large plane)
	initialize_floor_collision() 
	
	# 3. Create the structure (which is the reverse: carving paths from an all-wall structure)
	create_dungeon_paths() 
	
	# 4. Create the final visual structures (Walls) based on the map
	create_wall_structure() 
	
	initialize_minimap()

# ----------------------------------------------------
# 1. Initialize the Player's position
# ----------------------------------------------------
func initialize_player_position():
	# Spawn the player in the dead center of the map (where the safe zone is)
	var center_x = width / 2
	var center_y = height / 2
	
	# Ensure the position is centered on the tile (0.5 offset)
	# Assumes the player node is named "Player" and exists in the scene tree
	$Player.position = Vector3(center_x + 0.5, 2.0, center_y + 0.5)

# ----------------------------------------------------
# 2. Initialize the Floor's collision structure (Single Collider)
# ----------------------------------------------------
func initialize_floor_collision():
	const FLOOR_THICKNESS = 0.1
	const WALL_HEIGHT = 2.0
	const WALL_THICKNESS = 0.1
	
	# --- 1. Create the floor ---
	var floor = StaticBody3D.new()
	floor.name = "MassiveFloorCollider"
	
	var floor_shape = BoxShape3D.new()
	floor_shape.size = Vector3(width, FLOOR_THICKNESS, height)
	
	var floor_collision = CollisionShape3D.new()
	floor_collision.shape = floor_shape
	floor.add_child(floor_collision)
	
	floor.position = Vector3(width / 2.0, -(FLOOR_THICKNESS / 2.0), height / 2.0)
	
	# Optional visual mesh
	var visual = MeshInstance3D.new()
	var mesh = BoxMesh.new()
	mesh.size = floor_shape.size
	visual.mesh = mesh
	floor.add_child(visual)
	
	add_child(floor)
	
	# --- 2. Create edge walls ---
	var wall_positions = [
		Vector3(width/2, WALL_HEIGHT/2, -WALL_THICKNESS/2),      # Back edge
		Vector3(width/2, WALL_HEIGHT/2, height + WALL_THICKNESS/2), # Front edge
		Vector3(-WALL_THICKNESS/2, WALL_HEIGHT/2, height/2),    # Left edge
		Vector3(width + WALL_THICKNESS/2, WALL_HEIGHT/2, height/2)  # Right edge
	]
	
	var wall_sizes = [
		Vector3(width, WALL_HEIGHT, WALL_THICKNESS),  # Back/Front
		Vector3(WALL_THICKNESS, WALL_HEIGHT, height)  # Left/Right
	]
	
	for i in range(4):
		var wall = StaticBody3D.new()
		var cs = CollisionShape3D.new()
		cs.shape = BoxShape3D.new()
		cs.shape.size = wall_sizes[0] if i < 2 else wall_sizes[1]
		wall.add_child(cs)
		wall.position = wall_positions[i]
		add_child(wall)


# ----------------------------------------------------
# 3. Create the paths that cut through the floor (Dungeon Map Generation)
# ----------------------------------------------------
# ----------------------------------------------------
# 3. Create the paths that cut through the floor (Dungeon Map Generation)
# ----------------------------------------------------
func create_dungeon_paths():
	# Initialize the entire map array as walls (0) - (Untouched)
	dungeon_map.resize(width)
	for x in range(width):
		dungeon_map[x] = []
		dungeon_map[x].resize(height)
		for y in range(height):
			dungeon_map[x][y] = 0

	# 1. Pick key points, ensuring safety margins (Untouched)
	var num_points = 6 + randi() % 4
	var points = []
	var max_x = width - 2 
	var max_y = height - 2
	
	for i in range(num_points):
		points.append(Vector2(randi() % max_x, randi() % max_y))

	# ------------------ NEW: GUARANTEE MAIN AXIS PATH ------------------
	# Define entry and exit points in the middle of the map
	var entry_point = Vector2(1, height / 2)
	var exit_point = Vector2(width - 2, height / 2)
	
	# Pre-emptively carve this essential path first.
	carve_corridor(entry_point, exit_point)
	# ------------------------------------------------------------------

	# 2. Connect remaining random points (These paths now connect to the main path)
	for i in range(points.size() - 1):
		carve_corridor(points[i], points[i+1])

	# 3. Force guaranteed openings on edges (Already handled by the NEW step above, 
	# but we can leave these for secondary connections if you like)
	dungeon_map[1][height/2] = 1 
	dungeon_map[width-2][height/2] = 1
	
	# 4. Force central safe zone to be a path (1) (This is now redundant 
	# because the main axis carving does this, but keeping it is fine as a fail-safe)
	var clear_size = 20
	var half_clear_size = clear_size / 2 
	var center_start_x = width / 2 - half_clear_size
	var center_end_x = width / 2 + half_clear_size
	var center_start_y = height / 2 - half_clear_size
	var center_end_y = height / 2 + half_clear_size

	for x in range(center_start_x, center_end_x):
		for y in range(center_start_y, center_end_y):
			dungeon_map[x][y] = 1
# ----------------------------------------------------
# 4. Create the final structure (Walls)
# ----------------------------------------------------
func create_wall_structure():
	# Only spawn walls where the map is '0' (Wall)
	for x in range(width):
		for y in range(height):
			if dungeon_map[x][y] == 0:
				# Calculate the centered position for the wall
				var centered_pos3d = Vector3(x + 0.5, 0, y + 0.5)
				
				var wall = wall_scene.instantiate()
				# Corrected wall height offset 
				wall.position = centered_pos3d + Vector3(0, 1.05, 0)
				add_child(wall)

# ----------------------------------------------------
# HELPER FUNCTION (Used by create_dungeon_paths)
# ----------------------------------------------------
# ----------------------------------------------------
# HELPER FUNCTION (Used by create_dungeon_paths)
# ----------------------------------------------------
func carve_corridor(a: Vector2, b: Vector2):
	var x = int(a.x)
	var y = int(a.y)
	
	# ------------------ REVISED WIDENING HELPER ------------------
	# This helper carves a full 2x2 block starting from (px, py)
	var carve_2x2 = func(px, py):
		for dx in range(2):
			for dy in range(2):
				var carve_x = px + dx
				var carve_y = py + dy
				
				# Only carve if the calculated coordinate is within the map bounds
				if carve_x < width and carve_y < height:
					dungeon_map[carve_x][carve_y] = 1
	# --------------------------------------------------------------

	# Horizontal step toward b.x
	while x != int(b.x):
		# Carve a 2x2 block at the current position
		carve_2x2.call(x, y)
		
		# Move forward by one unit
		x += sign(b.x - x)

	# Vertical step toward b.y
	while y != int(b.y):
		# Carve a 2x2 block at the current position
		carve_2x2.call(x, y)
		
		# Move forward by one unit
		y += sign(b.y - y)

	# Ensure the final cell (which forms the end of the corner) is carved
	carve_2x2.call(x, y)


func initialize_minimap():
	if not minimap_scene:
		print("ERROR: Minimap scene not assigned!")
		return

	# Instantiate the minimap scene
	var minimap_instance = minimap_scene.instantiate()
	var minimap_control = minimap_instance.get_node("Control")
	add_child(minimap_instance)  # Add to scene tree
	# Pass the dungeon data
	if minimap_control.has_method("setup_map_data"):
		minimap_control.setup_map_data(dungeon_map, width, height)

	minimap_node = minimap_control
