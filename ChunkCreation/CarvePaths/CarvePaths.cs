using Godot;
using System;
using System.Collections.Generic;
namespace SinkholeProject.ChunkCreation.CarvePaths;
using SinkholeProject.World;
using System.Diagnostics;
using System.Linq;

public partial class CarvePaths : Node
{
	public enum BiomeType { Cave, Cavern }
	public enum RoomType {Minor, Major}
	
	private class Room
	{
		public Vector3I Center;
		public int Width, Height, Depth;
		public RoomType Type { get; set; }
		
		// Helper to get the bounding box boundaries
		public int XMin => Center.X - (Width / 2);
		public int XMax => Center.X + (Width / 2);
		public int YMin => Center.Y - (Height / 2);
		public int YMax => Center.Y + (Height / 2);
		public int ZMin => Center.Z - (Depth / 2);
		public int ZMax => Center.Z + (Depth / 2);

			public bool Intersects(Room other, int padding = 2)
		{
			return (this.XMin - padding < other.XMax + padding && this.XMax + padding > other.XMin - padding) &&
				   (this.YMin - padding < other.YMax + padding && this.YMax + padding > other.YMin - padding) &&
				   (this.ZMin - padding < other.ZMax + padding && this.ZMax + padding > other.ZMin - padding);
		}
		// Stores references to connected rooms
		public List<Room> Neighbors = new List<Room>(); 
	}
	
	// Helper method to keep things bidirectional
	private void Connect(Room a, Room b)
	{
		if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
		if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
	}

	public void CarveSeededVerticalPath(int chunkIndex, WorldContext context)
{
	var currentChunk = context.WorldChunks[chunkIndex];
	int height = currentChunk.height;
	int width  = currentChunk.width;
	int depth  = currentChunk.depth;
	int[,,] map = currentChunk.MapData;

	BiomeType biome = GetBiomeForChunk(chunkIndex, context);
	var rnd = new System.Random(context.Seed + chunkIndex);

	// 1. DETERMINE ENTRY POINTS
	List<Vector3I> entryPoints = new List<Vector3I>();
	if (chunkIndex == 0) {
		GD.Print($"Checking height: {height}");
		entryPoints = new List<Vector3I> { new Vector3I(width / 2, height - 1, depth / 2) };
	} else {
		foreach (var exit in context.WorldChunks[chunkIndex - 1].ExitPoints) {
		// We keep X and Z, but force Y to the ceiling of the current chunk
		
		// IMPORTANT:Map the exit coordinate (Y=0) of the previous chunk to the entry ceiling (Y=height-1) 
		// of the current chunk. This ensures a seamless vertical transition between stacked chunks.
			entryPoints.Add(new Vector3I(exit.X, height - 1, exit.Z));
		}
	}
	
	// 2. GENERATE ROOMS
	List<Room> allRooms = new List<Room>();
	GenerateRooms(allRooms, rnd, width, height, depth, biome, chunkIndex);
	List<Room> majorRooms = allRooms.Where(r => r.Type == RoomType.Major).ToList();
	List<Room> minorRooms = allRooms.Where(r => r.Type == RoomType.Minor).ToList();
	majorRooms.Sort((a, b) => b.Center.Y.CompareTo(a.Center.Y));
	
	for (int i = 0; i < majorRooms.Count - 1; i++)
{
	// A 30% chance to swap with the next room
	// This allows the path to momentarily go "up" before moving "down"
	if (rnd.NextDouble() < 0.3)
	{
		var temp = majorRooms[i];
		majorRooms[i] = majorRooms[i + 1];
		majorRooms[i + 1] = temp;
		
		// Skip the next index to prevent double-swapping 
		// and keeping the "flow" relatively intact
		i++; 
	}
}
	
	
	// 1. Connect the Major Spine (The Golden Path)
for (int i = 0; i < majorRooms.Count - 1; i++)
{
	Connect(majorRooms[i], majorRooms[i + 1]);
}

// 2. Attach Minor Rooms to the closest Major Room
// This creates natural "pockets" of exploration off the main path
foreach (var minor in minorRooms)
{
	// Find the closest major room
	var closestMajor = majorRooms.OrderBy(m => m.Center.DistanceTo(minor.Center)).First();
	
	Connect(minor, closestMajor);
	
	// Now carve the connection
	Vector3I startPoint = GetExitPoint(minor, minor.Center);
	Vector3I endPoint = GetExitPoint(closestMajor, closestMajor.Center);
	CarvePath(startPoint, endPoint, map, width, height, depth, biome, context);
}

// 3. Carve the Major Spine
foreach (var room in majorRooms)
{
	foreach (var neighbor in room.Neighbors)
	{
		// Ensure we only carve once per connection
		if (majorRooms.IndexOf(neighbor) > majorRooms.IndexOf(room))
		{
			Vector3I startPoint = GetExitPoint(room, room.Center);
			Vector3I endPoint = GetExitPoint(neighbor, neighbor.Center);
			CarvePath(startPoint, endPoint, map, width, height, depth, biome, context);
		}
	}
}

	// 3. GENERATE EXIT POINTS (For the next chunk)
	int exitCount = rnd.Next(1, 4);
	for(int i = 0; i < exitCount; i++) {
		currentChunk.ExitPoints.Add(new Vector3I(
			rnd.Next(20, width - 20), 0, rnd.Next(20, depth - 20)
		));
	}
	
	// 4. CARVE ROOMS
	foreach (var room in majorRooms) {
		CarveRoom(room, map, width, height, depth, biome, context);
	}
	foreach (var room in minorRooms) {
		CarveRoom(room, map, width, height, depth, biome, context);
	}
	// 5. CONNECT EVERYTHING
	// A. Connect Entry -> First Room
	foreach (var entry in entryPoints) {
		Room closestRoom = FindNearestRoom(entry, allRooms);
		CarvePath(entry, closestRoom.Center, map, width, height, depth, biome, context);
	}

	// B. Connect Last Room -> Exits //should be carve closest room to exits
	foreach (var exit in currentChunk.ExitPoints) {
		Room closestRoom = FindNearestRoom(exit, allRooms);
		CarvePath(closestRoom.Center, exit, map, width, height, depth, biome, context);
	}
	
}

		
		
		
		

	// --- Room Generation ---

	private void GenerateRooms(List<Room> rooms, System.Random rnd, int width, int height, int depth, BiomeType biome, int chunkIndex)
	{
		int majorRoomCount = 5; // TODO: go to a bigger size once pathing is figured out
		int minorRoomCount = 5;
		int sectionHeight = height / (majorRoomCount + minorRoomCount);
		// Spread rooms evenly across the chunk height
		int yOffset = 10;
		if (chunkIndex == 0) {
			yOffset = 40; //if first chunk then we need to account for the terrain usage which consists of the first 20 voxel rows
		} 

		void TryCreateRoom(RoomType type, int offset) {
			bool placed = false;
			int attempts = 0;
			while (!placed && attempts < 100) // Limit attempts to avoid infinite hang
				{
					// 1. Generate random coordinates (your existing logic)
					// ... (roomX, roomY, roomZ logic)


					// Generate Y directly within the safe bounds
					int roomY = rnd.Next(10, height - yOffset);
					
					// No subtraction needed here!
					int roomX = rnd.Next(20, width - 20);
					int roomZ = rnd.Next(20, depth - 20);

					int roomW, roomH, roomD;
					switch (biome)
					{
						case BiomeType.Cavern:
						case BiomeType.Cave:
						default:
							roomW = rnd.Next(12, 25);
							roomH = rnd.Next(8, 16);
							roomD = rnd.Next(12, 25);
							break;
					}
					
					var newRoom = new Room{
						Center = new Vector3I(roomX, roomY, roomZ),
						Width  = roomW,
						Height = roomH,
						Depth  = roomD,
						Type = type
					};
					// 2. Check for collisions
					bool collision = false;
					foreach (var existingRoom in rooms)
					{
						if (newRoom.Intersects(existingRoom))
						{
							collision = true;
							break;
						}
					}

					// 3. Place if clear
					if (!collision)
					{
						GD.Print($"Rooms generated: {newRoom.Type}");
						rooms.Add(newRoom);
						placed = true;
					}
					
					attempts++;
				}
			}
		// Generate Major
		for (int i = 0; i < majorRoomCount; i++) TryCreateRoom(RoomType.Major, yOffset);
		// Generate Minor
		for (int i = 0; i < minorRoomCount; i++) TryCreateRoom(RoomType.Minor, yOffset);
	}

	private Room FindNearestRoom(Vector3I point, List<Room> rooms)
	{
		Room nearest = rooms[0];
		float bestDist = float.MaxValue;
		foreach (var room in rooms)
		{
			float dist = ((Vector3)room.Center).DistanceTo((Vector3)point);
			if (dist < bestDist)
			{
				bestDist = dist;
				nearest = room;
			}
		}
		return nearest;
	}

	// --- Room Carvers ---

	private void CarveRoom(Room room, int[,,] map, int width, int height, int depth,
		BiomeType biome, WorldContext context)
	{
		switch (biome)
		{
			case BiomeType.Cave:
			case BiomeType.Cavern:
			default:
				CarveCavernRoom(room, map, width, height, depth, context);
				break;
		}
	}

	// Large flat noise ellipse
	private void CarveCavernRoom(Room room, int[,,] map, int width, int height, int depth, WorldContext context)
	{
		// Calculate the actual voxel boundaries for this room
		int xStart = room.Center.X - (room.Width / 2);
		int xEnd   = room.Center.X + (room.Width / 2);
		
		int yStart = room.Center.Y - (room.Height / 2);
		int yEnd   = room.Center.Y + (room.Height / 2);
		
		int zStart = room.Center.Z - (room.Depth / 2);
		int zEnd   = room.Center.Z + (room.Depth / 2);

		// Iterate through the box and carve
		for (int x = xStart; x <= xEnd; x++)
		{
			for (int y = yStart; y <= yEnd; y++)
			{
				for (int z = zStart; z <= zEnd; z++)
				{
					// We use SafeCarve to turn Solid (0) into Air (1)
					SafeCarve(x, y, z, map, width, height, depth);
				}
			}
		}
	}

	// --- Shaft Carvers ---

	private void CarvePath(Vector3I start, Vector3I end, int[,,] map,
		int width, int height, int depth, BiomeType biome, WorldContext context)
	{
		switch (biome)
		{
			case BiomeType.Cave:
			case BiomeType.Cavern:
				CarveRaggedShaft(start, end, map, width, height, depth, context);
				break;
		}
	}

private void CarveRaggedShaft(Vector3I start, Vector3I end, int[,,] map, 
	int width, int height, int depth, WorldContext context) {
	// Inside CarveRaggedShaft...
	// Check if the area is already empty before carving
	Vector3 current = new Vector3(start.X, start.Y, start.Z);
	Vector3 target = new Vector3(end.X, end.Y, end.Z);
	// In Godot, use .DistanceTo()
	while (current.DistanceTo(target) > 1.0f)
	{
		// Calculate direction towards the target
		Vector3 direction = (target - current).Normalized();
		
		// Add noise for an organic "wobble"
		float nX = context.Noise.GetNoise3D(current.X * 0.1f, current.Y * 0.1f, current.Z * 0.1f);
		float nY = context.Noise.GetNoise3D(current.X * 0.1f + 50f, current.Y * 0.1f, current.Z * 0.1f);
		float nZ = context.Noise.GetNoise3D(current.X * 0.1f + 100f, current.Y * 0.1f, current.Z * 0.1f);
		
		Vector3 wander = new Vector3(nX, nY, nZ) * 0.8f;
		
		// Move and apply noise steering
		current += (direction + wander) * 0.5f;
		
		// Carve using current position
		CarveRadius((int)current.X, (int)current.Y, (int)current.Z, 3, map, width, height, depth);
	}
}


	// --- Shared Helpers ---

	private void CarveSegment(Vector3I from, Vector3I to, int radius, int[,,] map,
		int width, int height, int depth)
	{
		int steps = (int)((Vector3)from).DistanceTo((Vector3)to) * 2;
		steps = Math.Max(steps, 1);
		for (int i = 0; i <= steps; i++)
		{
			float t = (float)i / steps;
			int x   = (int)Mathf.Lerp(from.X, to.X, t);
			int y   = (int)Mathf.Lerp(from.Y, to.Y, t);
			int z   = (int)Mathf.Lerp(from.Z, to.Z, t);
			CarveRadius(x, y, z, radius, map, width, height, depth);
		}
	}

private void CarveRadius(int cx, int cy, int cz, int radius, int[,,] map, int width, int height, int depth)
{
	int rSq = radius * radius; // Pre-calculate squared radius for performance
	for (int ix = -radius; ix <= radius; ix++)
	{
		for (int iy = -radius; iy <= radius; iy++)
		{
			for (int iz = -radius; iz <= radius; iz++)
			{
				// Check if this offset point is within the sphere radius
				if (ix * ix + iy * iy + iz * iz <= rSq)
				{
					SafeCarve(cx + ix, cy + iy, cz + iz, map, width, height, depth);
				}
			}
		}
	}
}

private void SafeCarve(int x, int y, int z, int[,,] map, int width, int height, int depth)
{
	// Bounds check
	if (x > 0 && x < width - 1 && y >= 0 && y < height && z > 0 && z < depth - 1)
	{
		// Only turn 0 (Solid) into 1 (Air). 
		// If it is already 1, we leave it alone. 
		// This prevents tunnels from "erasing" walls that are already 
		// part of a room or another tunnel path.
		map[x, y, z] = 0;
		}
	}

	
	private BiomeType GetBiomeForChunk(int chunkIndex, WorldContext context)
	{
		var rnd = new System.Random(context.Seed + chunkIndex * 777);
		return (BiomeType)rnd.Next(0, 3);
	}
	
	private Vector3I GetExitPoint(Room startRoom, Vector3I targetCenter)
{
	// 1. Calculate the center of the start room
	Vector3I startCenter = startRoom.Center;

	// 2. Determine which direction to head
	// This gives us a vector from start to target
	Vector3I direction = targetCenter - startCenter;

	// 3. Scale the direction to hit the room's boundary
	// We use a ratio based on the room's half-extents
	float ratioX = (float)startRoom.Width / (2 * Math.Abs(direction.X));
	float ratioY = (float)startRoom.Height / (2 * Math.Abs(direction.Y));
	float ratioZ = (float)startRoom.Depth / (2 * Math.Abs(direction.Z));

	// Pick the smallest ratio to ensure we don't overshoot the room's box
	float minRatio = Math.Min(ratioX, Math.Min(ratioY, ratioZ));

	// 4. Return the coordinate on the surface
	return new Vector3I(
		startCenter.X + (int)(direction.X * minRatio),
		startCenter.Y + (int)(direction.Y * minRatio),
		startCenter.Z + (int)(direction.Z * minRatio)
	);
}
	
	
}
