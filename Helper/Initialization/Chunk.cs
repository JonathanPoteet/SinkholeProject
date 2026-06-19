using Godot;
using System.Collections.Generic;
using SinkholeProject.Rooms;
using SinkholeProject.Helper.Volumes;
using System.Linq;

namespace SinkholeProject.Helper.Initialization
{
	public class Chunk
	{
		public int[,,] MapData;
		public int height;
		public int width;
		public int depth;
		public int GlobalYOffset;

		public BiomeType Biome;
		public List<Room> Rooms = new();
		public List<Bounds> ReservedVolumes = new List<Bounds>();
		public List<ChunkExit> ExitPoints = new List<ChunkExit>();
	}

	public class ChunkExit
	{
		public Vector3I Position { get; set; }
		public Room ParentRoom { get; set; } 

		public ChunkExit(Vector3I position, Room room = null)
		{
			Position = position;
			ParentRoom = room;
		}
	}

	public enum BiomeType { Cave, Tunnel, Cavern }
}
