using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds.Gen.Chunks;

internal class FlatChunkGenerator(World world) : ChunkSource
{
    private readonly World _world = world;

    public ChunkSource CreateParallelInstance() => new FlatChunkGenerator(_world);

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        byte[] blocks = new byte[32768];

        for (int x = 0; x < 16; ++x)
        {
            for (int z = 0; z < 16; ++z)
            {
                int index = x << 11 | z << 7;
                blocks[index] = (byte)Block.Bedrock.id;
                blocks[index + 1] = (byte)Block.Dirt.id;
                blocks[index + 2] = (byte)Block.Dirt.id;
                blocks[index + 3] = (byte)Block.GrassBlock.id;
            }
        }

        Chunk chunk = new(_world, blocks, chunkX, chunkZ);
        chunk.PopulateHeightMap();
        return chunk;
    }

    public bool Save(bool bl, LoadingDisplay loadingDisplay) => true;

    public bool CanSave() => true;

    public bool IsChunkLoaded(int x, int z) => true;
    public Chunk LoadChunk(int x, int z) => GetChunk(x, z);
    public void DecorateTerrain(ChunkSource chunkSource, int chunkX, int chunkZ) { }
    public bool Tick() => false;
    public string GetDebugInfo() => "FlatLevelSource";
}
