using Godot;
using System.Collections.Generic;

namespace SinkholeProject.Rooms
{
	public enum RoomType { Minor, Major }

	public struct Bounds
	{
		public Vector3I Min;
		public Vector3I Max;

		public Bounds(Vector3I center, int w, int h, int d)
		{
			Vector3I half = new Vector3I(w / 2, h / 2, d / 2);
			Min = center - half;
			Max = center + half;
		}

		public bool Intersects(Bounds other, int padding = 0)
		{
			return (Min.X - padding < other.Max.X + padding && Max.X + padding > other.Min.X - padding) &&
				   (Min.Y - padding < other.Max.Y + padding && Max.Y + padding > other.Min.Y - padding) &&
				   (Min.Z - padding < other.Max.Z + padding && Max.Z + padding > other.Min.Z - padding);
		}

		public bool Contains(Vector3I point)
		{
			return point.X >= Min.X && point.X < Max.X &&
				   point.Y >= Min.Y && point.Y < Max.Y &&
				   point.Z >= Min.Z && point.Z < Max.Z;
		}
	}

	public class Room
	{
		public Vector3I Center;
		public RoomType Type { get; set; }
		public Bounds Bounds { get; set; }
		public List<Room> Neighbors { get; set; } = new List<Room>();

		public Room(Vector3I center, int w, int h, int d, RoomType type)
		{
			Center = center;
			Type = type;
			Bounds = new Bounds(center, w, h, d);
		}
	}
}
