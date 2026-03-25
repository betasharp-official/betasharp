using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.Fakes;

public class FakeChunkSource : IChunkSource
{
    private readonly IWorldContext _world;
    
    public FakeChunkSource(IWorldContext world) => _world = world;

    public bool IsChunkLoaded(int x, int z) => true;

    public Chunk GetChunk(int x, int z) => new Chunk(_world, x, z);

    public Chunk LoadChunk(int x, int z) => GetChunk(x, z);

    public void DecorateTerrain(IChunkSource source, int x, int z)
    {
    }

    public bool Save(bool saveEntities, LoadingDisplay display) => true;

    public bool Tick() => false;

    public bool CanSave() => false;

    public string GetDebugInfo() => "FakeChunkSource";
}
