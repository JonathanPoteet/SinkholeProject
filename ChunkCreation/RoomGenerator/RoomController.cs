using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.World;
using SinkholeProject.Helper.VoxelUtils;
using SinkholeProject.Helper.Initialization;
using SinkholeProject.Rooms;
namespace SinkholeProject.RoomController;
using SinkholeProject.World;
using SinkholeProject.Helper.Volumes;
using System.Linq;
public partial class RoomController : Node
{
	
	public void GenerateRooms(List<Room> rooms, int cellW, int cellH, int cellD, BiomeType biome, int chunkIndex, System.Random rnd, List<Bounds> reservedVolumes, int[,,] map, int gridDiv, int padding)
{
		// Place one "Major" seed per grid cell
	for (int x = 0; x < gridDiv; x++)
	for (int y = 0; y < gridDiv; y++)
	for (int z = 0; z < gridDiv; z++)
	{
		if (rnd.NextDouble() < 0.2) 
		{
				Vector3I cellCenter = new Vector3I(
				padding + (x * cellW) + (cellW / 2),
				padding + (y * cellH) + (cellH / 2),
				padding + (z * cellD) + (cellD / 2)
			);
		
			// Create a base volume for this seed
			var seedBounds = new Bounds(cellCenter, rnd.Next(12, 18), rnd.Next(12, 18), rnd.Next(12, 18));
			rooms.Add(new Room(cellCenter, seedBounds, RoomType.Major));
			reservedVolumes.Add(seedBounds);
		}
	}

	// 2. Growth Phase (The "Cluster" Phase)
	// Add additional volumes that are guaranteed to be near existing rooms
	int extraVolumes = 10;
	for (int i = 0; i < extraVolumes; i++)
	{
		// Pick an existing room to grow from
		var targetRoom = rooms[rnd.Next(rooms.Count)];
		Vector3I growthCenter = targetRoom.Center + new Vector3I(rnd.Next(-10, 10), rnd.Next(-10, 10), rnd.Next(-10, 10));
		
		var newBounds = new Bounds(growthCenter, rnd.Next(8, 12), rnd.Next(8, 12), rnd.Next(8, 12));
		
		// Merge if it hits, otherwise add as new volume to that room
		var collidingRoom = rooms.FirstOrDefault(r => r.Intersects(newBounds));
		if (collidingRoom != null) collidingRoom.AddVolume(newBounds);
		else rooms.Add(new Room(growthCenter, newBounds, RoomType.Minor));
		
		reservedVolumes.Add(newBounds);
	}

	// 3. Post-Process: Ensure classification matches the structure
	var sortedRooms = rooms.OrderByDescending(r => r.GetTotalVolume()).ToList();
	for (int i = 0; i < sortedRooms.Count; i++)
	{
		sortedRooms[i].Type = (i < 8) ? RoomType.Major : RoomType.Minor;
	}
	
	
	//finally carve the rooms
	
	foreach (var room in rooms) {
		CarveRoom(room, map, biome);
	}
	
	
	
}
	
	//establishes where the pathing starts/ends. From where a room will allow it
	public Vector3I GetRoomConnectionPoint(Room startRoom, Vector3I targetCenter)
	{
		// Initialize with a far-away point
		Vector3I bestPoint = startRoom.Center;
		float minDistance = float.MaxValue;

		// Iterate through all volumes to find which one is closest to the target
		foreach (var volume in startRoom.Volumes)
		{
			// Clamp the target center to the current volume's bounds to find the 
			// closest point on/inside this specific volume
			Vector3I closestPoint = new Vector3I(
				Math.Clamp(targetCenter.X, volume.Min.X, volume.Max.X),
				Math.Clamp(targetCenter.Y, volume.Min.Y, volume.Max.Y),
				Math.Clamp(targetCenter.Z, volume.Min.Z, volume.Max.Z)
			);

			// Calculate distance from this point to the target center
			float dist = (targetCenter - closestPoint).LengthSquared(); // Use squared for performance

			if (dist < minDistance)
			{
				minDistance = dist;
				bestPoint = closestPoint;
			}
		}

		return bestPoint;
	}
	
	public Room FindNearestRoom(Vector3I point, List<Room> rooms)
	{
		Room nearest = null;
		float bestDist = float.MaxValue;
		foreach (var room in rooms)
		{
			//todo: possibly limit amount of doorways per room
			float dist = ((Vector3)room.Center).DistanceTo((Vector3)point);
			if (dist < bestDist)
			{
				bestDist = dist;
				nearest = room;
			}
		}
		return nearest;
	}
	
	private void CarveRoom(Room room, int[,,] map,
		BiomeType biome)
	{
		switch (biome)
		{
			case BiomeType.Cave:
			case BiomeType.Cavern:
			default:
				CarveCavernRoom(room, map);
				break;
		}
	}

	// Large cube
	private void CarveCavernRoom(Room room, int[,,] map)
	{
		// Get array bounds dynamically from the map itself
		int mapW = map.GetLength(0);
		int mapH = map.GetLength(1);
		int mapD = map.GetLength(2);

		foreach (var volume in room.Volumes)
		{
			// Clamp coordinates to the array bounds automatically
			int xStart = Math.Max(0, volume.Min.X);
			int xEnd   = Math.Min(mapW - 1, volume.Max.X);
			
			int yStart = Math.Max(0, volume.Min.Y);
			int yEnd   = Math.Min(mapH - 1, volume.Max.Y);
			
			int zStart = Math.Max(0, volume.Min.Z);
			int zEnd   = Math.Min(mapD - 1, volume.Max.Z);

			for (int x = xStart; x <= xEnd; x++)
			for (int y = yStart; y <= yEnd; y++)
			for (int z = zStart; z <= zEnd; z++)
			{
				// Now you know x, y, z are guaranteed to be within map bounds!
				VoxelUtils.SafeCarve(x, y, z, map, mapW, mapH, mapD);
			}
		}
	}

}
