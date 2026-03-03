using Godot;
using System;
using System.Collections.Generic;
namespace SinkholeProject.ChunkCreation.CarvePaths;
using SinkholeProject.World;

public partial class CarvePaths : Node
{
	public enum BiomeType { Cave, Tunnel, Cavern }

	private struct Room
	{
		public Vector3I Center;
		public int Width;
		public int Height;
		public int Depth;
	}

	public void CarveSeededVerticalPath(int chunkIndex, WorldContext context)
	{
		int height = context.WorldChunks[chunkIndex].height;
		int width  = context.WorldChunks[chunkIndex].width;
		int depth  = context.WorldChunks[chunkIndex].depth;
		int[,,] map = context.WorldChunks[chunkIndex].MapData;

		BiomeType biome = GetBiomeForChunk(chunkIndex, context);
		var rnd = new System.Random(context.Seed + chunkIndex * 1000);

		List<Vector3I> entrances = GetEntrancesForChunk(chunkIndex, width, depth, height, context);
		List<Vector3I> exits     = GetExitsForChunk(chunkIndex, width, depth, context);

		// Generate rooms spread across the chunk height
		List<Room> rooms = GenerateRooms(rnd, width, height, depth, biome);

		// Carve all rooms
		foreach (var room in rooms)
			CarveRoom(room, map, width, height, depth, biome, context);

		// Connect adjacent rooms with hallways
		for (int i = 0; i < rooms.Count - 1; i++)
			CarveHallway(rooms[i].Center, rooms[i + 1].Center, map, width, height, depth, biome, context);

		// For each entrance, connect to nearest room then shaft down to exit
		foreach (var entrance in entrances)
		{
			Room nearestToEntrance = FindNearestRoom(entrance, rooms);
			CarveVerticalShaft(new Vector3I(entrance.X, entrance.Y, entrance.Z),
				nearestToEntrance.Center, map, width, height, depth, biome, context);
		}

		foreach (var exit in exits)
		{
			Room nearestToExit = FindNearestRoom(exit, rooms);
			CarveVerticalShaft(nearestToExit.Center,
				new Vector3I(exit.X, exit.Y, exit.Z), map, width, height, depth, biome, context);
		}
	}

	// --- Room Generation ---

	private List<Room> GenerateRooms(System.Random rnd, int width, int height, int depth, BiomeType biome)
	{
		var rooms = new List<Room>();
		int roomCount = rnd.Next(3, 7);

		// Spread rooms evenly across the chunk height
		int sectionHeight = height / roomCount;

		for (int i = 0; i < roomCount; i++)
		{
			int yMin = i * sectionHeight + sectionHeight / 4;
			int yMax = (i + 1) * sectionHeight - sectionHeight / 4;
			int roomY = rnd.Next(yMin, yMax);

			int roomX = rnd.Next(20, width - 20);
			int roomZ = rnd.Next(20, depth - 20);

			int roomW, roomH, roomD;
			switch (biome)
			{
				case BiomeType.Tunnel:
					roomW = rnd.Next(8, 15);
					roomH = rnd.Next(4, 8);
					roomD = rnd.Next(8, 15);
					break;
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

			rooms.Add(new Room
			{
				Center = new Vector3I(roomX, roomY, roomZ),
				Width  = roomW,
				Height = roomH,
				Depth  = roomD
			});
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
			case BiomeType.Tunnel:
				CarveBoxRoom(room, map, width, height, depth);
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

	// --- Hallway Carvers ---

	private void CarveHallway(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth, BiomeType biome, WorldContext context)
	{
		switch (biome)
		{
			case BiomeType.Cave:
				CarveWindingHallway(from, to, map, width, height, depth, context);
				break;
			case BiomeType.Tunnel:
				CarveLHallway(from, to, map, width, height, depth);
				break;
			case BiomeType.Cavern:
				CarveWideHallway(from, to, map, width, height, depth, context);
				break;
		}
	}

	// Noise-driven winding corridor
	private void CarveWindingHallway(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth, WorldContext context)
	{
		int steps = (int)((Vector3)from).DistanceTo((Vector3)to) * 2;
		steps = Math.Max(steps, 1);
		for (int i = 0; i <= steps; i++)
		{
			float t   = (float)i / steps;
			float x   = Mathf.Lerp(from.X, to.X, t);
			float y   = Mathf.Lerp(from.Y, to.Y, t);
			float z   = Mathf.Lerp(from.Z, to.Z, t);
			float drift = context.Noise.GetNoise2D(x * 0.2f, z * 0.2f) * 8f;
			CarveRadius((int)(x + drift), (int)y, (int)(z + drift), 3, map, width, height, depth);
		}
	}

	// Strict L-shaped corridor (two straight segments)
	private void CarveLHallway(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth)
	{
		// First go X, then go Z
		Vector3I corner = new Vector3I(to.X, from.Y, from.Z);
		CarveSegment(from, corner, 2, map, width, height, depth);
		CarveSegment(corner, to, 2, map, width, height, depth);
	}

	// Wide multi-bend corridor
	private void CarveWideHallway(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth, WorldContext context)
	{
		int steps = (int)((Vector3)from).DistanceTo((Vector3)to) * 2;
		steps = Math.Max(steps, 1);
		for (int i = 0; i <= steps; i++)
		{
			float t = (float)i / steps;
			float x = Mathf.Lerp(from.X, to.X, t);
			float y = Mathf.Lerp(from.Y, to.Y, t);
			float z = Mathf.Lerp(from.Z, to.Z, t);
			CarveRadius((int)x, (int)y, (int)z, 5, map, width, height, depth);
		}
	}

	// --- Shaft Carvers ---

	private void CarveVerticalShaft(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth, BiomeType biome, WorldContext context)
	{
		switch (biome)
		{
			case BiomeType.Cave:
				CarveRaggedShaft(from, to, map, width, height, depth, context);
				break;
			case BiomeType.Tunnel:
				CarveNarrowShaft(from, to, map, width, height, depth);
				break;
			case BiomeType.Cavern:
				CarveSpiralShaft(from, to, map, width, height, depth, context);
				break;
		}
	}

	// Wide irregular shaft with noise
	private void CarveRaggedShaft(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth, WorldContext context)
	{
		int yStart = Math.Min(from.Y, to.Y);
		int yEnd   = Math.Max(from.Y, to.Y);
		for (int y = yStart; y <= yEnd; y++)
		{
			float t     = (float)(y - yStart) / Math.Max(yEnd - yStart, 1);
			float x     = Mathf.Lerp(from.X, to.X, t);
			float z     = Mathf.Lerp(from.Z, to.Z, t);
			float noise = context.Noise.GetNoise2D(x * 0.3f, y * 0.3f) * 5f;
			int radius  = 3 + (int)Math.Abs(noise);
			CarveRadius((int)(x + noise), y, (int)(z + noise), radius, map, width, height, depth);
		}
	}

	// Tight 2-3 voxel shaft
	private void CarveNarrowShaft(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth)
	{
		int yStart = Math.Min(from.Y, to.Y);
		int yEnd   = Math.Max(from.Y, to.Y);
		for (int y = yStart; y <= yEnd; y++)
		{
			float t = (float)(y - yStart) / Math.Max(yEnd - yStart, 1);
			int x   = (int)Mathf.Lerp(from.X, to.X, t);
			int z   = (int)Mathf.Lerp(from.Z, to.Z, t);
			CarveRadius(x, y, z, 2, map, width, height, depth);
		}
	}

	// Spiraling shaft
	private void CarveSpiralShaft(Vector3I from, Vector3I to, int[,,] map,
		int width, int height, int depth, WorldContext context)
	{
		int yStart = Math.Min(from.Y, to.Y);
		int yEnd   = Math.Max(from.Y, to.Y);
		float spiralRadius = 6f;
		for (int y = yStart; y <= yEnd; y++)
		{
			float t      = (float)(y - yStart) / Math.Max(yEnd - yStart, 1);
			float angle  = t * MathF.PI * 4f; // two full rotations
			float baseX  = Mathf.Lerp(from.X, to.X, t);
			float baseZ  = Mathf.Lerp(from.Z, to.Z, t);
			int x        = (int)(baseX + MathF.Cos(angle) * spiralRadius);
			int z        = (int)(baseZ + MathF.Sin(angle) * spiralRadius);
			x            = Math.Clamp(x, 5, width - 5);
			z            = Math.Clamp(z, 5, depth - 5);
			CarveRadius(x, y, z, 3, map, width, height, depth);
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

	// --- Entrance / Exit ---

	private List<Vector3I> GetEntrancesForChunk(int chunkIndex, int width, int depth, int height, WorldContext context)
	{
		if (chunkIndex == 0)
		{
			var rnd = new System.Random(context.Seed);
			int cx = width / 2 + rnd.Next(-10, 10);
			int cz = depth / 2 + rnd.Next(-10, 10);
			cx = Math.Clamp(cx, 20, width - 20);
			cz = Math.Clamp(cz, 20, depth - 20);
			return new List<Vector3I> { new Vector3I(cx, height - 2, cz) };
		}

		var aboveExits = GetExitsForChunk(chunkIndex - 1, width, depth, context);
		var entrances  = new List<Vector3I>();
		foreach (var exit in aboveExits)
			entrances.Add(new Vector3I(exit.X, height - 2, exit.Z));
		return entrances;
	}

	private List<Vector3I> GetExitsForChunk(int chunkIndex, int width, int depth, WorldContext context)
	{
		var rnd   = new System.Random(context.Seed + chunkIndex * 1000);
		int count = rnd.Next(1, 4);
		var exits = new List<Vector3I>();
		for (int i = 0; i < count; i++)
		{
			int x = rnd.Next(20, width - 20);
			int z = rnd.Next(20, depth - 20);
			exits.Add(new Vector3I(x, 2, z));
		}
		return exits;
	}

	private BiomeType GetBiomeForChunk(int chunkIndex, WorldContext context)
	{
		var rnd = new System.Random(context.Seed + chunkIndex * 777);
		return (BiomeType)rnd.Next(0, 3);
	}
}
