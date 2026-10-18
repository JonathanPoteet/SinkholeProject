using Godot;
using System;
namespace SinkholeProject.Player;
public partial class PlayerHelper : Node
{
	
	public void PositionPlayer(int[,,] map, int globalYOffset, int chunkIndex, int chunkHeight)
{
	var player = GetTree().Root.FindChild("Player", true, false) as Node3D;
	if (player == null) return;

	float voxelSize = 1.0f;

	int width = map.GetLength(0);
	int depth = map.GetLength(2);
	
	float centerX = width * 0.5f;
	float centerZ = depth * 0.5f;
	float minSpawnDistanceSquared = 36.0f;

	float bestScore = float.MaxValue;
	Vector3? bestPos = null;

	// IMPORTANT: stay in safe bounds for y+2 check
		GD.Print($"Checking mesh y: {globalYOffset}, chunkHeight: {chunkHeight}");
	for (int y = Math.Min(chunkHeight, map.GetLength(1)) - 4; y >= 0; y--)
	{
		for (int x = 4; x < width - 4; x++)
		{
			for (int z = 4; z < depth - 4; z++)
			{
				bool floor = HasFloorSupport(map, x, y, z);
				bool hasPlayerClearance = HasPlayerClearance(map, x, y, z);
				float shaftDx = x - centerX;
				float shaftDz = z - centerZ;
				bool clearOfExitShaft = shaftDx * shaftDx + shaftDz * shaftDz >= minSpawnDistanceSquared;

				if (floor && hasPlayerClearance && clearOfExitShaft)
				{
					float dx = x - centerX;
					float dz = z - centerZ;
					float score = dx * dx + dz * dz; // squared distance

					if (score < bestScore)
					{
						float chunkWorldY = globalYOffset - chunkHeight;

						float worldX = x * voxelSize;
						float worldY = chunkWorldY + (y + 0.75f) * voxelSize;
						float worldZ = z * voxelSize;

						bestScore = score;
						bestPos = new Vector3(worldX, worldY, worldZ);
					}
				}

			}
		}
	}
	
	if (bestPos != null)
	{
	player.GlobalPosition = bestPos.Value;
	GD.Print($"Spawn centered at {bestPos.Value}");
	return;
	}

	GD.PrintErr("No valid spawn found.");
}

	private static bool HasPlayerClearance(int[,,] map, int x, int y, int z)
	{
		for (int offsetX = -1; offsetX <= 1; offsetX++)
		for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
		for (int offsetY = 1; offsetY <= 3; offsetY++)
		{
			if (map[x + offsetX, y + offsetY, z + offsetZ] != 0) return false;
		}

		return true;
	}

	private static bool HasFloorSupport(int[,,] map, int x, int y, int z)
	{
		for (int offsetX = -1; offsetX <= 1; offsetX++)
		for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
		{
			if (map[x + offsetX, y, z + offsetZ] != 1) return false;
		}

		return true;
	}
}
