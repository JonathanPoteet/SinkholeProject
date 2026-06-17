using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.World;
using SinkholeProject.Helper.VoxelUtils;
using SinkholeProject.Helper.Initialization;
using SinkholeProject.Rooms;
namespace SinkholeProject.RoomController;
public partial class RoomController : Node
{
	
	public void GenerateRooms(List<Room> rooms, System.Random rnd, int width, int height, int depth, BiomeType biome, int chunkIndex)
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
					
					var newRoom = new Room(
						new Vector3I(roomX, roomY, roomZ), 
						roomW, 
						roomH, 
						roomD, 
						type
					);
					// 2. Check for collisions
					bool collision = false;
					foreach (var existingRoom in rooms)
					{
						if (newRoom.Bounds.Intersects(existingRoom.Bounds))
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
	
	//establishes where the pathing starts/ends. From where a room will allow it
	public Vector3I GetRoomConnectionPoint(Room startRoom, Vector3I targetCenter)
	{
		Vector3I startCenter = startRoom.Center;
		Vector3I direction = targetCenter - startCenter;

		// Calculate half-extents from the Bounds
		float halfW = (startRoom.Bounds.Max.X - startRoom.Bounds.Min.X) / 2.0f;
		float halfH = (startRoom.Bounds.Max.Y - startRoom.Bounds.Min.Y) / 2.0f;
		float halfD = (startRoom.Bounds.Max.Z - startRoom.Bounds.Min.Z) / 2.0f;

		// Avoid division by zero if direction is 0 in any axis
		float ratioX = direction.X != 0 ? halfW / Math.Abs(direction.X) : float.MaxValue;
		float ratioY = direction.Y != 0 ? halfH / Math.Abs(direction.Y) : float.MaxValue;
		float ratioZ = direction.Z != 0 ? halfD / Math.Abs(direction.Z) : float.MaxValue;

		float minRatio = Math.Min(ratioX, Math.Min(ratioY, ratioZ));

		return new Vector3I(
			startCenter.X + (int)(direction.X * minRatio),
			startCenter.Y + (int)(direction.Y * minRatio),
			startCenter.Z + (int)(direction.Z * minRatio)
		);
	}
	
	public Room FindNearestRoom(Vector3I point, List<Room> rooms)
	{
		Room nearest = rooms[0];
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
	
	public void CarveRoom(Room room, int[,,] map, int width, int height, int depth,
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

	// Large cube
	private void CarveCavernRoom(Room room, int[,,] map, int width, int height, int depth, WorldContext context)
	{
		// Calculate the actual voxel boundaries for this room
		int xStart = room.Bounds.Min.X;
		int xEnd   = room.Bounds.Max.X;
		
		int yStart = room.Bounds.Min.Y;
		int yEnd   = room.Bounds.Max.Y;
		
		int zStart = room.Bounds.Min.Z;
		int zEnd   = room.Bounds.Max.Z;

		// Iterate through the box and carve
		for (int x = xStart; x <= xEnd; x++)
		{
			for (int y = yStart; y <= yEnd; y++)
			{
				for (int z = zStart; z <= zEnd; z++)
				{
					// We use SafeCarve to turn Solid (0) into Air (1)
					VoxelUtils.SafeCarve(x, y, z, map, width, height, depth);
				}
			}
		}
	}

	
}
