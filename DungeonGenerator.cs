using Godot;
using System;
using SinkholeProject.ChunkCreation.DescentGenerator;
using SinkholeProject.ChunkCreation.GenerateMesh;
using SinkholeProject.ChunkCreation.StitchChunk;

using SinkholeProject.Helper.Initialization;
using System.Collections.Generic;
using SinkholeProject.ChunkCreation.InitializeChunk;
using SinkholeProject.World;
using SinkholeProject.Player;

public partial class DungeonGenerator : Node3D
{
	private WorldContext _context;
	
	public override void _Ready()
	{
		_context = new WorldContext(1337);
		GenerateDungeon();
		PlayerHelper playerHelper = new PlayerHelper();
		AddChild(playerHelper);
		playerHelper.PositionPlayer(_context.WorldChunks[0].MapData, _context.WorldChunks[0].GlobalYOffset, 0, _context.WorldChunks[0].height);
		
		
	}

	public void GenerateDungeon()
	{
		InitializeChunk chunkInitializer = new InitializeChunk();
		int chunkCount = 4;
		for (int i = 0; i < chunkCount; i++)
				{
					chunkInitializer.GenerateChunk(i, _context);
				}
				
		DescentGenerator carver = new DescentGenerator();
		for(int i = 0; i < chunkCount; i++) {
			carver.CarveSeededVerticalPath(i, _context);
		}
		StitchChunk chunkStitcher = new StitchChunk();
	  	for (int i = 0; i < chunkCount; i++) {
			chunkStitcher.StitchChunkBorders(i, _context.WorldChunks);
		}
		
		GenerateMesh meshGen = new GenerateMesh();
		AddChild(meshGen);
		for (int i = 0; i < chunkCount; i++)
				{
					meshGen.MeshGenerator(i, _context);
				}
				
	}
}
