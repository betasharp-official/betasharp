using System.Collections.Generic;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Chunks;

public class EmptyChunk : Chunk
{
    public EmptyChunk(World world, int x, int z) : base(world, x, z)
    {
        Empty = true;
    }

    public EmptyChunk(World world, byte[] blocks, int x, int z) : base(world, blocks, x, z)
    {
        Empty = true;
    }

    public override bool ChunkPosEquals(int x, int z) => x == this.X && z == this.Z;

    public override int GetHeight(int x, int z) => 0;

    public override void PopulateLight() { }
    public override void PopulateHeightMapOnly() { }
    public override void PopulateHeightMap() { }
    public override void PopulateBlockLight() { }

    public override int GetBlockId(int x, int y, int z) => 0;

    public override bool SetBlock(int x, int y, int z, int blockId, int meta) => true;
    public override bool SetBlock(int x, int y, int z, int blockId) => true;

    public override int GetBlockMeta(int x, int y, int z) => 0;
    public override void SetBlockMeta(int x, int y, int z, int meta) { }

    public override int GetLight(LightType type, int x, int y, int z) => 0;
    public override void SetLight(LightType type, int x, int y, int z, int level) { }
    public override int GetLight(int x, int y, int z, int skylight) => 0;

    public override void AddEntity(Entity entity) { }
    public override void RemoveEntity(Entity entity) { }
    public override void RemoveEntity(Entity entity, int chunkY) { }

    public override bool IsAboveMaxHeight(int x, int y, int z) => false;

    public override BlockEntity? GetBlockEntity(int x, int y, int z) => null;
    public override void AddBlockEntity(BlockEntity blockEntity) { }
    public override void SetBlockEntity(int x, int y, int z, BlockEntity blockEntity) { }
    public override void RemoveBlockEntityAt(int x, int y, int z) { }

    public override void Load() { }
    public override void Unload() { }
    public override void MarkDirty() { }

    public override void CollectOtherEntities(Entity excludeEntity, Box box, List<Entity> result) { }
    public override void CollectEntitiesOfType<T>(Box box, List<T> result) { }

    public override bool ShouldSave(bool saveAll) => false;

    public override int LoadFromPacket(byte[] data, int startX, int startY, int startZ, int endX, int endY, int endZ, int meta)
    {
        int width = endX - startX;
        int height = endY - startY;
        int depth = endZ - startZ;

        int volume = width * height * depth;
        return volume + (volume / 2 * 3);
    }

    public override JavaRandom GetSlimeRandom(long seed)
    {
        return new JavaRandom(World.getSeed() + X * X * 4987142L + X * 5947611L + Z * Z * 4392871L + Z * 389711L ^ seed);
    }

    public override bool IsEmpty() => true;
}
