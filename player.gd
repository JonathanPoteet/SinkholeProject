extends CharacterBody3D

# Movement Constants
const SPEED = 5.0
const JUMP_VELOCITY = 4.5
const GRAVITY = 15.0 # Define gravity properly

# Mouse Look Variables
@export var mouse_sensitivity: float = 0.1
var camera_pitch: float = 0.0 # Stores current up/down angle

func _ready():
	# Capture and hide the mouse cursor for first-person control
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

# Called when an input event (like mouse movement) occurs
func _input(event):
	if event is InputEventMouseMotion and Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		
		# 1. YAW (Left/Right Rotation) - Applied to the CharacterBody3D
		# This turns the whole character horizontally
		# We rotate around the global Y-axis (the character's vertical axis)
		rotate_y(deg_to_rad(-event.relative.x * mouse_sensitivity))

		# 2. PITCH (Up/Down Rotation) - Applied to the Camera3D
		
		# Calculate new pitch angle, clamped to prevent the camera from flipping over
		camera_pitch += event.relative.y * mouse_sensitivity
		camera_pitch = clamp(camera_pitch, -90.0, 90.0)

		# Get the Camera3D node
		# IMPORTANT: Make sure your Camera3D child node is named "Camera3D"
		var camera = $Camera3D 
		
		# Apply the pitch rotation to the camera's local X-axis
		camera.rotation_degrees.x = -camera_pitch


func _physics_process(delta: float) -> void:
	# --- Gravity and Jump ---
	# Add the gravity.
	if not is_on_floor():
		velocity.y -= GRAVITY * delta
	
	# Handle jump.
	if Input.is_action_just_pressed("player_jump"):
	#if Input.is_action_just_pressed("player_jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# --- Movement ---
	
	# Get the input direction using custom actions
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	
	# Calculate direction relative to the character's current facing direction (transform.basis)
	# The input_dir.y is forward/back movement (mapped to Z in world space)
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	if direction:
		# Move at full speed in the calculated direction
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		# Decelerate when no movement input is given
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	# Move the character and handle collision
	move_and_slide()
