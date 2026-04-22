using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Core.Systems;

public class WorldProperties
{
    protected WorldProperties()
    {
    }

    public WorldProperties(NBTTagCompound nbt)
    {
        RandomSeed = nbt.GetLong("RandomSeed");
        SpawnX = nbt.GetInteger("SpawnX");
        SpawnY = nbt.GetInteger("SpawnY");
        SpawnZ = nbt.GetInteger("SpawnZ");
        WorldTime = nbt.GetLong("Time");
        LastTimePlayed = nbt.GetLong("LastPlayed");
        LevelName = nbt.GetString("LevelName");
        SaveVersion = nbt.GetInteger("version");
        RainTime = nbt.GetInteger("rainTime");
        IsRaining = nbt.GetBoolean("raining");
        ThunderTime = nbt.GetInteger("thunderTime");
        IsThundering = nbt.GetBoolean("thundering");

        if (nbt.HasKey("generatorName"))
        {
            string generatorName = nbt.GetString("generatorName");
            TerrainType = WorldType.ParseWorldType(generatorName);
        }
        else
        {
            TerrainType = WorldType.Default;
        }

        if (nbt.HasKey("Player"))
        {
            PlayerTag = nbt.GetCompoundTag("Player");
            Dimension = PlayerTag.GetInteger("Dimension");
        }

        if (nbt.HasKey("GameRules"))
        {
            RulesTag = nbt.GetCompoundTag("GameRules");
        }

        if (nbt.HasKey("generatorOptions"))
        {
            GeneratorOptions = nbt.GetString("generatorOptions");
        }
    }

    public WorldProperties(long randomSeed, string levelName)
    {
        RandomSeed = randomSeed;
        LevelName = levelName;
        TerrainType = WorldType.Default;
    }

    public WorldProperties(WorldSettings settings, string levelName)
    {
        RandomSeed = settings.Seed;
        LevelName = levelName;
        TerrainType = settings.TerrainType;
        GeneratorOptions = settings.GeneratorOptions;
    }

    public WorldProperties(WorldProperties WorldProp)
    {
        RandomSeed = WorldProp.RandomSeed;
        SpawnX = WorldProp.SpawnX;
        SpawnY = WorldProp.SpawnY;
        SpawnZ = WorldProp.SpawnZ;
        WorldTime = WorldProp.WorldTime;
        LastTimePlayed = WorldProp.LastTimePlayed;
        SizeOnDisk = WorldProp.SizeOnDisk;
        PlayerTag = WorldProp.PlayerTag;
        RulesTag = WorldProp.RulesTag;
        Dimension = WorldProp.Dimension;
        LevelName = WorldProp.LevelName;
        SaveVersion = WorldProp.SaveVersion;
        RainTime = WorldProp.RainTime;
        TerrainType = WorldProp.TerrainType;
        IsRaining = WorldProp.IsRaining;
        ThunderTime = WorldProp.ThunderTime;
        IsThundering = WorldProp.IsThundering;
        GeneratorOptions = WorldProp.GeneratorOptions;
    }

    public virtual long RandomSeed { get; }
    public virtual int SpawnX { get; set; }
    public virtual int SpawnY { get; set; }
    public virtual int SpawnZ { get; set; }
    public virtual long WorldTime { get; set; }
    public virtual long LastTimePlayed { get; }
    public virtual long SizeOnDisk { get; set; }
    public virtual NBTTagCompound? PlayerTag { get; set; }
    public virtual NBTTagCompound? RulesTag { get; set; }
    public virtual int Dimension { get; }
    public virtual string LevelName { get; set; }
    public virtual int SaveVersion { get; set; }
    public virtual WorldType TerrainType { get; set; }
    public virtual bool IsRaining { get; set; }
    public virtual int RainTime { get; set; }
    public virtual bool IsThundering { get; set; }
    public virtual int ThunderTime { get; set; }
    public virtual string GeneratorOptions { get; set; } = "";

    public NBTTagCompound getNBTTagCompound()
    {
        NBTTagCompound nbt = new();
        UpdateTagCompound(nbt, PlayerTag);
        return nbt;
    }

    public NBTTagCompound getNBTTagCompoundWithPlayer(List<EntityPlayer> players)
    {
        NBTTagCompound nbt = new();
        NBTTagCompound? playerNbt = null;

        if (players.Count > 0 && players[0] is EntityPlayer player)
        {
            playerNbt = new NBTTagCompound();
            player.Write(playerNbt);
        }

        UpdateTagCompound(nbt, playerNbt);
        return nbt;
    }

    private void UpdateTagCompound(NBTTagCompound worldNbt, NBTTagCompound playerNbt)
    {
        worldNbt.SetLong("RandomSeed", RandomSeed);
        worldNbt.SetInteger("SpawnX", SpawnX);
        worldNbt.SetInteger("SpawnY", SpawnY);
        worldNbt.SetInteger("SpawnZ", SpawnZ);
        worldNbt.SetLong("Time", WorldTime);
        worldNbt.SetLong("SizeOnDisk", SizeOnDisk);

        worldNbt.SetLong("LastPlayed", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        worldNbt.SetString("LevelName", LevelName);
        worldNbt.SetInteger("version", SaveVersion);
        worldNbt.SetInteger("rainTime", RainTime);
        worldNbt.SetBoolean("raining", IsRaining);
        worldNbt.SetInteger("thunderTime", ThunderTime);
        worldNbt.SetBoolean("thundering", IsThundering);

        if (TerrainType != null)
        {
            worldNbt.SetString("generatorName", TerrainType.Name);
            worldNbt.SetString("generatorOptions", GeneratorOptions);
        }

        if (TerrainType != null)
        {
            worldNbt.SetString("generatorName", TerrainType.Name);
            worldNbt.SetString("generatorOptions", GeneratorOptions);
        }

        if (playerNbt != null)
        {
            worldNbt.SetCompoundTag("Player", playerNbt);
        }

        if (RulesTag != null)
        {
            worldNbt.SetCompoundTag("GameRules", RulesTag);
        }
    }

    public virtual void SetSpawn(int x, int y, int z)
    {
        SpawnX = x;
        SpawnY = y;
        SpawnZ = z;
    }

    public Vec3i GetSpawnPos() => new(SpawnX, SpawnY, SpawnZ);
}
