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
public partial class DescentGenerator : Node
{
	
	// Helper method to keep things bidirectional
	private void Connect(Room a, Room b)
	{
		if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
		if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
	}
	

	public void CarveSeededVerticalPath(int chunkIndex, WorldContext context)
	{	RoomController rc = new RoomController();
		PathController pc = new PathController();
		var currentChunk = context.WorldChunks[chunkIndex];
		int height = currentChunk.height;
		int width  = currentChunk.width;
		int depth  = currentChunk.depth;
		int[,,] map = currentChunk.MapData;
		// 1. Grid Configuration (The "Seed" Phase)
		int padding = 10; // Keep everything 10 voxels away from the edges
		int playableWidth = width - (padding * 2);
		int playableHeight = (height) - (padding * 2);
		int playableDepth = depth - (padding * 2);

		// 2. Divide the playable space
		int gridDiv = 4;
		int cellW = playableWidth / gridDiv;
		int cellH = playableHeight / gridDiv;
		int cellD = playableDepth / gridDiv;

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
		
		int exitCount = (chunkIndex == 0) ? 1 : context.Rnd.Next(1, 4);
		
			
				List<Room> allRooms = currentChunk.Rooms;
			if (chunkIndex > 0) {
				
				// 2. GENERATE ROOMS
				rc.GenerateRooms(allRooms, cellW, cellH, cellD, currentChunk.Biome, chunkIndex, context.Rnd, currentChunk.ReservedVolumes, map, gridDiv, padding);


				// 1. Connect the Major Spine (The Golden Path)
				BuildDungeonGraph(allRooms, entryPoints, context.Rnd, exitCount);
				
				


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
	}

		// 3. GENERATE EXIT POINTS (For the next chunk)
		List<ChunkExit> generatedExits = GenerateExits(allRooms, playableWidth, playableDepth, padding, chunkIndex);
//
	//// 2. Save them to the current chunk's data structure
		currentChunk.ExitPoints = generatedExits;
		GD.Print($"Generated Exits: {generatedExits.Count}");
		//
		//// 5. CONNECT EVERYTHING
		//// A. Connect Entry -> First Room
		foreach (var entry in entryPoints) {
			Room closestRoom = rc.FindNearestRoom(entry, allRooms);
			if (closestRoom != null) 
			{
			pc.CarvePath(entry, closestRoom.Center, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
			} else {
				foreach (var exit in currentChunk.ExitPoints) {
					pc.CarvePath(entry, exit.Position, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
				}
			}
		}
		var terminals = allRooms.Where(r => r.IsTerminal).ToList();
		//// B. Connect Last Room -> Exits //should carve from terminal room to exit
		if (chunkIndex > 0) {
			foreach (var connection in currentChunk.ExitPoints) {
				Vector3I closestRoom = rc.GetRoomConnectionPoint(connection.ParentRoom, connection.Position);
				pc.CarvePath(closestRoom, connection.Position, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, currentChunk.ReservedVolumes);
			}
		}
		
	}

		
		public void BuildDungeonGraph(List<Room> rooms, List<Vector3I> entryPoints, System.Random rnd, int exitCount)
		{
			// 1. Reset state
			foreach (var room in rooms) {
				room.Neighbors.Clear();
				room.Depth = -1;
			}

			Queue<Room> queue = new Queue<Room>();

			// 2. Initialize from all entry points
			foreach (var entry in entryPoints)
			{
				var startRoom = rooms.OrderBy(r => r.Center.DistanceTo(entry)).FirstOrDefault();
				if (startRoom != null && startRoom.Depth == -1)
				{
					startRoom.Depth = 0;
					queue.Enqueue(startRoom);
				}
			}

	// 3. Process to ensure all rooms are visited
	// We maintain a list of 'unvisited' to catch rooms that BFS didn't reach
	while (queue.Count > 0 || rooms.Any(r => r.Depth == -1))
	{
		// If queue is empty but rooms remain, jump to the nearest unvisited room
		if (queue.Count == 0)
		{
			var next = rooms.Where(r => r.Depth == -1)
							.OrderBy(r => rooms.Where(x => x.Depth != -1).Min(x => x.Center.DistanceTo(r.Center)))
							.First();
			next.Depth = 0; 
			queue.Enqueue(next);
		}

		var current = queue.Dequeue();

		// Find neighbors: We MUST ensure we connect to unvisited rooms
		var candidates = rooms.Where(r => r != current)
							  .OrderBy(r => current.Center.DistanceTo(r.Center))
							  .Take(3); // Increase to 3 to ensure better connectivity

		foreach (var neighbor in candidates)
		{
			Connect(current, neighbor);

			if (neighbor.Depth == -1)
			{
				neighbor.Depth = current.Depth + 1;
				queue.Enqueue(neighbor);
			}
		}
	}
			
	// A good terminal is a room with only 1 neighbor (a leaf) that isn't an entry point.
	// If you have few leaves, fallback to rooms with high depth.
	var potentialTerminals = rooms
		.Where(r => !entryPoints.Any(e => r.Center.DistanceTo(e) < 5)) // Adjust distance as needed
		.OrderByDescending(r => r.Depth)
		.ToList();
		

	// 2. Select exactly the number of exits available
	// Shuffle and take exactly the number of entryPoints
	var selectedTerminals = potentialTerminals
		.OrderBy(r => rnd.Next())
		.Take(Math.Max(1, exitCount)) 
		.ToList();
	
	GD.Print($"Assigned {selectedTerminals.Count} terminals for {exitCount} exits.");

	// 3. Assign terminal status
	foreach (var room in selectedTerminals)
	{
		room.IsTerminal = true;
		room.HasLight = true;
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

	// 3. Map terminals to their exit points
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
	
}
