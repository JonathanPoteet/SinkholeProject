using Godot;
using System.Collections.Generic;
using SinkholeProject.Helper.Initialization;

namespace SinkholeProject.World
{
	public class WorldContext
{
	public int Seed { get; }
	public FastNoiseLite Noise { get; }
	public Dictionary<int, Chunk> WorldChunks { get; }

	public WorldContext(int seed)
	{
		Seed = seed;

		Noise = new FastNoiseLite();
		Noise.Seed = seed;
		Noise.Frequency = 0.02f;
		Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;

		WorldChunks = new Dictionary<int, Chunk>();
	}
}
}
