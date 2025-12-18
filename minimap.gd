extends Control

@export var tile_size_2d: int = 4 # Size in pixels for each map cell (e.g., 4x4)
# ... other exports ...

var dungeon_map_ref: Array 
var map_width: int = 10
var map_height: int = 10
var minimap_texture: ImageTexture
var minimap_image: Image

func _ready():
	# Remove all logic and just call print here for testing.
	# The setup_map_data call handles the actual setup.
	print("Minimap node is READY!") 
	var img = Image.create(10, 10, false, Image.FORMAT_RGB8)
	img.fill(Color.RED)
	var tex = ImageTexture.create_from_image(img)
	$TextureRect.texture = tex
	


# This function will be called once by the DungeonGenerator to provide the map data
func setup_map_data(map_array: Array, width: int, height: int):
	dungeon_map_ref = map_array
	map_width = width
	map_height = height
	

	generate_minimap()
	
func generate_minimap():
	if dungeon_map_ref == null:
		return
	# Create an image of the right size
	minimap_image = Image.create(map_width, map_height, false, Image.FORMAT_RGB8)

	for x in range(map_width):
		for y in range(map_height):
			if dungeon_map_ref[x][y] == 1:
				minimap_image.set_pixel(x, y, Color.WHITE) # floor
			else:
				minimap_image.set_pixel(x, y, Color.BLACK) # wall


	# Create a texture from the image
	minimap_texture = ImageTexture.create_from_image(minimap_image)
	
	 #Assign it to your TextureRect child
	$TextureRect.texture = minimap_texture
	
