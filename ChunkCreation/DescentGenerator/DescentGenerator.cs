using Godot;
using System;
using System.Collections.Generic;
namespace SinkholeProject.ChunkCreation.DescentGenerator;
using SinkholeProject.World;
using System.Diagnostics;
using System.Linq;
using SinkholeProject.Helper.VoxelUtils;
using SinkholeProject.Helper.Initialization;
using SinkholeProject.Rooms;
using SinkholeProject.RoomController;
using SinkholeProject.PathController;
using SinkholeProject.Configuration;
public partial class DescentGenerator : Node
{
	

	public void CarveSeededVerticalPath(int chunkIndex, WorldContext context)
	{	RoomController rc = new RoomController();
		PathController pc = new PathController();
		var currentChunk = context.WorldChunks[chunkIndex];
		int height = currentChunk.height;
		int width  = currentChunk.width;
		int depth  = currentChunk.depth;
		int[,,] map = currentChunk.MapData;
		List<Room> allRooms = currentChunk.Rooms;
		
		// 1. Grid Configuration (The "Seed" Phase)
		int padding = 2; // Keep everything 10 voxels away from the edges
		int playableWidth = width - (padding * 2);
		int playableHeight = (height) - (padding * 2);
		int playableDepth = depth - (padding * 2);
		
		// 2. Divide the playable space
		int gridDiv = 5;
		ChunkSettings settings = new ChunkSettings(playableWidth, playableHeight, playableDepth, gridDiv, padding);

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
				entryPoints.Add(new Vector3I(exit.Position.X, height - 1, exit.Position.Z));
			}
		}
		GD.Print($"Building entryPoints for Chunk: {chunkIndex}, Exits in prev chunk: {(chunkIndex > 0 ? context.WorldChunks[chunkIndex - 1].ExitPoints.Count : "N/A")}");
			int exitCount = (chunkIndex == 0) ? 1 : context.Rnd.Next(1, 4);
			
			if (chunkIndex > 0) {
				rc.GenerateRooms(allRooms, currentChunk.Biome, chunkIndex, context.Rnd, currentChunk.ReservedVolumes, map, settings);
				BuildDungeonGraph(allRooms, entryPoints, context.Rnd, exitCount);
			} 
			
			List<ChunkExit> generatedExits = GenerateExits(allRooms, playableWidth, playableDepth, padding, chunkIndex);
				currentChunk.ExitPoints = generatedExits;
				GD.Print($"Generated Exits: {generatedExits.Count}");
				
			if (chunkIndex == 0)
				{
					GD.Print("First Chunk. Connecting Entry directly to Exit.");
					
					// Connect each entry point directly to each exit point (Transit mode)
					foreach (var entry in entryPoints)
					{
						foreach (var exit in currentChunk.ExitPoints)
						{
							pc.CarvePath(entry, exit.Position, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
						}
					}
				} else {
				

				foreach (var entry in entryPoints) {
					var startRoom = allRooms.OrderBy(r => r.Center.DistanceTo(entry)).First();
					if (startRoom != null) 
					{
					pc.CarvePath(entry, startRoom.Center, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
					}
				}
				
				foreach (var room in allRooms)
				foreach (var neighbor in room.Neighbors)
				{
					if (allRooms.IndexOf(neighbor) > allRooms.IndexOf(room))
					{
						Vector3I startPoint = rc.GetRoomConnectionPoint(room, room.Center);
						Vector3I endPoint = rc.GetRoomConnectionPoint(neighbor, neighbor.Center);
						pc.CarvePath(startPoint, endPoint, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
					}
				}
		
				foreach (var connection in currentChunk.ExitPoints) {
				Vector3I closestRoom = rc.GetRoomConnectionPoint(connection.ParentRoom, connection.Position);
				pc.CarvePath(closestRoom, connection.Position, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
				}	
			}
			
				
				
				
		
	}

		
public void BuildDungeonGraph(List<Room> rooms, List<Vector3I> entryPoints, System.Random rnd, int exitCount)
{
	// 1. Reset
	foreach (var r in rooms) { r.Neighbors.Clear(); r.Depth = 0; r.IsTerminal = false; }

	foreach (var room in rooms)
	{
		int k = GetWeightedK(rnd);
		var neighbors = rooms
			.Where(r => r != room)
			.OrderBy(r => r.Center.DistanceTo(room.Center))
			.Take(k);

		foreach (var neighbor in neighbors)
		{
			if (!room.Neighbors.Contains(neighbor))
			{
				room.Neighbors.Add(neighbor);
				neighbor.Neighbors.Add(room); // Ensure bidirectional
			}
		}
	}
	
	EnsureFullConnectivity(rooms, entryPoints);
	
	
	// 4. Select exits that are the absolute furthest
	// We order by Depth descending, so the largest depth numbers come first.
	var selected = rooms
		.OrderByDescending(r => r.Depth)
		.Take(exitCount)
		.ToList();

	foreach (var t in selected) { t.IsTerminal = true; t.HasLight = true; }
}

private void EnsureFullConnectivity(List<Room> rooms, List<Vector3I> entryPoints)
{
	// 1. Reset depths
	foreach (var r in rooms) r.Depth = -1;

	// 2. Initialize BFS with all entry points
	var queue = new Queue<Room>();
	foreach (var pt in entryPoints)
	{
		var startRoom = rooms.OrderBy(r => r.Center.DistanceTo(pt)).First();
		if (startRoom.Depth == -1)
		{
			startRoom.Depth = 0;
			queue.Enqueue(startRoom);
		}
	}

	// 3. Multi-Source BFS traversal
	while (queue.Count > 0)
	{
		var current = queue.Dequeue();
		foreach (var neighbor in current.Neighbors)
		{
			if (neighbor.Depth == -1)
			{
				neighbor.Depth = current.Depth + 1;
				queue.Enqueue(neighbor);
			}
		}
	}

	// 4. Bridge any islands (rooms that were unreachable from entry points)
	var islands = rooms.Where(r => r.Depth == -1).ToList();
	var reachable = rooms.Where(r => r.Depth != -1).ToList();

	foreach (var island in islands)
	{
		// Find closest room that is already part of the connected graph
		var bridge = reachable.OrderBy(r => r.Center.DistanceTo(island.Center)).First();
		
		bridge.Neighbors.Add(island);
		island.Neighbors.Add(bridge);
		
		// Update depth for the bridged island
		island.Depth = bridge.Depth + 1;
		reachable.Add(island);
	}
}

private List<ChunkExit> GenerateExits(List<Room> rooms, int playableWidth, int playableDepth, int padding, int chunkIndex)
{
	var exits = new List<ChunkExit>();

	if (chunkIndex == 0)
	{
		Vector3I center = new Vector3I(padding + playableWidth / 2, 0, padding + playableDepth / 2);
		exits.Add(new ChunkExit(center));
		return exits;
	}

	// 1. Find all rooms marked as terminals
	var terminals = rooms.Where(r => r.IsTerminal).ToList();

	// 2. Fallback logic: Use the "Center Room" as the terminal if none exist
	if (terminals.Count == 0 && rooms.Count > 0)
	{
		var centerRoom = rooms.OrderBy(r => 
			Math.Abs(r.Center.X - (padding + playableWidth / 2)) + 
			Math.Abs(r.Center.Z - (padding + playableDepth / 2))
		).First();

		centerRoom.IsTerminal = true;
		terminals.Add(centerRoom);
	}
//
	//// 3. Map terminals to their exit points
	foreach (var room in terminals)
	{
		Vector3I exitPoint = new Vector3I(
			(int)room.Center.X,
			0,
			(int)room.Center.Z
		);
		
		exits.Add(new ChunkExit(exitPoint, room));
	}

	return exits;
}

private bool IsTooClose(Vector3I candidate, List<Vector3I> existing, int minDistance) {
	foreach (var exit in existing) {
		if (candidate.DistanceTo(exit) < minDistance) return true;
	}
	return false;
}

private List<Room> GetUnassignedNeighbors(Room current, List<Room> allRooms, HashSet<Room> assignedRooms)
{
	var unassigned = new List<Room>();

	// Assuming your Room object has a 'PotentialNeighbors' list or similar
	// If you don't have a pre-defined list, we use distance to find 'nearby' rooms
	foreach (var room in allRooms)
	{
		if (assignedRooms.Contains(room)) continue;

		// Logic: Is this room close enough to be considered a 'neighbor'?
		// You can adjust the distance threshold (e.g., 50.0f) to match your room size
		if (room.Center.DistanceTo(current.Center) < 50.0f) 
		{
			unassigned.Add(room);
		}
	}

	return unassigned;
}

private Vector3I CalculateMajorPathExit(List<Room> rooms, int gridDiv, int cellW, int cellD, int chunkIndex, int padding)
{
	// 1. First Chunk Logic: The exit is usually fixed (e.g., center of the far edge)
	if (chunkIndex == 0)
	{
		// Example: Force exit to the center of the far-right edge
		return new Vector3I(
			(gridDiv - 1) * cellW + (cellW / 2) + padding, 
			0, 
			(gridDiv / 2) * cellD + (cellD / 2) + padding
		);
	}

	// 2. Subsequent Chunks: Look for the terminal room
	Room terminalRoom = rooms.LastOrDefault(r => r.Type == RoomType.Major);
	
	if (terminalRoom != null)
	{
		return new Vector3I(terminalRoom.Center.X, 0, terminalRoom.Center.Z);
	}

	// 3. Absolute Fallback: Center of the chunk
	return new Vector3I(gridDiv / 2 * cellW, 0, gridDiv / 2 * cellD);
}

public int GetWeightedK(System.Random rnd)
{
	int roll = rnd.Next(1, 101); // 1 to 100

	if (roll <= 50) return 1;      // 50% chance
	if (roll <= 80) return 2;      // 30% chance (51-75)
	return 3; //20% chance
}
	
}
