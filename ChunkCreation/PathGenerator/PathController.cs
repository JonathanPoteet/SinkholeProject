using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.Helper.VoxelUtils;
namespace SinkholeProject.PathController;
using SinkholeProject.Helper.Initialization;
using SinkholeProject.World;
using SinkholeProject.Helper.Volumes;
public partial class PathController : Node
{
	private const int CorridorRadius = 2;
		//TODO create a controller
		// it must take in the biome + start + end vectors
		// depending on location it must decide how to handle the path. needs to use a bias algo to make these decisions.
		// biome decides look/feel of the path (grassy, desert, futuristic) + any odd biases (steep drops?)
	
		public bool CarvePath(Vector3I start, Vector3I end, int[,,] map,
		int width, int height, int depth, BiomeType biome, System.Random rnd, Noise noise,
		HashSet<Vector3I> reservedPathVoxels,
		HashSet<Vector3I> roomVoxels, HashSet<Vector3I> roomClearance)
	{
		GD.Print($"Carving path from {start} to {end} in biome {biome}");
		if (!InBounds(start, width, height, depth) || !InBounds(end, width, height, depth)) return false;

		var path = FindPath(start, end, width, height, depth, roomVoxels, roomClearance, reservedPathVoxels);
		if (path == null)
		{
			GD.PushWarning($"No unobstructed route found from {start} to {end}.");
			return false;
		}

		var carvedVoxels = new HashSet<Vector3I>();
		foreach (var point in path)
		{
			for (int x = -CorridorRadius; x <= CorridorRadius; x++)
			for (int y = -CorridorRadius; y <= CorridorRadius; y++)
			for (int z = -CorridorRadius; z <= CorridorRadius; z++)
			{
				if (x * x + y * y + z * z > CorridorRadius * CorridorRadius) continue;
				var voxel = point + new Vector3I(x, y, z);
				if (!IsCarvable(voxel, width, height, depth, start, end)) continue;
				if (roomVoxels.Contains(voxel) && !IsDoorOpening(voxel, start, end)) continue;
				map[voxel.X, voxel.Y, voxel.Z] = 0;
				carvedVoxels.Add(voxel);
			}
		}

		foreach (var voxel in carvedVoxels)
			reservedPathVoxels.Add(voxel);
		return true;
	}

	private static readonly Vector3I[] Directions =
	{
		new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0),
		new(0, -1, 0), new(0, 0, 1), new(0, 0, -1)
	};

	private static List<Vector3I> FindPath(Vector3I start, Vector3I end, int width, int height, int depth,
		HashSet<Vector3I> roomVoxels, HashSet<Vector3I> roomClearance, HashSet<Vector3I> reservedVoxels)
	{
		var frontier = new PriorityQueue<Vector3I, int>();
		var previous = new Dictionary<Vector3I, Vector3I>();
		var costs = new Dictionary<Vector3I, int> { [start] = 0 };
		frontier.Enqueue(start, ManhattanDistance(start, end));

		while (frontier.TryDequeue(out var current, out int queuedPriority))
		{
			if (queuedPriority != costs[current] + ManhattanDistance(current, end)) continue;
			if (current == end) break;

			foreach (var direction in Directions)
			{
				var next = current + direction;
				if (!IsCarvable(next, width, height, depth, start, end)
					|| IsBlocked(next, start, end, roomVoxels, roomClearance)) continue;

				int nextCost = costs[current] + 1 + (reservedVoxels.Contains(next) ? 20 : 0);
				if (costs.TryGetValue(next, out int knownCost) && knownCost <= nextCost) continue;
				costs[next] = nextCost;
				previous[next] = current;
				frontier.Enqueue(next, nextCost + ManhattanDistance(next, end));
			}
		}

		if (start != end && !previous.ContainsKey(end)) return null;
		var path = new List<Vector3I> { end };
		while (path[^1] != start) path.Add(previous[path[^1]]);
		path.Reverse();
		return path;
	}

	private static bool IsBlocked(Vector3I point, Vector3I start, Vector3I end,
		HashSet<Vector3I> roomVoxels, HashSet<Vector3I> roomClearance)
	{
		return roomClearance.Contains(point) && !IsDoorOpening(point, start, end)
			|| roomVoxels.Contains(point) && !IsDoorOpening(point, start, end);
	}

	private static bool IsDoorOpening(Vector3I point, Vector3I start, Vector3I end)
		=> point.DistanceSquaredTo(start) <= CorridorRadius * CorridorRadius
			|| point.DistanceSquaredTo(end) <= CorridorRadius * CorridorRadius;

	private static int ManhattanDistance(Vector3I a, Vector3I b)
		=> Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);

	private static bool InBounds(Vector3I point, int width, int height, int depth)
		=> point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height && point.Z >= 0 && point.Z < depth;

	private static bool IsCarvable(Vector3I point, int width, int height, int depth, Vector3I start, Vector3I end)
	{
		if (point.Y < 0 || point.Y >= height) return false;

		bool insideSideMargins = point.X >= CorridorRadius + 1 && point.X < width - CorridorRadius - 1
			&& point.Z >= CorridorRadius + 1 && point.Z < depth - CorridorRadius - 1;
		if (!insideSideMargins) return false;

		if (point.Y >= 1 && point.Y < height - 1) return true;
		bool startPortal = (start.Y == 0 || start.Y == height - 1)
			&& point.DistanceSquaredTo(start) <= CorridorRadius * CorridorRadius;
		bool endPortal = (end.Y == 0 || end.Y == height - 1)
			&& point.DistanceSquaredTo(end) <= CorridorRadius * CorridorRadius;
		return startPortal || endPortal;
	}
	
}
