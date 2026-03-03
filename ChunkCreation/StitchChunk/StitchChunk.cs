using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.Helper.Initialization;
namespace SinkholeProject.ChunkCreation.StitchChunk;
public partial class StitchChunk : Node
{
	public void StitchChunkBorders(int chunkIndex, Dictionary<int, Chunk> worldChunks)
{
	if (!worldChunks.ContainsKey(chunkIndex + 1)) return;

	Chunk current = worldChunks[chunkIndex];
	Chunk below = worldChunks[chunkIndex + 1];

	// Use the smaller of the two dimensions to stay in bounds
	int safeWidth = Math.Min(current.width, below.width);
	int safeDepth = Math.Min(current.depth, below.depth);

	for (int x = 1; x < safeWidth - 1; x++)
	{
		for (int z = 1; z < safeDepth - 1; z++)
		{
			for (int i = 0; i < 3; i++)
			{
				int currentRow = i;
				int belowRow = below.height - 1 - i;

				int sum = 0;
				sum += current.MapData[x - 1, currentRow, z];
				sum += current.MapData[x + 1, currentRow, z];
				sum += current.MapData[x, currentRow, z - 1];
				sum += current.MapData[x, currentRow, z + 1];
				sum += current.MapData[x, currentRow, z] * 2;
				current.MapData[x, currentRow, z] = sum >= 4 ? 1 : 0;

				sum = 0;
				sum += below.MapData[x - 1, belowRow, z];
				sum += below.MapData[x + 1, belowRow, z];
				sum += below.MapData[x, belowRow, z - 1];
				sum += below.MapData[x, belowRow, z + 1];
				sum += below.MapData[x, belowRow, z] * 2;
				below.MapData[x, belowRow, z] = sum >= 4 ? 1 : 0;
			}
		}
	}
}
}
