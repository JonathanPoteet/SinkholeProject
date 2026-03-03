using Godot;
using System;
using System.Collections.Generic;
using SinkholeProject.Helper.Initialization;
namespace SinkholeProject.ChunkCreation.GenerateMesh;
using SinkholeProject.World;
using SinkholeProject.Helper.MarchingCubes;

public partial class GenerateMesh : Node
{
	private static float FillThreshold = 0.05f;
	private static readonly Vector3I[] cornerOffsets = new Vector3I[]
{
	new Vector3I(0, 0, 0), // 0
	new Vector3I(1, 0, 0), // 1
	new Vector3I(1, 0, 1), // 2
	new Vector3I(0, 0, 1), // 3
	new Vector3I(0, 1, 0), // 4
	new Vector3I(1, 1, 0), // 5
	new Vector3I(1, 1, 1), // 6
	new Vector3I(0, 1, 1)  // 7
};
	
	public void MeshGenerator(int chunkIndex, WorldContext context)
{
	MeshInstance3D newChunkMesh = new MeshInstance3D();
	AddChild(newChunkMesh);
	
	SurfaceTool st = new SurfaceTool();
	st.Begin(Mesh.PrimitiveType.Triangles);
	int yStart = 0;
	int yEnd = context.WorldChunks[chunkIndex].height;
	int[,,] map = context.WorldChunks[chunkIndex].MapData;
	for (int y = yStart; y < yEnd; y++)
		for (int x = 0; x < context.WorldChunks[chunkIndex].width - 1; x++)
			for (int z = 0; z < context.WorldChunks[chunkIndex].depth - 1; z++)
				MarchCube(st, x, y, z, map, chunkIndex, context.WorldChunks);

	st.GenerateNormals();
	st.Index();
	
	ArrayMesh mesh = st.Commit();
	StandardMaterial3D mat = new StandardMaterial3D();
	var rng = new RandomNumberGenerator();
	rng.Seed = (ulong)(context.Seed + chunkIndex);

	mat.AlbedoColor = new Color(
		rng.RandfRange(0.2f, 0.8f),
		rng.RandfRange(0.2f, 0.8f),
		rng.RandfRange(0.2f, 0.8f)
	);

	mat.Uv1Triplanar = true;

	newChunkMesh.Mesh = mesh;
	newChunkMesh.MaterialOverride = mat;
	newChunkMesh.GlobalPosition = new Vector3(0, context.WorldChunks[chunkIndex].GlobalYOffset - context.WorldChunks[chunkIndex].height, 0);
	newChunkMesh.CreateTrimeshCollision();
	GD.Print($"Chunk {chunkIndex}, Generated at {newChunkMesh.GlobalPosition}");
}

	private void MarchCube(SurfaceTool st, int x, int y, int z, int[,,] map, int chunkIndex, Dictionary<int, Chunk> worldChunks)
	{
		float[] cubeValues = new float[8];
		int cubeIndex = 0;

		for (int i = 0; i < 8; i++)
		{
			int nx = x + cornerOffsets[i].X;
			int ny = y + cornerOffsets[i].Y;
			int nz = z + cornerOffsets[i].Z;

		cubeValues[i] = SampleVoxel(nx, ny, nz, map, chunkIndex, worldChunks);

			if (cubeValues[i] > FillThreshold) cubeIndex |= (1 << i);
		}

		if (cubeIndex == 0 || cubeIndex == 255) return;

		for (int i = 0; MarchingCubesData.TriTable[cubeIndex, i] != -1; i += 3)
		{
			st.AddVertex(InterpEdge(MarchingCubesData.TriTable[cubeIndex, i + 2], x, y, z, cubeValues));
			st.AddVertex(InterpEdge(MarchingCubesData.TriTable[cubeIndex, i + 1], x, y, z, cubeValues));
			st.AddVertex(InterpEdge(MarchingCubesData.TriTable[cubeIndex, i], x, y, z, cubeValues));
		}
	}

	private Vector3 InterpEdge(int edgeIndex, int x, int y, int z, float[] cubeValues)
	{
		int v1Idx = MarchingCubesData.EdgeToCorners[edgeIndex][0];
		int v2Idx = MarchingCubesData.EdgeToCorners[edgeIndex][1];
		float valP1 = cubeValues[v1Idx];
		float valP2 = cubeValues[v2Idx];

		float mu = (FillThreshold - valP1) / (valP2 - valP1);
		Vector3 p1 = new Vector3(x, y, z) + (Vector3)cornerOffsets[v1Idx];
		Vector3 p2 = new Vector3(x, y, z) + (Vector3)cornerOffsets[v2Idx];
		return p1 + mu * (p2 - p1);
	}
	
private float SampleVoxel(int x, int y, int z, int[,,] map, int chunkIndex, Dictionary<int, Chunk> worldChunks)
{
	int height = worldChunks[chunkIndex].height;
	int width  = worldChunks[chunkIndex].width;
	int depth  = worldChunks[chunkIndex].depth;

	// X/Z walls: always solid
	if (x <= 0 || x >= width - 1 || z <= 0 || z >= depth - 1)
		return -1.0f;

	// y < 0 means we're sampling into the chunk ABOVE (lower chunkIndex)
	if (y < 0)
	{
		int neighborIndex = chunkIndex + 1;
		if (!worldChunks.ContainsKey(neighborIndex)) return -1.0f;
		int neighborY = worldChunks[neighborIndex].height + y; // y is negative
		if (neighborY < 0) return -1.0f;
		return (worldChunks[neighborIndex].MapData[x, neighborY, z] == 1) ? 1.0f : -1.0f;
	}

	// y >= height means we're sampling into the chunk BELOW (higher chunkIndex)
	if (y >= height)
	{
		int neighborIndex = chunkIndex - 1;
		if (!worldChunks.ContainsKey(neighborIndex)) return -1.0f;
		int neighborY = y - height;
		if (neighborY >= worldChunks[neighborIndex].height) return -1.0f;
		return (worldChunks[neighborIndex].MapData[x, neighborY, z] == 1) ? 1.0f : -1.0f;
	}

	return (map[x, y, z] == 1) ? 1.0f : -1.0f;
}

}
