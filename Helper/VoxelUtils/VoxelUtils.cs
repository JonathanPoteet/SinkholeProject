using Godot;
namespace SinkholeProject.Helper.VoxelUtils;
public static class VoxelUtils
{
	/// <summary>
	/// Safely carves a single voxel if it is within bounds and not already air.
	/// </summary>
	public static void SafeCarve(int x, int y, int z, int[,,] map, int width, int height, int depth, int airValue = 0)
	{
		// 1. Boundary check: ensure we are inside the array dimensions
		if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
		{
			// 2. Conditional Carving: Only carve if it's not already set to air.
			// Adjust the comparison (map[x, y, z] != airValue) based on your specific ID logic.
			map[x, y, z] = airValue;
		}
	}
	
	
	public static void CarveRadius(int cx, int cy, int cz, int radius, int[,,] map, int width, int height, int depth)
	{
		int rSq = radius * radius; // Pre-calculate squared radius for performance
		for (int ix = -radius; ix <= radius; ix++)
		{
			for (int iy = -radius; iy <= radius; iy++)
			{
				for (int iz = -radius; iz <= radius; iz++)
				{
					// Check if this offset point is within the sphere radius
					if (ix * ix + iy * iy + iz * iz <= rSq)
					{
						VoxelUtils.SafeCarve(cx + ix, cy + iy, cz + iz, map, width, height, depth);
					}
				}
			}
		}
	}
}
