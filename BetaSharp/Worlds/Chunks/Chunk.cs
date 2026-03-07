using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Profiling;
using BetaSharp.Util.Maths;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Chunks;

public class Chunk
{
    public static bool HasSkyLight;

    public byte[] Blocks;
    public ChunkNibbleArray Meta;
    public ChunkNibbleArray SkyLight;
    public ChunkNibbleArray BlockLight;
    public byte[] HeightMap;

    public bool Loaded;
    public World World;
    public int MinHeightMapValue;
    public readonly int X;
    public readonly int Z;
    public Dictionary<BlockPos, BlockEntity> BlockEntities;
    public List<Entity>[] Entities;
    public bool TerrainPopulated;
    public bool Dirty;
    public bool Empty;
    public bool LastSaveHadEntities;
    public long LastSaveTime;

    private static readonly ILogger<Chunk> s_logger = Log.Instance.For<Chunk>();

    public Chunk(World world, int x, int z)
    {
        BlockEntities = [];
        Entities = new List<Entity>[8];
        TerrainPopulated = false;
        Dirty = false;
        LastSaveHadEntities = false;
        LastSaveTime = 0L;
        World = world;
        X = x;
        Z = z;
        HeightMap = new byte[256];

        for (int i = 0; i < Entities.Length; i++)
        {
            Entities[i] = [];
        }
    }

    public Chunk(World world, byte[] blocks, int x, int z) : this(world, x, z)
    {
        Blocks = blocks;
        Meta = new ChunkNibbleArray(blocks.Length);
        SkyLight = new ChunkNibbleArray(blocks.Length);
        BlockLight = new ChunkNibbleArray(blocks.Length);
    }

    public virtual bool ChunkPosEquals(int x, int z) => x == X && z == Z;

    public virtual int GetHeight(int localX, int localZ)
    {
        return HeightMap[localZ << 4 | localX];
    }

    public virtual void PopulateLight() { }

    public virtual void PopulateHeightMapOnly()
    {
        int minHeight = 127;

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                int y = 127;
                int index = localX << 11 | localZ << 7;

                while (y > 0 && Block.BlockLightOpacity[Blocks[index + y - 1]] == 0)
                {
                    --y;
                }

                HeightMap[localZ << 4 | localX] = (byte)y;
                if (y < minHeight) minHeight = y;
            }
        }

        MinHeightMapValue = minHeight;
        Dirty = true;
    }

    public virtual void PopulateHeightMap()
    {
        int minHeight = 127;

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                int y = 127;
                int index = localX << 11 | localZ << 7;

                while (y > 0 && Block.BlockLightOpacity[Blocks[index + y - 1]] == 0)
                {
                    --y;
                }

                HeightMap[localZ << 4 | localX] = (byte)y;
                if (y < minHeight) minHeight = y;

                if (!World.dimension.HasCeiling)
                {
                    int lightLevel = 15;
                    int currentY = 127;

                    do
                    {
                        lightLevel -= Block.BlockLightOpacity[Blocks[index + currentY]];
                        if (lightLevel > 0)
                        {
                            SkyLight.SetNibble(localX, currentY, localZ, lightLevel);
                        }
                        --currentY;
                    } while (currentY > 0 && lightLevel > 0);
                }
            }
        }

        MinHeightMapValue = minHeight;

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                LightGaps(localX, localZ);
            }
        }

        Dirty = true;
    }

    public virtual void PopulateBlockLight() { }

    private void LightGaps(int localX, int localZ)
    {
        int height = GetHeight(localX, localZ);
        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;

        LightGap(worldX - 1, worldZ, height);
        LightGap(worldX + 1, worldZ, height);
        LightGap(worldX, worldZ - 1, height);
        LightGap(worldX, worldZ + 1, height);
    }

    private void LightGap(int worldX, int worldZ, int height)
    {
        int topY = World.getTopY(worldX, worldZ);
        if (topY > height)
        {
            World.queueLightUpdate(LightType.Sky, worldX, height, worldZ, worldX, topY, worldZ);
            Dirty = true;
        }
        else if (topY < height)
        {
            World.queueLightUpdate(LightType.Sky, worldX, topY, worldZ, worldX, height, worldZ);
            Dirty = true;
        }
    }

    private void UpdateHeightMap(int localX, int y, int localZ)
    {
        int oldHeight = HeightMap[localZ << 4 | localX];
        int newHeight = oldHeight;

        if (y > oldHeight) newHeight = y;

        int index = localX << 11 | localZ << 7;
        while (newHeight > 0 && Block.BlockLightOpacity[Blocks[index + newHeight - 1]] == 0)
        {
            --newHeight;
        }

        if (newHeight == oldHeight) return;

        World.setBlocksDirty(localX, localZ, newHeight, oldHeight);
        HeightMap[localZ << 4 | localX] = (byte)newHeight;

        if (newHeight < MinHeightMapValue)
        {
            MinHeightMapValue = newHeight;
        }
        else
        {
            int min = 127;
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    if (HeightMap[j << 4 | i] < min)
                    {
                        min = HeightMap[j << 4 | i];
                    }
                }
            }
            MinHeightMapValue = min;
        }

        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;

        if (newHeight < oldHeight)
        {
            for (int currY = newHeight; currY < oldHeight; ++currY)
            {
                SkyLight.SetNibble(localX, currY, localZ, 15);
            }
        }
        else
        {
            World.queueLightUpdate(LightType.Sky, worldX, oldHeight, worldZ, worldX, newHeight, worldZ);
            for (int currY = oldHeight; currY < newHeight; ++currY)
            {
                SkyLight.SetNibble(localX, currY, localZ, 0);
            }
        }

        int lightLevel = 15;
        int updateY = newHeight;

        while (newHeight > 0 && lightLevel > 0)
        {
            SkyLight.SetNibble(localX, newHeight, localZ, lightLevel);
            --newHeight;

            int opacity = Block.BlockLightOpacity[GetBlockId(localX, newHeight, localZ)];
            if (opacity == 0) opacity = 1;

            lightLevel -= opacity;
            if (lightLevel < 0) lightLevel = 0;
        }

        while (newHeight > 0 && Block.BlockLightOpacity[GetBlockId(localX, newHeight - 1, localZ)] == 0)
        {
            --newHeight;
        }

        if (newHeight != updateY)
        {
            World.queueLightUpdate(LightType.Sky, worldX - 1, newHeight, worldZ - 1, worldX + 1, updateY, worldZ + 1);
        }

        Dirty = true;
    }

    public virtual int GetBlockId(int x, int y, int z)
    {
        return Blocks[x << 11 | z << 7 | y] & 255;
    }

    public virtual bool SetBlock(int localX, int y, int localZ, int rawId, int meta)
    {
        byte newId = (byte)rawId;
        int height = HeightMap[localZ << 4 | localX];
        int oldId = Blocks[localX << 11 | localZ << 7 | y];

        if (oldId == rawId && Meta.GetNibble(localX, y, localZ) == meta) return false;

        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;
        Blocks[localX << 11 | localZ << 7 | y] = newId;

        if (oldId != 0 && !World.isRemote)
        {
            Block.Blocks[oldId].onBreak(World, worldX, y, worldZ);
        }

        Meta.SetNibble(localX, y, localZ, meta);

        if (!World.dimension.HasCeiling)
        {
            if (Block.BlockLightOpacity[newId] != 0)
            {
                if (y >= height) UpdateHeightMap(localX, y + 1, localZ);
            }
            else if (y == height - 1)
            {
                UpdateHeightMap(localX, y, localZ);
            }

            World.queueLightUpdate(LightType.Sky, worldX, y, worldZ, worldX, y, worldZ);
        }

        World.queueLightUpdate(LightType.Block, worldX, y, worldZ, worldX, y, worldZ);
        LightGaps(localX, localZ);
        Meta.SetNibble(localX, y, localZ, meta);

        if (rawId != 0)
        {
            Block.Blocks[rawId].onPlaced(World, worldX, y, worldZ);
        }

        Dirty = true;
        return true;
    }

    public virtual bool SetBlock(int localX, int y, int localZ, int rawId)
    {
        byte newId = (byte)rawId;
        int height = HeightMap[localZ << 4 | localX];
        int oldId = Blocks[localX << 11 | localZ << 7 | y];

        if (oldId == rawId) return false;

        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;
        Blocks[localX << 11 | localZ << 7 | y] = newId;

        if (oldId != 0)
        {
            Block.Blocks[oldId].onBreak(World, worldX, y, worldZ);
        }

        Meta.SetNibble(localX, y, localZ, 0);

        if (Block.BlockLightOpacity[newId] != 0)
        {
            if (y >= height) UpdateHeightMap(localX, y + 1, localZ);
        }
        else if (y == height - 1)
        {
            UpdateHeightMap(localX, y, localZ);
        }

        World.queueLightUpdate(LightType.Sky, worldX, y, worldZ, worldX, y, worldZ);
        World.queueLightUpdate(LightType.Block, worldX, y, worldZ, worldX, y, worldZ);
        LightGaps(localX, localZ);

        if (rawId != 0 && !World.isRemote)
        {
            Block.Blocks[rawId].onPlaced(World, worldX, y, worldZ);
        }

        Dirty = true;
        return true;
    }

    public virtual int GetBlockMeta(int x, int y, int z) => Meta.GetNibble(x, y, z);

    public virtual void SetBlockMeta(int x, int y, int z, int meta)
    {
        Dirty = true;
        Meta.SetNibble(x, y, z, meta);
    }

    public virtual int GetLight(LightType lightType, int x, int y, int z)
    {
        return lightType == LightType.Sky ? SkyLight.GetNibble(x, y, z) : lightType == LightType.Block ? BlockLight.GetNibble(x, y, z) : 0;
    }

    public virtual void SetLight(LightType lightType, int x, int y, int z, int value)
    {
        Dirty = true;
        if (lightType == LightType.Sky) SkyLight.SetNibble(x, y, z, value);
        else if (lightType == LightType.Block) BlockLight.SetNibble(x, y, z, value);
    }

    public virtual int GetLight(int x, int y, int z, int ambientDarkness)
    {
        int sky = SkyLight.GetNibble(x, y, z);
        if (sky > 0) HasSkyLight = true;

        sky -= ambientDarkness;
        int block = BlockLight.GetNibble(x, y, z);

        return block > sky ? block : sky;
    }

    public virtual void AddEntity(Entity entity)
    {
        LastSaveHadEntities = true;
        int chunkX = MathHelper.Floor(entity.x / 16.0D);
        int chunkZ = MathHelper.Floor(entity.z / 16.0D);

        if (chunkX != X || chunkZ != Z)
        {
            s_logger.LogWarning($"Entity in wrong chunk location! {entity}");
            s_logger.LogDebug(Environment.StackTrace);
        }

        int slice = MathHelper.Floor(entity.y / 16.0D);
        if (slice < 0) slice = 0;
        if (slice >= Entities.Length) slice = Entities.Length - 1;

        entity.isPersistent = true;
        entity.chunkX = X;
        entity.chunkSlice = slice;
        entity.chunkZ = Z;
        Entities[slice].Add(entity);
    }

    public virtual void RemoveEntity(Entity entity) => RemoveEntity(entity, entity.chunkSlice);

    public virtual void RemoveEntity(Entity entity, int chunkSlice)
    {
        if (chunkSlice < 0) chunkSlice = 0;
        if (chunkSlice >= Entities.Length) chunkSlice = Entities.Length - 1;

        Entities[chunkSlice].Remove(entity);
    }

    public virtual bool IsAboveMaxHeight(int localX, int y, int localZ)
    {
        return y >= HeightMap[localZ << 4 | localX];
    }

    public virtual BlockEntity? GetBlockEntity(int localX, int y, int localZ)
    {
        BlockPos pos = new(localX, y, localZ);

        if (!BlockEntities.TryGetValue(pos, out BlockEntity? entity))
        {
            int id = GetBlockId(localX, y, localZ);
            if (id == 0 || !Block.BlocksWithEntity[id]) return null;

            BlockWithEntity blockWithEntity = (BlockWithEntity)Block.Blocks[id];
            blockWithEntity.onPlaced(World, X * 16 + localX, y, Z * 16 + localZ);
            BlockEntities.TryGetValue(pos, out entity);
        }

        if (entity != null && entity.isRemoved())
        {
            BlockEntities.Remove(pos);
            return null;
        }

        return entity;
    }

    public virtual void AddBlockEntity(BlockEntity blockEntity)
    {
        int localX = blockEntity.X - X * 16;
        int localZ = blockEntity.Z - Z * 16;
        SetBlockEntity(localX, blockEntity.Y, localZ, blockEntity);

        if (Loaded) World.blockEntities.Add(blockEntity);
    }

    public virtual void SetBlockEntity(int localX, int y, int localZ, BlockEntity blockEntity)
    {
        BlockPos pos = new(localX, y, localZ);
        blockEntity.World = World;
        blockEntity.X = X * 16 + localX;
        blockEntity.Y = y;
        blockEntity.Z = Z * 16 + localZ;

        int id = GetBlockId(localX, y, localZ);
        if (id != 0 && Block.Blocks[id] is BlockWithEntity)
        {
            blockEntity.cancelRemoval();
            BlockEntities[pos] = blockEntity;
        }
        else
        {
            s_logger.LogWarning("Attempted to place a tile entity where there was no entity tile block!");
        }
    }

    public virtual void RemoveBlockEntityAt(int localX, int y, int localZ)
    {
        BlockPos pos = new(localX, y, localZ);
        if (Loaded && BlockEntities.Remove(pos, out BlockEntity? entity))
        {
            entity.markRemoved();
        }
    }

    public virtual void Load()
    {
        Loaded = true;
        World.processBlockUpdates(BlockEntities.Values);

        foreach (List<Entity> list in Entities)
        {
            World.addEntities(list);
        }
    }

    public virtual void Unload()
    {
        Loaded = false;

        foreach (BlockEntity var2 in BlockEntities.Values)
        {
            var2.markRemoved();
        }

        for (int var3 = 0; var3 < Entities.Length; ++var3)
        {
            World.unloadEntities(Entities[var3]);
        }

    }

    public virtual void MarkDirty() => Dirty = true;

    public virtual void CollectOtherEntities(Entity except, Box box, List<Entity> result)
    {
        int minSlice = MathHelper.Floor((box.MinY - 2.0D) / 16.0D);
        int maxSlice = MathHelper.Floor((box.MaxY + 2.0D) / 16.0D);

        if (minSlice < 0) minSlice = 0;
        if (maxSlice >= Entities.Length) maxSlice = Entities.Length - 1;

        for (int i = minSlice; i <= maxSlice; ++i)
        {
            foreach (Entity entity in Entities[i])
            {
                if (entity != except && entity.boundingBox.Intersects(box))
                {
                    result.Add(entity);
                }
            }
        }
    }

    public virtual void CollectEntitiesOfType<T>(Box box, List<T> result) where T : Entity
    {
        int minSlice = MathHelper.Floor((box.MinY - 2.0D) / 16.0D);
        int maxSlice = MathHelper.Floor((box.MaxY + 2.0D) / 16.0D);

        if (minSlice < 0) minSlice = 0;
        if (maxSlice >= Entities.Length) maxSlice = Entities.Length - 1;

        for (int i = minSlice; i <= maxSlice; ++i)
        {
            foreach (Entity entity in Entities[i])
            {
                if (entity is T typedEntity && entity.boundingBox.Intersects(box))
                {
                    result.Add(typedEntity);
                }
            }
        }
    }

    public virtual bool ShouldSave(bool saveEntities)
    {
        if (Empty) return false;

        if (saveEntities)
        {
            if (LastSaveHadEntities && World.getTime() != LastSaveTime) return true;
        }
        else if (LastSaveHadEntities && World.getTime() >= LastSaveTime + 600L)
        {
            return true;
        }

        return Dirty;
    }

    public virtual int LoadFromPacket(byte[] bytes, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, int offset)
    {
        int sizeX = maxX - minX;
        int sizeY = maxY - minY;
        int sizeZ = maxZ - minZ;
        bool isFullChunk = sizeX == 16 && sizeY == 128 && sizeZ == 16;

        Profiler.Start(isFullChunk ? "loadFromPacketFull" : "loadFromPacketSmall");

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                int index = x << 11 | z << 7 | minY;
                Buffer.BlockCopy(bytes, offset, Blocks, index, sizeY);
                offset += sizeY;
            }
        }

        PopulateHeightMapOnly();

        int halfSizeY = sizeY / 2;

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                int index = (x << 11 | z << 7 | minY) >> 1;
                Buffer.BlockCopy(bytes, offset, Meta.Bytes, index, halfSizeY);
                offset += halfSizeY;
            }
        }

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                int index = (x << 11 | z << 7 | minY) >> 1;
                Buffer.BlockCopy(bytes, offset, BlockLight.Bytes, index, halfSizeY);
                offset += halfSizeY;
            }
        }

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                int index = (x << 11 | z << 7 | minY) >> 1;
                Buffer.BlockCopy(bytes, offset, SkyLight.Bytes, index, halfSizeY);
                offset += halfSizeY;
            }
        }

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                for (int y = minY; y < maxY; y++)
                {
                    int id = GetBlockId(x, y, z);
                    if (id > 0 && Block.BlocksWithEntity[id])
                    {
                        GetBlockEntity(x, y, z);
                    }
                }
            }
        }

        Profiler.Stop(isFullChunk ? "loadFromPacketFull" : "loadFromPacketSmall");
        return offset;
    }

    public int ToPacket(byte[] bytes, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, int offset)
    {
        int sizeX = maxX - minX;
        int sizeY = maxY - minY;
        int sizeZ = maxZ - minZ;

        if (sizeX * sizeY * sizeZ == Blocks.Length)
        {
            Buffer.BlockCopy(Blocks, 0, bytes, offset, Blocks.Length);
            offset += Blocks.Length;
            Buffer.BlockCopy(Meta.Bytes, 0, bytes, offset, Meta.Bytes.Length);
            offset += Meta.Bytes.Length;
            Buffer.BlockCopy(BlockLight.Bytes, 0, bytes, offset, BlockLight.Bytes.Length);
            offset += BlockLight.Bytes.Length;
            Buffer.BlockCopy(SkyLight.Bytes, 0, bytes, offset, SkyLight.Bytes.Length);
            return offset + SkyLight.Bytes.Length;
        }
        else
        {
            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = x << 11 | z << 7 | minY;
                    Buffer.BlockCopy(Blocks, index, bytes, offset, sizeY);
                    offset += sizeY;
                }
            }

            int halfSizeY = sizeY / 2;

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = (x << 11 | z << 7 | minY) >> 1;
                    Buffer.BlockCopy(Meta.Bytes, index, bytes, offset, halfSizeY);
                    offset += halfSizeY;
                }
            }

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = (x << 11 | z << 7 | minY) >> 1;
                    Buffer.BlockCopy(BlockLight.Bytes, index, bytes, offset, halfSizeY);
                    offset += halfSizeY;
                }
            }

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = (x << 11 | z << 7 | minY) >> 1;
                    Buffer.BlockCopy(SkyLight.Bytes, index, bytes, offset, halfSizeY);
                    offset += halfSizeY;
                }
            }

            return offset;
        }
    }

    public virtual JavaRandom GetSlimeRandom(long scrambler)
    {
        return new JavaRandom(World.getSeed() + X * X * 4987142 + X * 5947611 + Z * Z * 4392871L + Z * 389711 ^ scrambler);
    }

    public virtual bool IsEmpty() => false;

    public void Fill() => BlockSource.Fill(Blocks);
}
