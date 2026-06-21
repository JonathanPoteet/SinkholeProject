namespace SinkholeProject.Configuration
{
	public class ChunkSettings
	{
		public int GridDiv { get; }
		public int CellW { get; }
		public int CellH { get; }
		public int CellD { get; }
		public int Padding { get; } //this is not for use cell by cell

		public ChunkSettings(int playableWidth, int playableHeight, int playableDepth, int gridDiv, int padding)
		{
			GridDiv = gridDiv;
			Padding = padding;
			
			// Calculate dimensions based on the grid divisions
			CellW = playableWidth / gridDiv;
			CellH = playableHeight / gridDiv;
			CellD = playableDepth / gridDiv;
		}
	}
}
