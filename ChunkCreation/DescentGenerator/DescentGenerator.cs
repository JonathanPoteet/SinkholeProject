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
	
	public class PathSegment {
		public List<Vector3I> Voxels = new List<Vector3I>();
		public PathSegment(List<Vector3I> pathVoxels)
		{
			Voxels = pathVoxels;
		}
		//public PathProfile Profile { get; set; }
	}
	
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

		BiomeType biome = SetBiomeForChunk(chunkIndex, context);
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
		rc.GenerateRooms(allRooms, rnd, width, height, depth, biome, chunkIndex);
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
				Vector3I startPoint = rc.GetRoomConnectionPoint(minor, minor.Center);
				Vector3I endPoint = rc.GetRoomConnectionPoint(closestMajor, closestMajor.Center);
				pc.CarvePath(startPoint, endPoint, map, width, height, depth, biome, context);
			}

			// 3. Carve the Major Spine
			foreach (var room in majorRooms)
			{
				foreach (var neighbor in room.Neighbors)
				{
					// Ensure we only carve once per connection
					if (majorRooms.IndexOf(neighbor) > majorRooms.IndexOf(room))
					{
						Vector3I startPoint = rc.GetRoomConnectionPoint(room, room.Center);
						Vector3I endPoint = rc.GetRoomConnectionPoint(neighbor, neighbor.Center);
						pc.CarvePath(startPoint, endPoint, map, width, height, depth, biome, context);
					}
				}
			}

		// 3. GENERATE EXIT POINTS (For the next chunk)
		List<Vector3I> generatedExits = GenerateExits(allRooms, rnd, width, depth, 30);

	// 2. Save them to the current chunk's data structure
		currentChunk.ExitPoints = generatedExits;
		
		// 4. CARVE ROOMS
		foreach (var room in majorRooms) {
			rc.CarveRoom(room, map, width, height, depth, biome, context);
		}
		foreach (var room in minorRooms) {
			rc.CarveRoom(room, map, width, height, depth, biome, context);
		}
		// 5. CONNECT EVERYTHING
		// A. Connect Entry -> First Room
		foreach (var entry in entryPoints) {
			Room closestRoom = rc.FindNearestRoom(entry, allRooms);
			pc.CarvePath(entry, closestRoom.Center, map, width, height, depth, biome, context);
		}

		// B. Connect Last Room -> Exits //should be carve closest room to exits
		foreach (var exit in currentChunk.ExitPoints) {
			Room closestRoom = rc.FindNearestRoom(exit, allRooms);
			pc.CarvePath(closestRoom.Center, exit, map, width, height, depth, biome, context);
		}
		
	}

		



// sets the biome type	
	private BiomeType SetBiomeForChunk(int chunkIndex, WorldContext context)
	{
		var rnd = new System.Random(context.Seed + chunkIndex);
		return (BiomeType)rnd.Next(0, 3);
	}
	
		private List<Vector3I> GenerateExits(List<Room> rooms, System.Random rnd, int width, int depth, int minDistance) 
		{
			List<Vector3I> exits = new List<Vector3I>();
		
			// 1. Force the Major Exit
			exits.Add(CalculateMajorPathExit(rooms));

			// 2. Generate random exits on the Y = 0 plane
			int attempts = 0;
			int maxCount = rnd.Next(0, 4);
			while (exits.Count < maxCount && attempts < 200) 
			{
				// Strictly at Y = 0
				Vector3I candidate = new Vector3I(
					rnd.Next(20, width - 20), 
					0, 
					rnd.Next(20, depth - 20)
				);
				
				if (!IsTooClose(candidate, exits, minDistance)) 
				{
					exits.Add(candidate);
				}
				attempts++;
			}
			return exits;
	}

private bool IsTooClose(Vector3I candidate, List<Vector3I> existing, int minDistance) {
	foreach (var exit in existing) {
		if (candidate.DistanceTo(exit) < minDistance) return true;
	}
	return false;
}

private Vector3I CalculateMajorPathExit(List<Room> rooms)
{
	// 1. Get the terminal room of the major path
	// Assuming your major path is a list or the last room added 
	// to your 'rooms' list is the terminus.
	Room terminalRoom = rooms.LastOrDefault(r => r.Type == RoomType.Major);

	if (terminalRoom == null) 
	{
		// Fallback: Use the center of the first room if something went wrong
		return rooms.FirstOrDefault()?.Center ?? new Vector3I(0, 0, 0);
	}

	// 2. Determine an exit point on the boundary
	// We pick a point on the "bottom" face (YMin) of the major room 
	// to ensure it continues downward for chunk streaming.
	return new Vector3I(
		terminalRoom.Center.X,
		0, // Exit at the bottom boundary
		terminalRoom.Center.Z
	);
}
	
}
