using Godot;
using System.Collections.Generic;
namespace SinkholeProject.Helper.Initialization;
using SinkholeProject.Rooms;
using SinkholeProject.Helper.Volumes;
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
	public List<Bounds> ReservedVolumes = new List<Bounds>();
	public List<Vector3I> ExitPoints = new List<Vector3I>();
}

public enum BiomeType { Cave, Tunnel, Cavern }
