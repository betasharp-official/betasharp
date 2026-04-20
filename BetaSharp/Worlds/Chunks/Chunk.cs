using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Profiling;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Chunks;

public class Chunk
{
    public static bool HasSkyLight;

    public byte[] Blocks;
    public ChunkNibbleArray Meta;
    public ChunkNibbleArray SkyLight;
    public ChunkNibbleArray BlockLight;
    public byte[] HeightMap = new byte[256];

    public bool Loaded;
    public IWorldContext World;
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

    public Chunk(IWorldContext world, int x, int z)
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

        for (int i = 0; i < Entities.Length; i++)
        {
            Entities[i] = [];
        }
    }

    public Chunk(IWorldContext world, NBTTagCompound nbt) : this(world, nbt.GetInteger("xPos"), nbt.GetInteger("zPos"))
    {
        Blocks = nbt.GetByteArray("Blocks");
        HeightMap = nbt.GetByteArray("HeightMap");
        TerrainPopulated = nbt.GetBoolean("TerrainPopulated");

        Meta = new ChunkNibbleArray(nbt.GetByteArray("Data"));
        SkyLight = new ChunkNibbleArray(nbt.GetByteArray("SkyLight"));
        BlockLight = new ChunkNibbleArray(nbt.GetByteArray("BlockLight"));
    }

    public Chunk(IWorldContext world, byte[] blocks, int x, int z) : this(world, x, z)
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
        int h = ChuckFormat.ChunkHeight - 1;
        int minHeight = h;

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                int y = h;
                int index = ChuckFormat.GetIndex(localX, localZ);

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
        int h = ChuckFormat.ChunkHeight - 1;
        int minHeight = h;

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                int y = h;
                int index = ChuckFormat.GetIndex(localX, localZ);

                while (y > 0 && Block.BlockLightOpacity[Blocks[index + y - 1]] == 0)
                {
                    --y;
                }

                HeightMap[localZ << 4 | localX] = (byte)y;
                if (y < minHeight) minHeight = y;

                if (!World.Dimension.HasCeiling)
                {
                    int lightLevel = 15;
                    int currentY = h;

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
        int topY = World.Reader.GetTopY(worldX, worldZ);
        if (topY > height)
        {
            World.Lighting.QueueLightUpdate(LightType.Sky, worldX, height, worldZ, worldX, topY, worldZ);
            Dirty = true;
        }
        else if (topY < height)
        {
            World.Lighting.QueueLightUpdate(LightType.Sky, worldX, topY, worldZ, worldX, height, worldZ);
            Dirty = true;
        }
    }

    private void UpdateHeightMap(int localX, int y, int localZ)
    {
        int oldHeight = HeightMap[localZ << 4 | localX];
        int newHeight = oldHeight;

        if (y > oldHeight) newHeight = y;

        int index = ChuckFormat.GetIndex(localX, localZ);
        while (newHeight > 0 && Block.BlockLightOpacity[Blocks[index + newHeight - 1]] == 0)
        {
            --newHeight;
        }

        if (newHeight == oldHeight) return;

        World.Broadcaster.SetBlocksDirty(localX, localZ, newHeight, oldHeight);
        HeightMap[localZ << 4 | localX] = (byte)newHeight;

        if (newHeight < MinHeightMapValue)
        {
            MinHeightMapValue = newHeight;
        }
        else
        {
            int min = ChuckFormat.ChunkHeight - 1;
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
            World.Lighting.QueueLightUpdate(LightType.Sky, worldX, oldHeight, worldZ, worldX, newHeight, worldZ);
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
            World.Lighting.QueueLightUpdate(LightType.Sky, worldX - 1, newHeight, worldZ - 1, worldX + 1, updateY, worldZ + 1);
        }

        Dirty = true;
    }

    public virtual int GetBlockId(int x, int y, int z) => this[x, y, z];

    public virtual int this[int x, int y, int z]
    {
        get => Blocks[ChuckFormat.GetIndex(x, y, z)];
        set => Blocks[ChuckFormat.GetIndex(x, y, z)] = (byte)value;
    }


    public virtual bool SetBlock(int localX, int y, int localZ, int rawId, int meta, bool notifyBlockPlaced = true)
    {
        int pos = ChuckFormat.GetIndex(localX,  y, localZ);
        byte newId = (byte)rawId;
        int height = HeightMap[localZ << 4 | localX];
        int oldId = Blocks[pos];

        if (oldId == rawId && Meta.GetNibble(localX, y, localZ) == meta) return false;

        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;
        Blocks[pos] = newId;

        if (notifyBlockPlaced && oldId != 0 && !World.IsRemote)
        {
            Block.Blocks[oldId].OnBreak(new OnBreakEvent(World, null, worldX, y, worldZ));
        }

        Meta.SetNibble(localX, y, localZ, meta);

        if (!World.Dimension.HasCeiling)
        {
            if (Block.BlockLightOpacity[newId] != 0)
            {
                if (y >= height) UpdateHeightMap(localX, y + 1, localZ);
            }
            else if (y == height - 1)
            {
                UpdateHeightMap(localX, y, localZ);
            }

            World.Lighting.QueueLightUpdate(LightType.Sky, worldX, y, worldZ, worldX, y, worldZ);
        }

        World.Lighting.QueueLightUpdate(LightType.Block, worldX, y, worldZ, worldX, y, worldZ);
        LightGaps(localX, localZ);
        Meta.SetNibble(localX, y, localZ, meta);

        if (notifyBlockPlaced && rawId != 0 && !World.IsRemote)
        {
            Block.Blocks[rawId].OnPlaced(new OnPlacedEvent(World, null, 0, 0, worldX, y, worldZ));
        }

        Dirty = true;
        return true;
    }

    public virtual bool SetBlock(int localX, int y, int localZ, int rawId, bool notifyBlockPlaced = true)
    {
        int pos = ChuckFormat.GetIndex(localX,  y, localZ);
        byte newId = (byte)rawId;
        int height = HeightMap[localZ << 4 | localX];
        int oldId = Blocks[pos];

        if (oldId == rawId) return false;

        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;
        Blocks[pos] = newId;

        if (oldId != 0)
        {
            Block.Blocks[oldId].OnBreak(new OnBreakEvent(World, null, worldX, y, worldZ));
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

        World.Lighting.QueueLightUpdate(LightType.Sky, worldX, y, worldZ, worldX, y, worldZ);
        World.Lighting.QueueLightUpdate(LightType.Block, worldX, y, worldZ, worldX, y, worldZ);
        LightGaps(localX, localZ);

        if (notifyBlockPlaced && rawId != 0 && !World.IsRemote)
        {
            Block.Blocks[rawId].OnPlaced(new OnPlacedEvent(World, null, 0, 0, worldX, y, worldZ));
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
        int chunkX = MathHelper.Floor(entity.X / 16.0D);
        int chunkZ = MathHelper.Floor(entity.Z / 16.0D);

        if (chunkX != X || chunkZ != Z)
        {
            s_logger.LogWarning($"Entity in wrong chunk location! {entity}");
            s_logger.LogDebug(Environment.StackTrace);
        }

        int slice = MathHelper.Floor(entity.Y / 16.0D);
        if (slice < 0) slice = 0;
        if (slice >= Entities.Length) slice = Entities.Length - 1;

        entity.IsPersistent = true;
        entity.ChunkX = X;
        entity.ChunkSlice = slice;
        entity.ChunkZ = Z;
        Entities[slice].Add(entity);
    }

    public virtual void RemoveEntity(Entity entity) => RemoveEntity(entity, entity.ChunkSlice);

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

        if (BlockEntities.TryGetValue(pos, out BlockEntity? entity))
        {
            if (entity != null && !entity.IsRemoved())
            {
                return entity;
            }
        }

        int worldX = X * 16 + localX;
        int worldZ = Z * 16 + localZ;

        entity = World.Entities.GetOrCreateBlockEntity<BlockEntity>(worldX, y, worldZ);

        if (entity != null)
        {
            BlockEntities[pos] = entity;
        }

        return entity;
    }

    public virtual void AddBlockEntity(BlockEntity blockEntity)
    {
        int localX = blockEntity.X - X * 16;
        int localZ = blockEntity.Z - Z * 16;
        SetBlockEntity(localX, blockEntity.Y, localZ, blockEntity);

        if (Loaded) World.Entities.BlockEntities.Add(blockEntity);
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
            blockEntity.CancelRemoval();
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
            entity.MarkRemoved();
        }
    }

    public virtual void Load()
    {
        Loaded = true;
        World.Entities.ProcessBlockUpdates(BlockEntities.Values);

        foreach (List<Entity> list in Entities)
        {
            World.Entities.AddEntities(list);
        }
    }

    public virtual void Unload()
    {
        Loaded = false;

        foreach (BlockEntity blockEntity in BlockEntities.Values)
        {
            blockEntity.MarkRemoved();
        }

        for (int sectionIndex = 0; sectionIndex < Entities.Length; ++sectionIndex)
        {
            World.Entities.UnloadEntities(Entities[sectionIndex]);
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
                if (entity != except && entity.BoundingBox.Intersects(box) && !entity.Dead)
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
                if (!entity.Dead && entity is T typedEntity && entity.BoundingBox.Intersects(box))
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
            if (LastSaveHadEntities && World.GetTime() != LastSaveTime) return true;
        }
        else if (LastSaveHadEntities && World.GetTime() >= LastSaveTime + 600L)
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
        bool isFullChunk = sizeX == 16 && sizeY == ChuckFormat.ChunkHeight && sizeZ == 16;

        using (Profiler.Begin(isFullChunk ? "LoadChunkFull" : "LoadChunkPartial"))
        {
            for (int x = minX; x < maxX; ++x)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    int index = ChuckFormat.GetIndex(x, minY, z);
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
                    int index = (ChuckFormat.GetIndex(x, minY, z)) >> 1;
                    Buffer.BlockCopy(bytes, offset, Meta.Bytes, index, halfSizeY);
                    offset += halfSizeY;
                }
            }

            for (int x = minX; x < maxX; ++x)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    int index = (ChuckFormat.GetIndex(x, minY, z)) >> 1;
                    Buffer.BlockCopy(bytes, offset, BlockLight.Bytes, index, halfSizeY);
                    offset += halfSizeY;
                }
            }

            for (int x = minX; x < maxX; ++x)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    int index = (ChuckFormat.GetIndex(x, minY, z)) >> 1;
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

            Loaded = true;
            return offset;
        }
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
                    int index = ChuckFormat.GetIndex(x, minY, z);
                    Buffer.BlockCopy(Blocks, index, bytes, offset, sizeY);
                    offset += sizeY;
                }
            }

            int halfSizeY = sizeY / 2;

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = (ChuckFormat.GetIndex(x, minY, z)) >> 1;
                    Buffer.BlockCopy(Meta.Bytes, index, bytes, offset, halfSizeY);
                    offset += halfSizeY;
                }
            }

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = (ChuckFormat.GetIndex(x, minY, z)) >> 1;
                    Buffer.BlockCopy(BlockLight.Bytes, index, bytes, offset, halfSizeY);
                    offset += halfSizeY;
                }
            }

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    int index = (ChuckFormat.GetIndex(x, minY, z)) >> 1;
                    Buffer.BlockCopy(SkyLight.Bytes, index, bytes, offset, halfSizeY);
                    offset += halfSizeY;
                }
            }

            return offset;
        }
    }

    public virtual JavaRandom GetSlimeRandom(long scrambler)
    {
        return new JavaRandom(World.Seed + X * X * 4987142 + X * 5947611 + Z * Z * 4392871L + Z * 389711 ^ scrambler);
    }

    public virtual bool IsEmpty() => false;

    public void Fill() => BlockSource.Fill(Blocks);
}
