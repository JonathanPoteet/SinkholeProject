using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.Helper.Initialization;
using SinkholeProject.World;
namespace SinkholeProject.ChunkCreation.InitializeChunk;
public partial class InitializeChunk : Node
{
	public void GenerateChunk(int chunkIndex, WorldContext context)
{
	Chunk newChunk = new Chunk();

	Random rnd = new Random(context.Seed + chunkIndex + 1000);
	newChunk.width  = 100;
	newChunk.height = rnd.Next(45, 65);
	newChunk.depth  = 100;

	int pastOffset = 0;
	int pastHeight = 0;
	if (chunkIndex != 0)
	{
		pastOffset = context.WorldChunks[chunkIndex - 1].GlobalYOffset;
		pastHeight = context.WorldChunks[chunkIndex - 1].height;
	}

	newChunk.GlobalYOffset = (chunkIndex == 0) ? newChunk.height : pastOffset - pastHeight;
	newChunk.Biome = GetBiomeForChunk(chunkIndex, context);

	GD.Print($"Generating Chunk {chunkIndex}, Global Y Offset: {newChunk.GlobalYOffset}, Biome: {newChunk.Biome}");

	int[,,] map = new int[newChunk.width, newChunk.height, newChunk.depth];
	InitializeSolid(map, newChunk.width, newChunk.height, newChunk.depth, chunkIndex, newChunk.GlobalYOffset, context.Noise);

	newChunk.MapData = map;
	context.WorldChunks[chunkIndex] = newChunk;
}

private BiomeType GetBiomeForChunk(int chunkIndex, WorldContext context)
{
	var rnd = new Random(context.Seed + chunkIndex * 777);
	return (BiomeType)rnd.Next(0, 3);
}
	
	private void InitializeSolid(int[,,] map, int width, int height, int depth, int chunkIndex, int globalYOffset, FastNoiseLite noise)
{
	
	for (int y = 0; y < height; y++)
	{
		float globalY = globalYOffset-y;
		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < depth; z++)
			{
				map[x, y, z] = 1;
				// For chunk 0, fade the top 20 layers to air using noise
				//todo this can be replaced with several variations to create different landscapes based on a biome
			if (chunkIndex == 0 && y >= height - 20)
			{
				float fadeT = (y - (height - 20)) / 20.0f; // 0 to 1
				float surfaceNoise = noise.GetNoise2D(x * 0.5f, z * 0.5f);
				float threshold = fadeT + surfaceNoise * 0.4f;
				if (threshold > 0.2f) // was 0.5f, now only cuts the very top
					map[x, y, z] = 0;
			}
			}
		}
	}
}
}
