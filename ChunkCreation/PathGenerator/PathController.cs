using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.Helper.VoxelUtils;
using SinkholeProject.World;
namespace SinkholeProject.PathController;
using SinkholeProject.Helper.Initialization;
public partial class PathController : Node
{
		//TODO create a controller
		// it must take in the biome + start + end vectors
		// depending on location it must decide how to handle the path. needs to use a bias algo to make these decisions.
		// biome decides look/feel of the path (grassy, desert, futuristic) + any odd biases (steep drops?)
	
			public void CarvePath(Vector3I start, Vector3I end, int[,,] map,
				int width, int height, int depth, BiomeType biome, WorldContext context)
			{
				//TODO create a controller
				// it must take in the biome + start + end vectors
				// depending on location it must decide how to handle the path. needs to use a bias algo to make these decisions.
				// biome decides look/feel of the path (grassy, desert, futuristic) + any odd biases (steep drops?)
				CarveRaggedShaft(start, end, map, width, height, depth, context);
			}
	
	
	
	
	// Generates a list of voxels representing the path (the "Ghost Path")
		private void CarveRaggedShaft(Vector3I start, Vector3I end, int[,,] map, 
	int width, int height, int depth, WorldContext context) {
		// Inside CarveRaggedShaft...
		// Check if the area is already empty before carving
		Vector3 current = new Vector3(start.X, start.Y, start.Z);
		Vector3 target = new Vector3(end.X, end.Y, end.Z);
		// In Godot, use .DistanceTo()
		while (current.DistanceTo(target) > 1.0f)
		{
			// Calculate direction towards the target
			Vector3 direction = (target - current).Normalized();
			
			// Add noise for an organic "wobble"
			float nX = context.Noise.GetNoise3D(current.X * 0.1f, current.Y * 0.1f, current.Z * 0.1f);
			float nY = context.Noise.GetNoise3D(current.X * 0.1f + 50f, current.Y * 0.1f, current.Z * 0.1f);
			float nZ = context.Noise.GetNoise3D(current.X * 0.1f + 100f, current.Y * 0.1f, current.Z * 0.1f);
			
			Vector3 wander = new Vector3(nX, nY, nZ) * 0.8f;
			
			// Move and apply noise steering
			current += (direction + wander) * 0.5f;
			
			// Carve using current position
			VoxelUtils.CarveRadius((int)current.X, (int)current.Y, (int)current.Z, 3, map, width, height, depth);
	}
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
