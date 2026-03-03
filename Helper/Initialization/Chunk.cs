using Godot;
using System.Collections.Generic;
namespace SinkholeProject.Helper.Initialization;

public class Chunk
{
	public int[,,] MapData;
	public int height;
	public int width;
	public int depth;
	public int GlobalYOffset;

	// Biome and planning data lives on the chunk directly
	public BiomeType Biome;
	public List<Room> Rooms              = new();
	public List<(int From, int To)> Edges = new();
	public List<int> GuaranteedPath      = new();
	public Vector3I EntryPoint;
	public Vector3I ExitPoint;
	public int EntryRoomIndex;
	public int ExitRoomIndex;
}

public enum BiomeType { Cave, Tunnel, Cavern }

public struct Room
{
	public Vector3I Center;
	public int Width;
	public int Height;
	public int Depth;
}
