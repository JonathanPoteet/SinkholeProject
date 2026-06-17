using Godot;
using System.Collections.Generic;
using System;
using SinkholeProject.Helper.Initialization;

namespace SinkholeProject.World
{
	public class WorldContext
{
	public int Seed { get; }
	public FastNoiseLite Noise { get; }
	public Dictionary<int, Chunk> WorldChunks { get; }
	public System.Random Rnd { get; }

	public WorldContext(int seed)
	{
		Seed = seed;

		Noise = new FastNoiseLite();
		Noise.Seed = seed;
		Rnd = new Random(seed);
		Noise.Frequency = 0.02f;
		Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;

		WorldChunks = new Dictionary<int, Chunk>();
	}
}
}
