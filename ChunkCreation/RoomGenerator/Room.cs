using Godot;
using System.Collections.Generic;
using SinkholeProject.Helper.Volumes;
namespace SinkholeProject.Rooms
{
	public enum RoomType { Minor, Major }

	public class Room
	{
		public Vector3I Center;
		public RoomType Type { get; set; }
		public Bounds Bounds { get; set; }
		public List<Room> Neighbors { get; set; } = new List<Room>();

		public Room(Vector3I center, Bounds bounds, RoomType type)
		{
			Center = center;
			Type = type;
			Bounds = bounds;
		}
	}
}
