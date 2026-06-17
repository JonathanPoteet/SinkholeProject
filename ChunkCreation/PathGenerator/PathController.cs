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
		//TODO create a controller
		// it must take in the biome + start + end vectors
		// depending on location it must decide how to handle the path. needs to use a bias algo to make these decisions.
		// biome decides look/feel of the path (grassy, desert, futuristic) + any odd biases (steep drops?)
	
		public void CarvePath(Vector3I start, Vector3I end, int[,,] map, 
		int width, int height, int depth, BiomeType biome, System.Random rnd, Noise noise, List<Bounds> reservedVolumes)
	{
		// 1. Generate path segments (The "Planning Phase")
		// We store the voxels AND the bounding boxes for the reservation system
		List<Vector3I> pathVoxels = GenerateRaggedPathVoxels(start, end, noise, 3);
		List<Bounds> pathSegments = CreatePathSegments(pathVoxels, 30); // 10 = segment length

		// 2. Validation: Check against Rooms and existing ReservedVolumes
		
		foreach (var segment in pathSegments)
		{
			// Check collision against other paths
			foreach (var reserved in reservedVolumes)
			{
				if (segment.Intersects(reserved, padding: 2))
				{
					// Optional: Decide if you want to skip or merge
					GD.Print("Path intersecting existing reservation.");
				}
			}
		}

		// 3. Commitment: Carve and Reserve
		foreach (var v in pathVoxels)
		{
			// Ensure bounds before carving
			if (v.X >= 0 && v.X < width && v.Y >= 0 && v.Y < height && v.Z >= 0 && v.Z < depth)
			{
				map[v.X, v.Y, v.Z] = 0; // Set to Air
			}
		}

		// Register all path segments as reserved space in this chunk
		reservedVolumes.AddRange(pathSegments);
	}
	
	
	private List<Bounds> CreatePathSegments(List<Vector3I> voxels, int stepSize)
{
	var segments = new List<Bounds>();
	for (int i = 0; i < voxels.Count; i += stepSize)
	{
		int end = Math.Min(i + stepSize, voxels.Count);
		// Create an AABB for this chunk of the path
		segments.Add(CalculateBoundsFromVoxels(voxels.GetRange(i, end - i)));
	}
	return segments;
}

private Bounds CalculateBoundsFromVoxels(List<Vector3I> voxels)
{
	Vector3I min = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
	Vector3I max = new Vector3I(int.MinValue, int.MinValue, int.MinValue);
	foreach (var v in voxels)
	{
		min = new Vector3I(Math.Min(min.X, v.X), Math.Min(min.Y, v.Y), Math.Min(min.Z, v.Z));
		max = new Vector3I(Math.Max(max.X, v.X), Math.Max(max.Y, v.Y), Math.Max(max.Z, v.Z));
	}
	return new Bounds(min, max);
}
	private List<Vector3I> GenerateRaggedPathVoxels(Vector3I start, Vector3I end, Noise noise, int radius) 
{
	List<Vector3I> pathVoxels = new List<Vector3I>();
	
	Vector3 current = new Vector3(start.X, start.Y, start.Z);
	Vector3 target = new Vector3(end.X, end.Y, end.Z);
	
	// Track the last point to avoid adding redundant voxels for the same area
	Vector3I lastAdded = new Vector3I(-999, -999, -999);

	while (current.DistanceTo(target) > 1.0f)
	{
		Vector3 direction = (target - current).Normalized();
		
		// Organic wobble
		float nX = noise.GetNoise3D(current.X * 0.1f, current.Y * 0.1f, current.Z * 0.1f);
		float nY = noise.GetNoise3D(current.X * 0.1f + 50f, current.Y * 0.1f, current.Z * 0.1f);
		float nZ = noise.GetNoise3D(current.X * 0.1f + 100f, current.Y * 0.1f, current.Z * 0.1f);
		
		Vector3 wander = new Vector3(nX, nY, nZ) * 0.8f;
		current += (direction + wander) * 0.5f;

		// Convert to Voxel Space
		Vector3I currentVoxel = new Vector3I((int)current.X, (int)current.Y, (int)current.Z);

		// Only add voxels if we have moved to a new integer position
		if (currentVoxel != lastAdded)
		{
			AddRadiusVoxels(current, radius, pathVoxels);
			lastAdded = currentVoxel;
		}
	}
	
	return pathVoxels;
}
	


		private static void AddRadiusVoxels(Vector3 pos, int radius, List<Vector3I> voxels)
		{
			int r = radius;
			for (int x = -r; x <= r; x++)
				for (int y = -r; y <= r; y++)
					for (int z = -r; z <= r; z++)
						if (x * x + y * y + z * z <= r * r)
							voxels.Add(new Vector3I((int)pos.X + x, (int)pos.Y + y, (int)pos.Z + z));
		}
	
}
