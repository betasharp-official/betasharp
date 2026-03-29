using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage.RegionFormat;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Chunks.Storage;

internal class RegionChunkStorage : IChunkStorage
{
    private readonly ILogger<RegionChunkStorage> _logger = Log.Instance.For<RegionChunkStorage>();
    private readonly string _dir;

    public RegionChunkStorage(string inputDir)
    {
        _dir = inputDir;
    }

    public Chunk? LoadChunk(IWorldContext world, int chunkX, int chunkZ)
    {
        using ChunkDataStream? s = RegionIo.GetChunkInputStream(_dir, chunkX, chunkZ);
        if (s == null)
        {
            return null;
        }

        Stream stream = s.Stream;

        if (stream != null)
        {
            NBTTagCompound chunkTag = NbtIo.Read(stream);
            if (!chunkTag.HasKey("Level"))
            {
                _logger.LogInformation($"Chunk file at {chunkX},{chunkZ} is missing level data, skipping");
                return null;
            }
            else if (!chunkTag.GetCompoundTag("Level").HasKey("Blocks"))
            {
                _logger.LogInformation($"Chunk file at {chunkX},{chunkZ} is missing block data, skipping");
                return null;
            }
            else
            {
                Chunk chunk = LoadChunkFromNbt(world, chunkTag.GetCompoundTag("Level"));
                if (!chunk.ChunkPosEquals(chunkX, chunkZ))
                {
                    _logger.LogInformation($"Chunk file at {chunkX},{chunkZ} is in the wrong location; relocating. (Expected {chunkX}, {chunkZ}, got {chunk.X}, {chunk.Z})");
                    chunkTag.SetInteger("xPos", chunkX);
                    chunkTag.SetInteger("zPos", chunkZ);
                    chunk = LoadChunkFromNbt(world, chunkTag.GetCompoundTag("Level"));
                }

                chunk.Fill();
                return chunk;
            }
        }
        else
        {
            return null;
        }
    }

    public void SaveChunk(IWorldContext world, Chunk chunk, Action unused1, long unused2)
    {
        try
        {
            using Stream? stream = RegionIo.GetChunkOutputStream(_dir, chunk.X, chunk.Z);
            if (stream == null)
            {
                return;
            }

            NBTTagCompound tag = new();
            NBTTagCompound levelTag = new();
            tag.SetTag("Level", levelTag);
            storeChunkInCompound(chunk, world, levelTag);
            NbtIo.Write(tag, stream);
            WorldProperties properties = world.Properties;
            properties.SizeOnDisk += RegionIo.GetSizeDelta(_dir, chunk.X, chunk.Z);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception");
        }
    }

    public static void storeChunkInCompound(Chunk chunk, IWorldContext world, NBTTagCompound nbt)
    {
        nbt.SetInteger("xPos", chunk.X);
        nbt.SetInteger("zPos", chunk.Z);
        nbt.SetLong("LastUpdate", world.GetTime());
        nbt.SetByteArray("Blocks", chunk.Blocks);
        nbt.SetByteArray("Data", chunk.Meta.Bytes);
        nbt.SetByteArray("SkyLight", chunk.SkyLight.Bytes);
        nbt.SetByteArray("BlockLight", chunk.BlockLight.Bytes);
        nbt.SetByteArray("HeightMap", chunk.HeightMap);
        nbt.SetBoolean("TerrainPopulated", chunk.TerrainPopulated);
        chunk.LastSaveHadEntities = false;
        NBTTagList entityTags = new();

        NBTTagCompound entityTag;
        for (int entitySlice = 0; entitySlice < chunk.Entities.Length; ++entitySlice)
        {
            foreach (Entity entity in chunk.Entities[entitySlice])
            {
                chunk.LastSaveHadEntities = true;
                entityTag = new NBTTagCompound();
                if (entity.saveSelfNbt(entityTag))
                {
                    entityTags.SetTag(entityTag);
                }
            }
        }

        nbt.SetTag("Entities", entityTags);
        NBTTagList blockEntityTags = new();

        foreach (BlockEntity blockEntity in chunk.BlockEntities.Values)
        {
            entityTag = new NBTTagCompound();
            blockEntity.writeNbt(entityTag);
            blockEntityTags.SetTag(entityTag);
        }

        nbt.SetTag("TileEntities", blockEntityTags);

        if (world.IsRemote) return;

        NBTTagList tileTickTags = new();
        long worldTime = world.GetTime();
        foreach ((int x, int y, int z, int blockId, long scheduledTime, long scheduledOrder) in world.TickScheduler.GetPendingTicksInChunk(chunk.X, chunk.Z))
        {
            long delta = scheduledTime - worldTime;
            int t = (int)Math.Clamp(delta, (long)int.MinValue, (long)int.MaxValue);
            int p = scheduledOrder > int.MaxValue ? int.MaxValue : (int)scheduledOrder;
            NBTTagCompound tickTag = new();
            tickTag.SetInteger("x", x);
            tickTag.SetInteger("y", y);
            tickTag.SetInteger("z", z);
            tickTag.SetInteger("t", t);
            tickTag.SetInteger("p", p);
            tickTag.SetInteger("i", blockId);
            tileTickTags.SetTag(tickTag);
        }

        if (tileTickTags.TagCount() > 0)
        {
            nbt.SetTag("TileTicks", tileTickTags);
        }
    }

    public static Chunk LoadChunkFromNbt(IWorldContext world, NBTTagCompound nbt)
    {
        int chunkX = nbt.GetInteger("xPos");
        int chunkZ = nbt.GetInteger("zPos");
        Chunk chunk = new(world, chunkX, chunkZ)
        {
            Blocks = nbt.GetByteArray("Blocks"),
            Meta = new ChunkNibbleArray(nbt.GetByteArray("Data")),
            SkyLight = new ChunkNibbleArray(nbt.GetByteArray("SkyLight")),
            BlockLight = new ChunkNibbleArray(nbt.GetByteArray("BlockLight")),
            HeightMap = nbt.GetByteArray("HeightMap"),
            TerrainPopulated = nbt.GetBoolean("TerrainPopulated")
        };
        if (!chunk.Meta.IsInitialized)
        {
            chunk.Meta = new ChunkNibbleArray(chunk.Blocks.Length);
        }

        if (chunk.HeightMap == null || !chunk.SkyLight.IsInitialized)
        {
            chunk.HeightMap = new byte[256];
            chunk.SkyLight = new ChunkNibbleArray(chunk.Blocks.Length);
            chunk.PopulateHeightMap();
        }
        else if (chunk.HeightMap.Length == 256)
        {
            for (int i = 0; i < 256; i++)
            {
                if (chunk.HeightMap[i] > 127)
                {
                    chunk.PopulateHeightMapOnly();
                    break;
                }
            }
        }

        if (!chunk.BlockLight.IsInitialized)
        {
            chunk.BlockLight = new ChunkNibbleArray(chunk.Blocks.Length);
            chunk.PopulateLight();
        }

        NBTTagList entityTags = nbt.GetTagList("Entities");
        if (entityTags != null)
        {
            for (int entityIndex = 0; entityIndex < entityTags.TagCount(); ++entityIndex)
            {
                NBTTagCompound entityTag = (NBTTagCompound)entityTags.TagAt(entityIndex);
                Entity? entity = EntityRegistry.GetEntityFromNbt(entityTag, world);
                chunk.LastSaveHadEntities = true;
                if (entity != null)
                {
                    chunk.AddEntity(entity);
                }
            }
        }

        NBTTagList blockEntityTags = nbt.GetTagList("TileEntities");
        if (blockEntityTags != null)
        {
            for (int blockEntityIndex = 0; blockEntityIndex < blockEntityTags.TagCount(); ++blockEntityIndex)
            {
                NBTTagCompound blockEntityTag = (NBTTagCompound)blockEntityTags.TagAt(blockEntityIndex);
                BlockEntity? blockEntity = BlockEntity.CreateFromNbt(blockEntityTag);
                if (blockEntity != null)
                {
                    chunk.AddBlockEntity(blockEntity);
                }
            }
        }

        if (world.IsRemote || !nbt.HasKey("TileTicks")) return chunk;

        NBTTagList tileTickTags = nbt.GetTagList("TileTicks");
        int minWx = chunkX * 16;
        int maxWx = minWx + 15;
        int minWz = chunkZ * 16;
        int maxWz = minWz + 15;

        for (int i = 0; i < tileTickTags.TagCount(); i++)
        {
            try
            {
                if (tileTickTags.TagAt(i) is not NBTTagCompound tickTag)
                {
                    continue;
                }

                if (!tickTag.HasKey("i"))
                {
                    continue;
                }

                int blockId = tickTag.GetInteger("i");
                if (blockId <= 0 || blockId >= Block.Blocks.Length || Block.Blocks[blockId] == null)
                {
                    continue;
                }

                int x = tickTag.GetInteger("x");
                int y = tickTag.GetInteger("y");
                int z = tickTag.GetInteger("z");
                if (y < 0 || y > 127 || x < minWx || x > maxWx || z < minWz || z > maxWz)
                {
                    Log.Instance.For<RegionChunkStorage>().LogDebug("Skipping TileTicks entry with out-of-range coordinates ({X},{Y},{Z}) for chunk {ChunkX},{ChunkZ}", x, y, z, chunkX, chunkZ);
                    continue;
                }

                int t = tickTag.GetInteger("t");
                world.TickScheduler.ScheduleBlockUpdateFromChunkLoad(x, y, z, blockId, t);
            }
            catch (InvalidCastException)
            {
                Log.Instance.For<RegionChunkStorage>().LogDebug("Skipping TileTicks entry with unexpected NBT types at index {Index}", i);
            }
        }


        return chunk;
    }

    public void SaveEntities(IWorldContext world, Chunk chunk)
    {
    }

    public void Tick()
    {
    }

    public void Flush()
    {
    }

    public void FlushToDisk()
    {
    }
}
