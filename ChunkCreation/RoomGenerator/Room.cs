using Godot;
using System.Collections.Generic;
using SinkholeProject.Helper.Volumes;
using System.Linq;
using SinkholeProject.Configuration;

namespace SinkholeProject.Rooms
{
	public enum RoomType { Unassigned, Minor, Major }

	public class Room
	{
		public RoomType Type { get; set; } = RoomType.Unassigned;
		public List<Vector3I> Cells { get; set; } = new List<Vector3I>();
		public List<Room> Neighbors { get; set; } = new List<Room>();
		public bool IsTerminal { get; set; }
		public bool HasLight { get; set; }
		public int Depth { get; set; }
		private List<Bounds> _cachedBounds = new List<Bounds>();
		public List<Vector3I> ConnectionPoints { get; set; } = new List<Vector3I>();
		
		private Vector3I? _cachedCenter;

		// Call this once after your RoomGenerator finishes adding all cells

		public Vector3I Center => _cachedCenter ?? Vector3I.Zero;
		
		// Private field for settings
		private ChunkSettings _settings;

		public void FinalizeCenter()
		{
			long sumX = 0, sumY = 0, sumZ = 0;
			foreach (var cell in Cells)
			{
				// Convert grid index to physical center here
				sumX += (cell.X * _settings.CellW) + (_settings.CellW / 2);
				sumY += (cell.Y * _settings.CellH) + (_settings.CellH / 2);
				sumZ += (cell.Z * _settings.CellD) + (_settings.CellD / 2);
			}
			
			_cachedCenter = new Vector3I((int)(sumX / Cells.Count), (int)(sumY / Cells.Count), (int)(sumZ / Cells.Count));
		}
		
		public Room(ChunkSettings settings)
		{
			IsTerminal = false;
			HasLight = false;
			Depth = -1;
			// Use 'this' to distinguish the field from the parameter
			_settings = settings;
		}

		public void AddCell(Vector3I gridPos)
		{
			Cells.Add(gridPos);
			_cachedBounds.Add(GetBoundsForCell(gridPos));
		}

		public bool Intersects(Bounds newBounds)
		{
			// Now you just iterate over the pre-calculated list
			foreach (Bounds b in _cachedBounds)
			{
				if (b.Intersects(newBounds)) return true;
			}
			return false;
		}

		public Bounds GetBoundsForCell(Vector3I cellPos)
		{
			// 1. Calculate the center position of the cell
			// Depending on your grid logic, this usually involves adding half the dimensions
			int centerX = _settings.Padding + (cellPos.X * _settings.CellW) + (_settings.CellW / 2);
			int centerY = _settings.Padding + (cellPos.Y * _settings.CellH) + (_settings.CellH / 2);
			int centerZ = _settings.Padding + (cellPos.Z * _settings.CellD) + (_settings.CellD / 2);

			Vector3I center = new Vector3I(centerX, centerY, centerZ);

			// 2. Pass the vector and the dimensions to your existing constructor
			return new Bounds(center, _settings.CellW, _settings.CellH, _settings.CellD);
		}
	}
}
