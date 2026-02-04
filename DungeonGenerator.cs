using Godot;
using System;
using SinkholeProject.Helper.MarchingCubes;
using System.Collections.Generic;

public partial class DungeonGenerator : Node3D
{
	[ExportGroup("Map Settings")]
	[Export] public int Width = 100;
	[Export] public int Height = 100;
	[Export] public float FillThreshold = 0.05f;
	[Export] public float NoiseFrequency = 0.05f;
	[Export] public int SmoothingPasses = 5;
	private int _currentDepth; // Store the random result here
	private int[,,] _map;
	private MeshInstance3D _meshInstance;
	private FastNoiseLite _noise;
	
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

	public override void _Ready()
	{
		_meshInstance = new MeshInstance3D{Name = "CaveMesh"};
		AddChild(_meshInstance);
		
		StandardMaterial3D mat = new StandardMaterial3D();
		mat.VertexColorUseAsAlbedo = true; 
		mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // See both sides
		_meshInstance.MaterialOverride = mat;
		
		GenerateDungeon();
	}

	public void GenerateDungeon()
	{
		_currentDepth = (int)GD.RandRange(0, 50);
		_map = new int[Width, _currentDepth, Height];

		ApplyNoise();

		for (int i = 0; i < SmoothingPasses; i++)
			SmoothMap();
		cullFloaters();
		
		PrintMap();
		GenerateMesh();
		PositionPlayer();
		
	}

	/* ===================== MAP GENERATION ===================== */

	private void ApplyNoise()
	{
		_noise = new FastNoiseLite
		{
			Seed = (int)GD.Randi(),
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = NoiseFrequency
		};

	// 3. Triple loop to fill the 3D volume
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < _currentDepth; y++)
			{
				for (int z = 0; z < Height; z++)
				{
					// Boundary check: Seal the side walls AND the floor/ceiling
					if (x == 0 || x == Width - 1 || 
						z == 0 || z == Height - 1 || 
						y == 0 || y == _currentDepth - 1)
					{
						_map[x, y, z] = 0; // Solid boundary
					}
					else
					{
						// Sample 3D noise for vertical connectivity
						float normalizedY = (float)y / (_currentDepth - 1);
						float noiseVal = _noise.GetNoise3D(x, y, z);
						float bias = (normalizedY * 0.5f) - 0.25f; // Adjust these numbers to taste
						float finalValue = noiseVal - bias;

						_map[x, y, z] = finalValue > FillThreshold ? 1 : 0;
					}
				}
			}
		}
	}


	private int CountWallNeighbors(int x, int y, int z, int[,,] map)
	{
		int count = 0;
		// Loop through a 3x3x3 area around the voxel
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					// Skip the center voxel itself
					if (i == 0 && j == 0 && k == 0) continue;

					// Check the 3D map
					if (map[x + i, y + j, z + k] == 0) 
						count++;
				}
			}
		}
		return count;
	}
	
private List<Vector3I> GetRegionVoxels(int startX, int startY, int startZ, bool[,,] checkedVoxels, int targetValue)
{
	List<Vector3I> voxels = new List<Vector3I>();
	Queue<Vector3I> queue = new Queue<Vector3I>();
	
	queue.Enqueue(new Vector3I(startX, startY, startZ));
	checkedVoxels[startX, startY, startZ] = true;

	while (queue.Count > 0)
	{
		Vector3I curr = queue.Dequeue();
		voxels.Add(curr);

		// 26-Neighbor Check: Loop from -1 to 1 on all axes
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				for (int z = -1; z <= 1; z++)
				{
					if (x == 0 && y == 0 && z == 0) continue; // Skip center

					int nx = curr.X + x;
					int ny = curr.Y + y;
					int nz = curr.Z + z;

					if (nx >= 0 && nx < Width && ny >= 0 && ny < _currentDepth && nz >= 0 && nz < Height)
					{
						if (!checkedVoxels[nx, ny, nz] && _map[nx, ny, nz] == targetValue)
						{
							checkedVoxels[nx, ny, nz] = true;
							queue.Enqueue(new Vector3I(nx, ny, nz));
						}
					}
				}
			}
		}
	}
	return voxels;
}

private void SmoothMap()
{
	// 1. Standard Smoothing Pass (Run 2-3 times if needed)
	for (int i = 0; i < 2; i++) 
	{
		int[,,] old = (int[,,])_map.Clone();
		for (int x = 1; x < Width - 1; x++)
		{
			for (int y = 1; y < _currentDepth - 1; y++)
			{
				for (int z = 1; z < Height - 1; z++)
				{
					int walls = CountWallNeighbors(x, y, z, old);
					if (walls > 14) _map[x, y, z] = 0;
					else if (walls < 12) _map[x, y, z] = 1;
				}
			}
		}
	}

	// 2. Flood Fill to find Floaters
}
	private void cullFloaters() {
		bool[,,] checkedAir = new bool[Width, _currentDepth, Height];
	List<List<Vector3I>> airRegions = new List<List<Vector3I>>();

	for (int x = 0; x < Width; x++) {
		for (int y = 0; y < _currentDepth; y++) {
			for (int z = 0; z < Height; z++) {
				if (!checkedAir[x, y, z] && _map[x, y, z] == 1) {
					airRegions.Add(GetRegionVoxels(x, y, z, checkedAir, 1));
				}
			}
		}
	}
	GD.Print($"Flood Fill found {airRegions.Count} distinct air regions.");
	for (int i = 0; i < Math.Min(airRegions.Count, 10); i++)
	{
		GD.Print($"Region {i}: {airRegions[i].Count} voxels");
	}
	// 3. Keep only the largest air region, fill others with stone
	if (airRegions.Count > 1)
	{
		// Sort by size descending
		airRegions.Sort((a, b) => b.Count.CompareTo(a.Count));

		// Start from index 1 (the smaller regions) and turn them back to solid (0)
		for (int i = 1; i < airRegions.Count; i++)
		{
			foreach (Vector3I floaterVoxel in airRegions[i])
			{
				_map[floaterVoxel.X, floaterVoxel.Y, floaterVoxel.Z] = 0;
			}
		}
	}
	
	// PASS 2: ROCK CULL (Remove floating islands)
	bool[,,] checkedRock = new bool[Width, _currentDepth, Height];
	List<List<Vector3I>> rockRegions = new List<List<Vector3I>>();

	for (int x = 0; x < Width; x++) {
		for (int y = 0; y < _currentDepth; y++) {
			for (int z = 0; z < Height; z++) {
				if (!checkedRock[x, y, z] && _map[x, y, z] == 0) {
					rockRegions.Add(GetRegionVoxels(x, y, z, checkedRock, 0));
				}
			}
		}
	}

	foreach (var region in rockRegions) {
		if (!IsRegionGrounded(region)) {
			foreach (var pos in region) {
				_map[pos.X, pos.Y, pos.Z] = 1; // Delete floating rocks
			}
		}
	}
}
	
private bool IsRegionGrounded(List<Vector3I> region)
{
	foreach (Vector3I v in region)
	{
		// If any voxel in this rock cluster touches the outer shell, it's grounded
		if (v.X == 0 || v.X == Width - 1 || 
			v.Y == 0 || v.Y == _currentDepth - 1 || 
			v.Z == 0 || v.Z == Height - 1)
		{
			return true;
		}
	}
	return false;
}
	/* ===================== MESH GENERATION ===================== */

	private void GenerateMesh()
	{
		SurfaceTool st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		for (int y = 0; y < _currentDepth - 1; y++)
		{
			for (int x = 0; x < Width - 1; x++)
			{
				for (int z = 0; z < Height - 1; z++)
				{
					// MarchCube logic goes here
					MarchCube(st, x, y, z);
				}
			}
		}
		st.GenerateNormals(); // This calculates the lighting for smooth shading
		st.Index();           // This merges duplicate vertices to save memory
		ArrayMesh mesh = st.Commit();
		_meshInstance.Mesh = mesh;
		
		foreach (var child in _meshInstance.GetChildren()) {
		child.Free(); // Use Free() immediately for safety here
	}
		_meshInstance.CreateTrimeshCollision();
	}
	
private void MarchCube(SurfaceTool st, int x, int y, int z)
{
	// 1. Get noise values for the 8 corners
	float[] cubeValues = new float[8];
	for (int i = 0; i < 8; i++) {
		cubeValues[i] = SampleFloat(x + cornerOffsets[i].X, 
									y + cornerOffsets[i].Y, 
									z + cornerOffsets[i].Z);
	}

	// 2. Build the 8-bit index (0-255)
	int cubeIndex = 0;
	if (cubeValues[0] > FillThreshold) cubeIndex |= 1;
	if (cubeValues[1] > FillThreshold) cubeIndex |= 2;
	if (cubeValues[2] > FillThreshold) cubeIndex |= 4;
	if (cubeValues[3] > FillThreshold) cubeIndex |= 8;
	if (cubeValues[4] > FillThreshold) cubeIndex |= 16;
	if (cubeValues[5] > FillThreshold) cubeIndex |= 32;
	if (cubeValues[6] > FillThreshold) cubeIndex |= 64;
	if (cubeValues[7] > FillThreshold) cubeIndex |= 128;

	// 3. Skip if the cube is entirely inside or entirely outside
	// (This replaces the need for the 256-edgeTable bitmask check)
	if (cubeIndex == 0 || cubeIndex == 255) return;

	// 4. Build triangles using the Triangle Table
	for (int i = 0; MarchingCubesData.TriTable[cubeIndex, i] != -1; i += 3)
	{
		int e1 = MarchingCubesData.TriTable[cubeIndex, i];
		int e2 = MarchingCubesData.TriTable[cubeIndex, i + 1];
		int e3 = MarchingCubesData.TriTable[cubeIndex, i + 2];

		// InterpEdge calculates the smooth vertex position on the edge
		st.AddVertex(InterpEdge(e3, x, y, z, cubeValues));
		st.AddVertex(InterpEdge(e2, x, y, z, cubeValues));
		st.AddVertex(InterpEdge(e1, x, y, z, cubeValues));
	}
}

	private float SampleFloat(int x, int y, int z)
	{
		// Bounds Check: If we are outside the map, treat it as empty air (0)
		// This prevents "IndexOutOfRangeException"
	// Force the outer 1-voxel border to be "Air" (-1.0)
	// This forces the mesh to close and prevents it from looking like a solid block
	if (x <= 0 || x >= Width - 1 || y <= 0 || y >= _currentDepth - 1 || z <= 0 || z >= Height - 1)
	{
		return -1.0f; 
	}

	//  Get your raw noise (-1.0 to 1.0 range usually)
	float noiseValue = _noise.GetNoise3D(x, y, z);
	return noiseValue;
	}
	
	
	
	


private Vector3 InterpEdge(int edgeIndex, int x, int y, int z, float[] cubeValues)
{
	int v1Idx = MarchingCubesData.EdgeToCorners[edgeIndex][0];
	int v2Idx = MarchingCubesData.EdgeToCorners[edgeIndex][1];

	float valP1 = cubeValues[v1Idx];
	float valP2 = cubeValues[v2Idx];

	// SAFETY CHECK: If the values are too close, just return the midpoint
	// This prevents the "Big Box" caused by Infinity coordinates
	if (Mathf.Abs(valP1 - valP2) < 0.00001f)
	{
		Vector3 p1Mid = new Vector3(x, y, z) + (Vector3)cornerOffsets[v1Idx];
		Vector3 p2Mid = new Vector3(x, y, z) + (Vector3)cornerOffsets[v2Idx];
		return (p1Mid + p2Mid) * 0.5f;
	}

	float mu = (FillThreshold - valP1) / (valP2 - valP1);
	
	Vector3 p1 = new Vector3(x, y, z) + (Vector3)cornerOffsets[v1Idx];
	Vector3 p2 = new Vector3(x, y, z) + (Vector3)cornerOffsets[v2Idx];

	return p1 + mu * (p2 - p1);
}
	
	
	private void PositionPlayer()
{
	// 1. Find the player (Search the whole scene tree if not exported)
	var player = GetTree().Root.FindChild("Player", true, false) as Node3D;

	if (player == null)
	{
		GD.PrintErr("DungeonGenerator: Could not find Player node in the scene!");
		return;
	}

	// 2. Search for a valid spawn point [x, y, z]
	// We start searching from the top layers down to find the "surface" or highest floor
	for (int y = _currentDepth - 2; y >= 1; y--) 
	{
		for (int x = 5; x < Width - 5; x++)
		{
			for (int z = 5; z < Height - 5; z++)
			{
				// Check if current voxel is floor (1) AND the one above is empty (0)
				if (_map[x, y, z] == 1 && _map[x, y + 1, z] == 0)
				{
					// Calculate Global Position
					// x and z are horizontal, y is vertical
					Vector3 spawnPos = GlobalTransform.Origin + new Vector3(x + 0.5f, y + 1.5f, z + 0.5f);
					
					player.GlobalPosition = spawnPos;
					
					GD.Print($"Player positioned at 3D coordinate: X:{x}, Y:{y}, Z:{z}");
					return;
				}
			}
		}
	}
	
	GD.PrintErr("DungeonGenerator: Could not find a valid spawn point!");
}
	
private void PrintMap()
{
	int y = 21;
	GD.Print($"===== VIEWING FLOOR AT HEIGHT Y: {y} =====");
	for (int z = 0; z < Height; z++) // Use Z as the depth of the row
	{
		string row = "";
		for (int x = 0; x < Width; x++)
		{
			// 0 is usually WALL, 1 is usually FLOOR (Empty space)
			row += _map[x, y, z] == 0 ? "██" : "  "; 
		}
		GD.Print(row);
	}
}

}
