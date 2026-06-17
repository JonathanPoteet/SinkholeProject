using Godot;

namespace SinkholeProject.Helper.Volumes
{
	/// <summary>
	/// Represents an Axis-Aligned Bounding Box (AABB) for collision detection
	/// and spatial reservation in the dungeon generation system.
	/// </summary>
	public struct Bounds
	{
		public Vector3I Min { get; }
		public Vector3I Max { get; }

		public Bounds(Vector3I center, int w, int h, int d)
	{
		Vector3I half = new Vector3I(w / 2, h / 2, d / 2);
		Min = center - half;
		Max = Min + new Vector3I(w, h, d); // Exclusive max: [Min, Max)
	}
	public Bounds(Vector3I min, Vector3I max)
	{
		Min = min;
		Max = max;
	}

	public bool Intersects(Bounds other, int padding = 5) //keeping a high padding by default for room generation to have the rooms not be impossible to separate
	{
		// Standard AABB intersection test
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
}
