using Godot;
using System;
using System.Collections.Generic;
namespace SinkholeProject.ChunkCreation.CarvePaths;
using SinkholeProject.World;
using System.Diagnostics;

public partial class CarvePaths : Node
{
	public enum BiomeType { Cave, Cavern }

	private class Room
	{
		public Vector3I Center;
		public int Width, Height, Depth;
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
	
	// Iterate through each entry point identified for this chunk
	foreach (Vector3I point in entryPoints)
	{
		GD.Print($"Processing entry point at: X={point.X}, Y={point.Y}, Z={point.Z}");
	}
	

	// 2. GENERATE ROOMS
	List<Room> rooms = GenerateRooms(rnd, width, height, depth, biome, chunkIndex);
	rooms.Sort((a, b) => b.Center.Y.CompareTo(a.Center.Y));
	
		// C. Connect Room Spine (DAG)
	for (int i = 0; i < rooms.Count; i++) {
		// Build the DAG relationships
		if (i < rooms.Count - 1) Connect(rooms[i], rooms[i + 1]);
		if (i < rooms.Count - 2 && rnd.NextDouble() < 0.4) Connect(rooms[i], rooms[i + 2]);

		// Carve connections
		foreach (var neighbor in rooms[i].Neighbors) {
			if (rooms.IndexOf(neighbor) > i) {
				CarveVerticalShaft(rooms[i].Center, neighbor.Center, map, width, height, depth, biome, context);
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
	
	foreach (Vector3I point in currentChunk.ExitPoints)
	{
		GD.Print($" point at: X={point.X}, Y={point.Y}, Z={point.Z}");
	}

	// 4. CARVE ROOMS
	foreach (var room in rooms) {
		CarveRoom(room, map, width, height, depth, biome, context);
	}

	// 5. CONNECT EVERYTHING
	// A. Connect Entry -> First Room
	foreach (var entry in entryPoints) {
		CarveVerticalShaft(entry, rooms[0].Center, map, width, height, depth, biome, context);
	}

	// B. Connect Last Room -> Exits //should be carve closest room to exits
	foreach (var exit in currentChunk.ExitPoints) {
		CarveVerticalShaft(rooms[^1].Center, exit, map, width, height, depth, biome, context);
	}
	
}

		
		
		
		

	// --- Room Generation ---

	private List<Room> GenerateRooms(System.Random rnd, int width, int height, int depth, BiomeType biome, int chunkIndex)
	{
		var rooms = new List<Room>();
		int roomCount = rnd.Next(3, 7);
		int sectionHeight = height / roomCount;
		// Spread rooms evenly across the chunk height
			int yOffset = 0;
			if (chunkIndex == 0) {
				yOffset = 20; //if first chunk then we need to account for the terrain usage which consists of the first 20 voxel rows
			} 

		for (int i = 0; i < roomCount; i++)
		{
			int yMin = i * sectionHeight + sectionHeight / 4;
			int yMax = (i + 1) * sectionHeight - sectionHeight / 4;
			
			int roomY = rnd.Next(yMin, yMax) - yOffset;
			// Ensure Y doesn't exceed the original physical chunk height
			roomY = Math.Clamp(roomY, 0, depth - 1);
		
			int roomX = rnd.Next(20, width - 20);
			int roomZ = rnd.Next(20, depth - 20);

			int roomW, roomH, roomD;
			switch (biome)
			{
				case BiomeType.Cavern:
					roomW = rnd.Next(20, 35);
					roomH = rnd.Next(6, 12);
					roomD = rnd.Next(20, 35);
					break;
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
				Depth  = roomD
			};

			int xMin = newRoom.Center.X - (newRoom.Width / 2);
			int xMax = newRoom.Center.X + (newRoom.Width / 2);
			int zMin = newRoom.Center.Z - (newRoom.Depth / 2);
			int zMax = newRoom.Center.Z + (newRoom.Depth / 2);

			rooms.Add(newRoom);
			Debug.WriteLine($"[Room {i}] Pos: ({newRoom.Center.X}, {newRoom.Center.Y}, {newRoom.Center.Z}) | Size: {newRoom.Width}x{newRoom.Height}x{newRoom.Depth}");
		}

		return rooms;
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
				CarveBlobRoom(room, map, width, height, depth, context);
				break;
			case BiomeType.Cavern:
				CarveCavernRoom(room, map, width, height, depth, context);
				break;
		}
	}

	// Irregular noise-carved blob
	private void CarveBlobRoom(Room room, int[,,] map, int width, int height, int depth, WorldContext context)
	{
		int hw = room.Width / 2;
		int hh = room.Height / 2;
		int hd = room.Depth / 2;

		for (int x = -hw; x <= hw; x++)
		for (int y = -hh; y <= hh; y++)
		for (int z = -hd; z <= hd; z++)
		{
			float noise = context.Noise.GetNoise3D(
				(room.Center.X + x) * 0.3f,
				(room.Center.Y + y) * 0.3f,
				(room.Center.Z + z) * 0.3f);

			float nx = (float)x / hw;
			float ny = (float)y / hh;
			float nz = (float)z / hd;
			float ellipse = nx * nx + ny * ny + nz * nz;

			if (ellipse + noise * 0.4f < 1.0f)
				SafeCarve(room.Center.X + x, room.Center.Y + y, room.Center.Z + z,
					map, width, height, depth);
		}
	}

	// Strict rectangular box
	private void CarveBoxRoom(Room room, int[,,] map, int width, int height, int depth)
	{
		int hw = room.Width / 2;
		int hh = room.Height / 2;
		int hd = room.Depth / 2;

		for (int x = -hw; x <= hw; x++)
		for (int y = -hh; y <= hh; y++)
		for (int z = -hd; z <= hd; z++)
			SafeCarve(room.Center.X + x, room.Center.Y + y, room.Center.Z + z,
				map, width, height, depth);
	}

	// Large flat noise ellipse
	private void CarveCavernRoom(Room room, int[,,] map, int width, int height, int depth, WorldContext context)
	{
		int hw = room.Width / 2;
		int hh = room.Height / 2;
		int hd = room.Depth / 2;

		for (int x = -hw; x <= hw; x++)
		for (int y = -hh; y <= hh; y++)
		for (int z = -hd; z <= hd; z++)
		{
			float nx = (float)x / hw;
			float ny = (float)(y * 2) / hh; // flatten vertically
			float nz = (float)z / hd;
			float ellipse = nx * nx + ny * ny + nz * nz;

			float noise = context.Noise.GetNoise3D(
				(room.Center.X + x) * 0.15f,
				(room.Center.Y + y) * 0.15f,
				(room.Center.Z + z) * 0.15f) * 0.3f;

			if (ellipse + noise < 1.0f)
				SafeCarve(room.Center.X + x, room.Center.Y + y, room.Center.Z + z,
					map, width, height, depth);
		}
	}

	// --- Shaft Carvers ---

	private void CarveVerticalShaft(Vector3I start, Vector3I end, int[,,] map,
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
	int width, int height, int depth, WorldContext context)
{
	// 1. Calculate distance in 3D
	float dist = ((Vector3)start).DistanceTo((Vector3)end);
	int steps = (int)(dist * 2); // 2 steps per unit ensures no holes

	for (int i = 0; i <= steps; i++)
	{
		float t = (float)i / steps;
		
		// 2. Interpolate all 3 axes
		float x = Mathf.Lerp(start.X, end.X, t);
		float y = Mathf.Lerp(start.Y, end.Y, t);
		float z = Mathf.Lerp(start.Z, end.Z, t);
		
		// 3. Add noise to the path for a natural "cave" feel
		// Keep the noise magnitude smaller than your radius to prevent gaps
		float noiseX = context.Noise.GetNoise3D(x * 0.2f, y * 0.2f, z * 0.2f) * 2f;
		float noiseZ = context.Noise.GetNoise3D(x * 0.2f, y * 0.2f, z * 0.2f + 100f) * 2f;
		
		int radius = 3; // Keep radius consistent
		
		// 4. Carve
		CarveRadius((int)(x + noiseX), (int)y, (int)(z + noiseZ), radius, map, width, height, depth);
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

	private void CarveRadius(int cx, int cy, int cz, int radius, int[,,] map,
		int width, int height, int depth)
	{
		for (int x = -radius; x <= radius; x++)
		for (int y = -radius; y <= radius; y++)
		for (int z = -radius; z <= radius; z++)
			if (x * x + y * y + z * z <= radius * radius)
				SafeCarve(cx + x, cy + y, cz + z, map, width, height, depth);
	}

	private void SafeCarve(int x, int y, int z, int[,,] map, int width, int height, int depth)
	{
		if (x > 0 && x < width - 1 && y >= 0 && y < height && z > 0 && z < depth - 1)
			map[x, y, z] = 0;
	}

	
	private Vector3I GetConnectionPoint(int chunkIndex, int width, int depth, WorldContext context)
	{
		// Use the index of the chunk ABOVE as the seed. 
		// This ensures both Chunk N and Chunk N+1 see the same coordinates for the bridge.
		var rnd = new System.Random(context.Seed + (chunkIndex - 1)); 
		return new Vector3I(
			rnd.Next(20, width - 20),
			0, // Y is irrelevant for the anchor, we use this for both Top/Bottom
			rnd.Next(20, depth - 20)
		);
	}

	private BiomeType GetBiomeForChunk(int chunkIndex, WorldContext context)
	{
		var rnd = new System.Random(context.Seed + chunkIndex * 777);
		return (BiomeType)rnd.Next(0, 3);
	}
}
