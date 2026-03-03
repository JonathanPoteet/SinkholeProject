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

	float bestScore = float.MaxValue;
	Vector3? bestPos = null;

	// IMPORTANT: stay in safe bounds for y+2 check
		GD.Print($"Checking mesh y: {globalYOffset}, chunkHeight: {chunkHeight}");
	for (int y = chunkHeight - 3; y >= 0; y--)
	{
		for (int x = 2; x < width - 2; x++)
		{
			for (int z = 2; z < depth - 2; z++)
			{
				bool floor = map[x, y, z] == 1;
				bool headroom1 = map[x, y + 1, z] == 0;
				bool headroom2 = map[x, y + 2, z] == 0;

				if (floor && headroom1 && headroom2)
				{
					float dx = x - centerX;
					float dz = z - centerZ;
					float score = dx * dx + dz * dz; // squared distance

					if (score < bestScore)
					{
						float chunkWorldY = globalYOffset - chunkHeight;

						float worldX = x * voxelSize;
						float worldY = chunkWorldY + (y + 1.5f) * voxelSize;
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
}
