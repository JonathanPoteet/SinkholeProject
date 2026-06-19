using Godot;
using System.Collections.Generic;
using SinkholeProject.Helper.Volumes;
namespace SinkholeProject.Rooms
{
	public enum RoomType { Unassigned, Minor, Major }

	public class Room
	{
		public Vector3I Center;
		public RoomType Type { get; set; } = RoomType.Unassigned;
		public List<Bounds> Volumes { get; set; } = new List<Bounds>();
		public List<Room> Neighbors { get; set; } = new List<Room>();
		public bool IsTerminal { get; set; } // Flag this if it's the end of the graph
		public bool HasLight { get; set; }
		public int Depth {get; set;}
		
		public Room(Vector3I center, Bounds initialBounds, RoomType type = RoomType.Unassigned)
		{
			Center = center;
			Type = type;
			Volumes.Add(initialBounds);
			IsTerminal = false;
			HasLight = false;
			Depth = -1;
		}
		
		public void AddVolume(Bounds newBounds)
		{
			Volumes.Add(newBounds);
		}
		
		public bool Intersects(Bounds newBounds, int padding = 0)
		{
			foreach (var myVolume in this.Volumes)
			{
				// Compare existing volumes against the candidate bounds
				if (myVolume.Intersects(newBounds, padding))
				{
					return true;
				}
			}
			return false;
		}
		
		public int GetTotalVolume()
		{
			int total = 0;
			foreach (var volume in Volumes)
			{
				// Assuming your Bounds class has Width, Height, and Depth properties
				total += (volume.Width * volume.Height * volume.Depth);
			}
			return total;
		}

	}
}
