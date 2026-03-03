# SinkholeProject

**Sinkhole** is an experimental, procedural dungeon crawler built with [Godot 4](https://godotengine.org/) using a mix of C# and GDScript.

Players descend an infinite sinkhole that is generated on‑the‑fly using a chunk‑based streaming system. Dungeon layouts are created dynamically as the player moves, and a minimap updates in real time to reveal newly explored areas.

> ⚠️ _Work in progress_: this repository is intended for exploration of procedural generation, chunk streaming, and dungeon layout algorithms. Many systems are unfinished and may change without notice.

---

## Features

* **Infinite, procedural dungeon** built around a sinkhole concept.
* **Chunk‑based world streaming** keeps performance stable by generating only visible sections.
* **Dynamic dungeon generator** creates floor plans as the player descends.
* **Real‑time minimap** written in GDScript.
* Code written in **C#** for game logic with supporting **GDScript** for Godot scenes.

## Getting Started

### Prerequisites

You will need:

* [Godot 4.x](https://godotengine.org/download) (mono export for C# support)
* [.NET SDK 7.0+](https://dotnet.microsoft.com/download) (for compiling C# scripts)
* Optional: Visual Studio / Visual Studio Code for editing

### Running the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/SinkholeProject.git
   cd SinkholeProject
   ```
2. Open `project.godot` in Godot 4.
3. Let Godot import the C# solution (`SinkholeProject.sln`).
4. Press **Play** to start the game in the editor.

### Building from Command Line

The Godot editor normally handles compilation, but you can also build using the .NET CLI:

```bash
# from the workspace root
dotnet build SinkholeProject.sln
```

## Project Structure

```
SinkholeProject/
├─ ChunkCreation/          # classes that carve and stitch dungeon chunks
├─ DungeonFactory/         # factory for creating dungeon entities
├─ Helper/                 # utility classes and data structures
├─ Player/                 # player controller and helper scripts
└─ World/                  # world context and streaming logic
```

Scenes and Godot‑specific resources live alongside the top‑level C# files (e.g. `dungeon.tscn`, `Minimap.tscn`, `Wall.tscn`).

## Contributing

Contributions are welcome! Feel free to open issues or pull requests. Some areas that could use work:

* Smoother chunk transitions and performance optimizations
* Smarter dungeon layout algorithms
* Additional enemy/loot systems
* Polish on the minimap and player controls

Please follow standard GitHub workflows, and ensure your changes compile before submitting.

## License & Visibility

This repository is currently public primarily as a portfolio piece — the code is visible to demonstrate techniques and help with job applications. It isn't guaranteed to remain open source or receive third‑party contributions.

The included `LICENSE` file is MIT, but feel free to fork or browse the code for learning purposes. If you fork and start using it in a closed‑source project, please retain attribution.

---
