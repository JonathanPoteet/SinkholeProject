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
using SinkholeProject.Configuration;
public partial class RoomController : Node
{
	
	public void GenerateRooms(List<Room> rooms, BiomeType biome, int chunkIndex, System.Random rnd, List<Bounds> reservedVolumes, int[,,] map, ChunkSettings settings)
{
	var candidates = new Dictionary<Vector3I, Candidate>();
	for (int x = 1; x < settings.GridDiv - 1; x++)
	for (int y = 1; y < settings.GridDiv - 1; y++)
	for (int z = 1; z < settings.GridDiv - 1; z++)
	{
		if (rnd.NextDouble() < 0.2) 
		{
			// Inside your generation loop
			Vector3I cellCenter = GetCellCenter(x, y, z, settings.CellW, settings.CellH, settings.CellD);
			candidates[new Vector3I(x, y, z)] = new Candidate(x, y, z, cellCenter);
		}
	}

	var processed = new HashSet<Vector3I>();

	foreach (var pos in candidates.Keys)
	{
		if (processed.Contains(pos)) continue;

		// Start a new Room
		var newRoom = new Room(settings);
		
		// Recursive search: Find all neighbors that should be in this room
		// This is a simple Breadth-First Search (BFS)
		Queue<Vector3I> queue = new Queue<Vector3I>();
		queue.Enqueue(pos);
		
		// Define the 6 directions for a 3D grid (X, Y, Z axis)
		Vector3I[] directions = 
		{
			new Vector3I(1, 0, 0), new Vector3I(-1, 0, 0),
			new Vector3I(0, 1, 0), new Vector3I(0, -1, 0),
			new Vector3I(0, 0, 1), new Vector3I(0, 0, -1)
		};

		while(queue.Count > 0 && newRoom.Cells.Count < 2)
		{
			var current = queue.Dequeue();
			
			// Skip if already processed (handles cases where a cell is added to the queue twice)
			if (processed.Contains(current)) continue;
			
			newRoom.AddCell(candidates[current].GridPosition);
			processed.Add(current);

			// --- 6-Way Neighbor Check ---
			foreach (var dir in directions)
			{
				Vector3I neighborPos = current + dir;

				// Check if the neighbor exists in your candidates pool and hasn't been visited
				if (candidates.ContainsKey(neighborPos) && !processed.Contains(neighborPos))
				{
					queue.Enqueue(neighborPos);
				}
			}
		}
		newRoom.FinalizeCenter(); //sets a center
		rooms.Add(newRoom);
	}

	//// 3. Post-Process: Ensure classification matches the structure
	//var sortedRooms = rooms.OrderByDescending(r => r.GetTotalVolume()).ToList();
	//for (int i = 0; i < sortedRooms.Count; i++)
	//{
		//sortedRooms[i].Type = (i < 8) ? RoomType.Major : RoomType.Minor;
	//}
	
	
	//finally carve the rooms
	GD.Print($"Rooms generated: {rooms.Count}");
	var roomVoxels = BuildRoomVoxelSet(rooms);
	foreach (var room in rooms)
		CarveRoom(room, map, biome, roomVoxels);
	
	
	
}

public class Candidate
{
	public Vector3I GridPosition { get; } // The x, y, z index in your grid
	public Vector3I WorldPosition { get; } // The center point in world space
	
	public Candidate(int x, int y, int z, Vector3I worldCenter)
	{
		GridPosition = new Vector3I(x, y, z);
		WorldPosition = worldCenter;
	}

	// Helper to check if this candidate is adjacent to another in the grid
	public bool IsAdjacentTo(Candidate other)
	{
		int dist = Math.Abs(GridPosition.X - other.GridPosition.X) +
				   Math.Abs(GridPosition.Y - other.GridPosition.Y) +
				   Math.Abs(GridPosition.Z - other.GridPosition.Z);
		
		return dist == 1; // Returns true if they are direct neighbors (no diagonals)
	}
}

private Vector3I GetCellCenter(int x, int y, int z, int cellW, int cellH, int cellD)
{
	// The start of the cell in world space
	int startX = (x * cellW);
	int startY = (y * cellH);
	int startZ = (z * cellD);

	// The center is the start plus half the cell dimension
	return new Vector3I(
		startX + (cellW / 2),
		startY + (cellH / 2),
		startZ + (cellD / 2)
	);
}
	
	public Vector3I GetRoomConnectionPoint(Room startRoom, Vector3I targetCenter)
{
	Vector3I bestPoint = Vector3I.Zero;
	float minDistance = float.MaxValue;

	// Only iterate through the pre-filtered edge voxels!
	foreach (var point in startRoom.ConnectionPoints)
	{
		float dist = (targetCenter - point).LengthSquared();
		if (dist < minDistance)
		{
			minDistance = dist;
			bestPoint = point;
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
			float dist = ((Vector3)room.Center).DistanceTo((Vector3)point);
			if (dist < bestDist)
			{
				bestDist = dist;
				nearest = room;
			}
		}
		return nearest;
	}
	
	private void CarveRoom(Room room, int[,,] map, BiomeType biome, HashSet<Vector3I> roomVoxels)
	{
		switch (biome)
		{
			case BiomeType.Cave:
			case BiomeType.Cavern:
			default:
				CarveCavernRoom(room, map, roomVoxels);
				break;
		}
	}

		private void CarveCavernRoom(Room room, int[,,] map, HashSet<Vector3I> roomVoxels)
		{
			var roomSpecificVoxels = new HashSet<Vector3I>();
			foreach (var cell in room.Cells)
			{
				var bounds = room.GetBoundsForCell(cell);
				for (int x = bounds.Min.X; x < bounds.Max.X; x++)
				for (int y = bounds.Min.Y; y < bounds.Max.Y; y++)
				for (int z = bounds.Min.Z; z < bounds.Max.Z; z++)
					roomSpecificVoxels.Add(new Vector3I(x, y, z));
			}

			foreach (var voxel in roomSpecificVoxels)
			{
				// 3. Check 6-way neighbors
				bool isEdge = false;
				Vector3I[] directions = {
					new Vector3I(1,0,0), new Vector3I(-1,0,0),
					new Vector3I(0,1,0), new Vector3I(0,-1,0),
					new Vector3I(0,0,1), new Vector3I(0,0,-1)
				};

				foreach (var dir in directions)
				{
					// If a neighbor is NOT in our set, then this voxel is on the boundary
					if (!roomSpecificVoxels.Contains(voxel + dir))
					{
						isEdge = true;
						break;
					}
				}

				// 4. Assign value: Walls for edge, Empty for interior
				if (isEdge)
				{
					map[voxel.X, voxel.Y, voxel.Z] = 1; // Wall
					// Collect this as a potential spot for a doorway/connection
					room.ConnectionPoints.Add(voxel);
				}
				else
				{
					map[voxel.X, voxel.Y, voxel.Z] = 0; // Air
				}
			}
		}

		private HashSet<Vector3I> BuildRoomVoxelSet(List<Room> rooms)
		{
			var voxels = new HashSet<Vector3I>();
			foreach (var room in rooms)
			foreach (var cell in room.Cells)
			{
				var bounds = room.GetBoundsForCell(cell);
				for (int x = bounds.Min.X; x < bounds.Max.X; x++)
				for (int y = bounds.Min.Y; y < bounds.Max.Y; y++)
				for (int z = bounds.Min.Z; z < bounds.Max.Z; z++)
					voxels.Add(new Vector3I(x, y, z));
			}
			return voxels;
		}

}
