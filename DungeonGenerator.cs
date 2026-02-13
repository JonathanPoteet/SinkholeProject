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
	private MeshInstance3D _meshInstance;
	private FastNoiseLite _noise;
	private int _chunkCount;
	
	private Dictionary<int, int[,,]> _worldChunks = new Dictionary<int, int[,,]>();

	// We also need to keep track of the exits for EACH chunk to know where to start the next
	private Dictionary<int, List<Vector3I>> _chunkExits = new Dictionary<int, List<Vector3I>>();
	
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
		GenerateDungeon();
	}

	public void GenerateDungeon()
	{
	_chunkCount = 0;
	// Ensure depth is significant enough for a descent
	_currentDepth = (int)GD.RandRange(40, 60); 
	
	// 2. Carve a guaranteed path from Top to Bottom
	List<Vector3I> firstEntrance = new List<Vector3I> { new Vector3I(Width / 2, _currentDepth - 1, Height / 2) };
	GenerateChunk(firstEntrance);
	GenerateChunk(_chunkExits[1]);
	GenerateChunk(_chunkExits[2]);
	GenerateChunk(_chunkExits[3]);
	GenerateChunk(_chunkExits[4]);
	}
	
	
	public void GenerateChunk(List<Vector3I> entrances){
	// 1. Initialize the array (Object creation)
	// Ensure Width, _currentDepth, and Height are already set
	 int[,,] map = new int[Width, _currentDepth, Height];

	// 2. Start with a solid block (1 = Rock)
	InitializeSolid(map);
	
	// 3. Prepare Entrances 
	// If the entrances came from the BOTTOM (Y=0) of the previous chunk,
	// we must move them to the TOP (Y = _currentDepth - 1) of this new chunk.
	List<Vector3I> adjustedEntrances = new List<Vector3I>();
	foreach (var ent in entrances)
	{
		adjustedEntrances.Add(new Vector3I(ent.X, _currentDepth - 1, ent.Z));
	}
	
	Random rnd = new Random();
	int exitCount = rnd.Next(1, 6);
	GD.Print($"New Chunk: {adjustedEntrances.Count} Entrances, {exitCount} Exits");
	
	List<Vector3I> exitPoints = new List<Vector3I>();

	// 1. Determine the new Exit Points at the bottom (Y = 0)
	for (int i = 0; i < exitCount; i++)
	{
		exitPoints.Add(new Vector3I(
			rnd.Next(10, Width - 10),
			0, 
			rnd.Next(10, Height - 10)
		));
	}

	// 4. Carve the paths to the new random exits
	CarveVerticalPath(adjustedEntrances, exitPoints,  map);
	
	// 5. Build the visual representation
	GenerateMesh(map);
	_chunkCount++;
	
	//store map/store exits
	_worldChunks.Add(_chunkCount, map);
	
	_chunkExits.Add(_chunkCount, exitPoints);
	
	if(_chunkCount == 1) PositionPlayer(map);
	GD.Print("Chunk generation complete.");
	
}

private void InitializeSolid(int[,,] map) {
	for (int x = 0; x < Width; x++)
	{
		for (int y = 0; y < _currentDepth; y++)
		{
			for (int z = 0; z < Height; z++)
			{
				map[x, y, z] = 1; 
			}
		}
	}
	GD.Print("Solid block initialized successfully.");
}

	/* ===================== NEW: THE DRILLER ===================== */

private void CarveVerticalPath(List<Vector3I> startPositions, List<Vector3I> exitPoints, int[,,] map)
{
	// 2. Map every entrance to every exit (or a random selection)
	foreach (Vector3I startPos in startPositions)
	{
		foreach (Vector3I exitGoal in exitPoints)
		{
			// This ensures every entrance has a path to every exit
			// creating a highly interconnected "Rat's Nest" style dungeon
			CarvePathBetweenPoints(startPos, exitGoal, map);
		}
	}
}

private void CarvePathBetweenPoints(Vector3I start, Vector3I end, int[,,] map)
{
	Vector3 curr = new Vector3(start.X, start.Y, start.Z);
	Random rnd = new Random();

	// Progress is now moving TOWARD 0
	while (curr.Y > end.Y)
	{
		// Subtract from Y to move down
		float nextY = Math.Max(curr.Y - rnd.Next(4, 8), end.Y);
		
		float nextX = Mathf.Lerp(curr.X, end.X, 0.3f) + rnd.Next(-5, 6);
		float nextZ = Mathf.Lerp(curr.Z, end.Z, 0.3f) + rnd.Next(-5, 6);
		
		Vector3 target = new Vector3(nextX, nextY, nextZ);

		float dist = curr.DistanceTo(target);
		int steps = (int)(dist * 2);
		for (int i = 0; i <= steps; i++)
		{
			Vector3 lerpPos = curr.Lerp(target, (float)i / steps);
			CarveSphere(lerpPos, rnd.Next(2, 4), map);
		}

		curr = target;
	}
}

private void CarveSphere(Vector3 pos, int radius, int[,,] map)
{
	for (int x = -radius; x <= radius; x++)
	{
		for (int y = -1; y <= 2; y++) // Height for player
		{
			for (int z = -radius; z <= radius; z++)
			{
				if (x * x + z * z <= radius * radius)
				{
					int nx = (int)pos.X + x;
					int ny = (int)pos.Y + y;
					int nz = (int)pos.Z + z;

					if (nx > 0 && nx < Width - 1 && ny >= 0 && ny < _currentDepth && nz > 0 && nz < Height - 1)
					{
						map[nx, ny, nz] = 0; // 0 is Air
					}
				}
			}
		}
	}
}

	/* ===================== MAP GENERATION ===================== */

	private void ApplyErosionNoise(int[,,] map)
{
	for (int x = 1; x < Width - 1; x++)
	{
		for (int y = 1; y < _currentDepth - 1; y++)
		{
			for (int z = 1; z < Height - 1; z++)
			{
				// If it's already air (from the driller), we might expand it
				// If it's rock, we might turn it to air if noise is high
				float noiseVal = _noise.GetNoise3D(x, y, z);
				
				// We only turn rock into air if the noise is very strong.
				// This creates pockets that connect to our main path.
				if (map[x, y, z] == 0 && noiseVal > FillThreshold + 0.1f)
				{
					map[x, y, z] = 0; //air
				}
			}
		}
	}
}

	/* ===================== MESH GENERATION ===================== */

	private void GenerateMesh(int[,,] map)
	{
		MeshInstance3D newChunkMesh = new MeshInstance3D();
		AddChild(newChunkMesh);
		SurfaceTool st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		for (int y = 0; y < _currentDepth - 1; y++)
		{
			for (int x = 0; x < Width - 1; x++)
			{
				for (int z = 0; z < Height - 1; z++)
				{
					// MarchCube logic goes here
					MarchCube(st, x, y, z, map);
				}
			}
		}
		st.GenerateNormals(); // This calculates the lighting for smooth shading
		st.Index();           // This merges duplicate vertices to save memory
		ArrayMesh mesh = st.Commit();
		StandardMaterial3D prototypeMaterial = new StandardMaterial3D();

		// 1. Set the color
		prototypeMaterial.AlbedoColor = new Color(0.4f, 0.4f, 0.45f);

		// 2. Enable Triplanar mapping (Note the casing: Uv1Triplanar)
		prototypeMaterial.Uv1Triplanar = true; 

		// 3. Set the Triplanar sharpness (makes the blend between sides look better)
		prototypeMaterial.Uv1TriplanarSharpness = 0.5f;

		// 4. Apply to your mesh
		newChunkMesh.Mesh = mesh;
		newChunkMesh.MaterialOverride = prototypeMaterial; // Use your material
		float verticalOffset = (_chunkCount) * (_currentDepth - 1);
		newChunkMesh.GlobalPosition = new Vector3(0, -verticalOffset, 0);
		
		foreach (var child in newChunkMesh.GetChildren()) {
		child.Free(); // Use Free() immediately for safety here
		}
		newChunkMesh.CreateTrimeshCollision();
		GD.Print($"Chunk {_chunkCount} generated at Global Y: {newChunkMesh.GlobalPosition.Y}");
		
	}
	
private void MarchCube(SurfaceTool st, int x, int y, int z, int[,,] map)
{
	// 1. Get noise values for the 8 corners
	float[] cubeValues = new float[8];
	for (int i = 0; i < 8; i++) {
		cubeValues[i] = SampleFloat(x + cornerOffsets[i].X, 
									y + cornerOffsets[i].Y, 
									z + cornerOffsets[i].Z, map);
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

	private float SampleFloat(int x, int y, int z, int[,,] map)
	{
		// Bounds Check: If we are outside the map, treat it as empty air (0)
		// This prevents "IndexOutOfRangeException"
	// Force the outer 1-voxel border to be "Air" (-1.0)
	// This forces the mesh to close and prevents it from looking like a solid block
	if (x <= 0 || x >= Width - 1 || y <= 0 || y >= _currentDepth - 1 || z <= 0 || z >= Height - 1)
	{
		return -1.0f; 
	}

	// Assuming 1 = Rock (Solid) and 0 = Air (Empty) based on your previous logic
	// Marching Cubes usually considers values ABOVE a threshold as "Solid"
	return (map[x, y, z] == 1) ? 1.0f : -1.0f;
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
	
	
	private void PositionPlayer(int[,,] map)
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
			// Change your loop to start from the top of the map
			// Loop ends at Depth - 2 so that y + 1 is always safe (max Depth - 1)
		for (int y = _currentDepth - 1; y >= 0; y--) 
		{
			for (int x = 5; x < Width - 5; x++)
			{
				for (int z = 5; z < Height - 5; z++)
				{
					
					if ((map[x, y, z] == 0 && map[x, y - 1, z] == 1) || (y == (_currentDepth-1) && map[x, y, z] == 1))
					{
						// We place the player at Y because Y is the AIR voxel.
						// We add a small 1.1f offset to keep their feet from clipping into the floor.
						Vector3 spawnPos = GlobalTransform.Origin + new Vector3(x + 0.5f, y + 1.1f, z + 0.5f);
						
						player.GlobalPosition = spawnPos;
						GD.Print($"Player spawned at: X:{x}, Y:{y}, Z:{z}");
						return;
					}
				}
			}
		}
	
		GD.PrintErr("DungeonGenerator: Could not find a valid spawn point!");
	}
	


}
