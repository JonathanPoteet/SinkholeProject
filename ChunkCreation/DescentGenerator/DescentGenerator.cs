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
	private const int CorridorRadius = 2;

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
		int gridDiv = 7;
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
			var roomVoxels = new HashSet<Vector3I>();
			var roomClearance = new HashSet<Vector3I>();
			var reservedPathVoxels = new HashSet<Vector3I>();
			
			if (chunkIndex > 0) {
				rc.GenerateRooms(allRooms, currentChunk.Biome, chunkIndex, context.Rnd, currentChunk.ReservedVolumes, map, settings);
					roomVoxels = BuildRoomVoxelSet(allRooms);
					roomClearance = BuildRoomClearanceSet(roomVoxels, width, height, depth);
					foreach (var room in allRooms) AssignDoors(room, roomVoxels, width, depth);
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
							GD.Print($"Carving path from Entry: {entry} to Exit: {exit.Position}");
							pc.CarvePath(entry, exit.Position, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, reservedPathVoxels, roomVoxels, roomClearance);
						}
					}
				} else {
				

				foreach (var entry in entryPoints) {
					var startRoom = allRooms.OrderBy(r => r.Center.DistanceTo(entry)).FirstOrDefault();
					if (startRoom != null) 
					{
					foreach (var door in startRoom.DoorCandidates.OrderBy(point => point.DistanceTo(entry)).ToList())
					{
						if (!pc.CarvePath(entry, door, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, reservedPathVoxels, roomVoxels, roomClearance)) continue;
						startRoom.DoorCandidates.Remove(door);
						break;
					}
					}
				}
				
				var processedConnections = new HashSet<(int, int)>();
				foreach (var room in allRooms)
				foreach (var neighbor in room.Neighbors)
				{
					int roomIndex = allRooms.IndexOf(room);
					int neighborIndex = allRooms.IndexOf(neighbor);
					var connectionKey = (Math.Min(roomIndex, neighborIndex), Math.Max(roomIndex, neighborIndex));
					if (processedConnections.Add(connectionKey))
					{
						if (TryConnectRooms(room, neighbor, pc, currentChunk.MapData,
							width, height, depth, currentChunk.Biome, context, reservedPathVoxels, roomVoxels, roomClearance))
						{
							continue;
						}
						else
							GD.PushWarning($"No clear door pair between rooms at {room.Center} and {neighbor.Center} — connection skipped.");
					}
				}
		
				foreach (var connection in currentChunk.ExitPoints) {
				Vector3I closestRoom = connection.ParentRoom.DoorCandidates
					.OrderBy(point => point.DistanceTo(connection.Position)).FirstOrDefault();
				pc.CarvePath(closestRoom, connection.Position, currentChunk.MapData, width, height, depth, currentChunk.Biome, context.Rnd, context.Noise, reservedPathVoxels, roomVoxels, roomClearance);
				}	
			}
			
				
				
				
		
	}

		
public void BuildDungeonGraph(List<Room> rooms, List<Vector3I> entryPoints, System.Random rnd, int exitCount)
{
	foreach (var room in rooms) { room.Neighbors.Clear(); room.Depth = -1; room.IsTerminal = false; }
	if (rooms.Count == 0) return;

	var connected = new HashSet<Room>();
	var root = entryPoints.Count > 0
		? rooms.OrderBy(room => room.Center.DistanceTo(entryPoints[0])).First()
		: rooms[0];
	connected.Add(root);
	root.Depth = 0;

	while (connected.Count < rooms.Count)
	{
		var best = (From: (Room)null, To: (Room)null, Distance: float.MaxValue);
		foreach (var from in connected)
		foreach (var to in rooms)
		{
			if (connected.Contains(to)) continue;
			float distance = from.Center.DistanceTo(to.Center);
			if (distance < best.Distance) best = (from, to, distance);
		}

		best.From.Neighbors.Add(best.To);
		best.To.Neighbors.Add(best.From);
		best.To.Depth = best.From.Depth + 1;
		connected.Add(best.To);
	}

	var selected = rooms.OrderByDescending(room => room.Depth).Take(exitCount).ToList();
	foreach (var terminal in selected) { terminal.IsTerminal = true; terminal.HasLight = true; }
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

private void AssignDoors(Room room, HashSet<Vector3I> roomVoxels, int width, int depth)
{
	room.DoorCandidates.Clear();
	foreach (var point in room.ConnectionPoints)
	{
		bool hasInteriorAboveAndBelow = roomVoxels.Contains(point + Vector3I.Up)
			&& roomVoxels.Contains(point + Vector3I.Down);
		bool xSideWall = !roomVoxels.Contains(point + Vector3I.Right)
			|| !roomVoxels.Contains(point + Vector3I.Left);
		bool zSideWall = !roomVoxels.Contains(point + Vector3I.Back)
			|| !roomVoxels.Contains(point + Vector3I.Forward);
		bool spacedDoorway = xSideWall && point.Z % 3 == 0 || zSideWall && point.X % 3 == 0;
		bool awayFromChunkEdge = point.X >= CorridorRadius + 2
			&& point.X < width - CorridorRadius - 2
			&& point.Z >= CorridorRadius + 2
			&& point.Z < depth - CorridorRadius - 2;
		if (hasInteriorAboveAndBelow && spacedDoorway && awayFromChunkEdge)
			room.DoorCandidates.Add(point);
	}
}

private HashSet<Vector3I> BuildRoomClearanceSet(HashSet<Vector3I> roomVoxels, int width, int height, int depth)
{
	var clearance = new HashSet<Vector3I>();
	foreach (var voxel in roomVoxels)
	for (int x = -CorridorRadius; x <= CorridorRadius; x++)
	for (int y = -CorridorRadius; y <= CorridorRadius; y++)
	for (int z = -CorridorRadius; z <= CorridorRadius; z++)
	{
		if (x * x + y * y + z * z > CorridorRadius * CorridorRadius) continue;
		var expanded = voxel + new Vector3I(x, y, z);
		if (expanded.X >= 0 && expanded.X < width && expanded.Y >= 0 && expanded.Y < height && expanded.Z >= 0 && expanded.Z < depth)
			clearance.Add(expanded);
	}
	return clearance;
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

private bool TryPickDoors(Room a, Room b, List<Room> allRooms, out Vector3I doorA, out Vector3I doorB)
{
	doorA = default; doorB = default;
	var pairs = new List<(Vector3I da, Vector3I db, float d)>();
	foreach (var da in a.DoorCandidates)
		foreach (var db in b.DoorCandidates)
			pairs.Add((da, db, da.DistanceTo(db)));

	foreach (var (da, db, _) in pairs.OrderBy(p => p.d))
	{
		doorA = da; doorB = db;
		return true;
	}
	return false;
}

private bool TryConnectRooms(Room a, Room b, PathController pathController, int[,,] map,
	int width, int height, int depth, BiomeType biome, WorldContext context,
	HashSet<Vector3I> reservedPathVoxels, HashSet<Vector3I> roomVoxels, HashSet<Vector3I> roomClearance)
{
	const int candidateLimit = 8;
	var candidates = new PriorityQueue<(Vector3I DoorA, Vector3I DoorB), float>();
	foreach (var doorA in a.DoorCandidates)
	foreach (var doorB in b.DoorCandidates)
	{
		float distance = doorA.DistanceSquaredTo(doorB);
		if (candidates.Count == candidateLimit)
		{
			candidates.TryPeek(out _, out float worstPriority);
			if (-distance <= worstPriority) continue;
			candidates.Dequeue();
		}
		candidates.Enqueue((doorA, doorB), -distance);
	}

	var orderedCandidates = new List<(Vector3I DoorA, Vector3I DoorB)>();
	while (candidates.TryDequeue(out var pair, out _)) orderedCandidates.Add(pair);
	orderedCandidates.Reverse();
	foreach (var (doorA, doorB) in orderedCandidates)
	{
		if (!pathController.CarvePath(doorA, doorB, map, width, height, depth, biome,
			context.Rnd, context.Noise, reservedPathVoxels, roomVoxels, roomClearance)) continue;
		a.DoorCandidates.Remove(doorA);
		b.DoorCandidates.Remove(doorB);
		return true;
	}
	return false;
}
	
}
