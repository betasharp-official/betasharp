# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BetaSharp is an enhanced recreation of Minecraft Beta 1.7.3, written in C# on .NET 10. It is a multi-project solution with a shared core library, game client, dedicated server, and an Avalonia-based launcher.

> **Note**: The client and server expect the Minecraft JAR file to be in their running directory.

## Build & Run Commands

```bash
# Run the launcher (recommended entry point — handles auth and starts client)
cd BetaSharp.Launcher && dotnet run --configuration Release

# Build a specific project
cd BetaSharp.(Client|Server|Launcher) && dotnet build --configuration Release

# Build everything from root
dotnet build --configuration Release

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName=BetaSharp.Tests.UnitTest1.Test1"
```

## Formatting & Analysis

```bash
# Check formatting and analyzers (what CI runs)
dotnet format --verify-no-changes --verbosity minimal
dotnet format analyzers --verify-no-changes --verbosity minimal

# Auto-fix
dotnet format
dotnet format analyzers
```

The `.editorconfig` enforces: 4-space indentation, LF line endings for `.cs` files, UTF-8, no trailing whitespace, `s_` prefix for private static fields, and PascalCase for constants. Security diagnostics are treated as errors.

## Solution Structure

| Project | Type | Purpose |
|---------|------|---------|
| `BetaSharp/` | Library | Shared core: blocks, entities, items, worlds, network, server logic |
| `BetaSharp.Client/` | Executable | Game client with OpenGL rendering, UI, input, audio |
| `BetaSharp.Server/` | Executable | Standalone dedicated server |
| `BetaSharp.Launcher/` | WinExe (Avalonia) | Launcher with Microsoft account auth (MSAL), AOT compiled |
| `BetaSharp.Tests/` | Test (xUnit) | Unit tests |

## Architecture

### Core Library (`BetaSharp/`)

- **`Bootstrap.cs` / `Registries/DefaultRegistries.cs`** — Initialization entry point; registers all blocks, items, and entities via the registry pattern.
- **`Blocks/`, `Items/`, `Entities/`** — Definitions and behavior for all game objects.
- **`Worlds/`** — World system split into: `Core/` (server world), `Chunks/` (chunk management), `Storage/` (NBT persistence), `Generation/` (terrain gen), `Lighting/`, `Mechanics/`, and `ClientData/` (client-side cache).
- **`Server/BetaSharpServer.cs`** — Base server implementation shared by both multiplayer and dedicated server. Contains `ChunkMap`, `PlayerManager`, and `Commands/`.
- **`Network/`** — Packet-based protocol with `NetHandler` base class; packets are separated into S2C and C2S namespaces.
- **`NBT/`** — Named Binary Tag serialization (Minecraft's native format) used for world persistence.
- **`PathFinding/`** — Entity AI pathfinding.

### Client (`BetaSharp.Client/`)

- **`BetaSharp.cs`** — Main game loop and client initialization; has a static `Instance` singleton.
- **`Display.cs`** — Window/display management via Silk.NET (GLFW).
- **`Rendering/`** — OpenGL rendering pipeline:
  - `Core/OpenGL/` — Low-level OpenGL abstractions
  - `Chunks/` — Chunk mesh building and rendering with frustum culling
  - `Blocks/`, `Entities/`, `Items/` — Model renderers
  - `PostProcessing/` — Post-process effects
  - `GameRenderer.cs` / `WorldRenderer.cs` — Top-level rendering orchestrators
- **`Guis/`** — Custom GUI layout system using a Flexbox-based engine (see `CREDITS.md`).
- **`UI/Screens/`** — Individual screens: menus, HUD, pause screen, containers.
- **`DynamicTexture/`** — Procedurally animated textures (fire, water, lava, portal, clock, compass).
- **`Sound/`** — Audio via SFML.Audio.
- **`Resource/Pack/`** — Texture pack loading.

### Key Technologies

- **Silk.NET** — OpenGL bindings and windowing (GLFW)
- **ImGui.NET** — Debug/development overlays
- **SFML.Audio** — Sound
- **SixLabors.ImageSharp** — Image loading
- **Avalonia 11** — Launcher UI
- **Microsoft.Identity.Client (MSAL)** — Microsoft account authentication

## Code Conventions

- Write idiomatic **C#**, not Java-style code. See [Microsoft C# conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- **Do not introduce new IKVM or Java types.** The long-term goal is to eliminate all IKVM/Java dependencies. When refactoring existing code, convert Java types to their C# equivalents (e.g., `java.util.List` → `List<T>`, Java I/O → `System.IO`).
- If converting Java code would require a massive rewrite that blocks progress, leave it temporarily but mark it for future refactoring.
