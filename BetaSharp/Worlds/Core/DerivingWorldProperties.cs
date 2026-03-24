using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Core;

internal class DerivingWorldProperties : WorldProperties
{
    private readonly WorldProperties _baseProperties;

    public DerivingWorldProperties(WorldProperties baseProperties) => _baseProperties = baseProperties;

    public override long RandomSeed => _baseProperties.RandomSeed;
    public override int SpawnX => _baseProperties.SpawnX;
    public override int SpawnY => _baseProperties.SpawnY;
    public override int SpawnZ => _baseProperties.SpawnZ;
    public override long WorldTime => _baseProperties.WorldTime;
    public override long LastTimePlayed => _baseProperties.LastTimePlayed;
    public override long SizeOnDisk => _baseProperties.SizeOnDisk;

    public override NBTTagCompound? PlayerTag
    {
        get => _baseProperties.PlayerTag;
        set => _baseProperties.PlayerTag = value;
    }

    public override NBTTagCompound? RulesTag
    {
        get => _baseProperties.RulesTag;
        set => _baseProperties.RulesTag = value;
    }

    public override int Dimension => _baseProperties.Dimension;
    public override string LevelName => _baseProperties.LevelName;
    public override int SaveVersion => _baseProperties.SaveVersion;
    public override bool IsRaining => _baseProperties.IsRaining;
    public override int RainTime => _baseProperties.RainTime;
    public override bool IsThundering => _baseProperties.IsThundering;
    public override int ThunderTime => _baseProperties.ThunderTime;

    public override WorldType TerrainType
    {
        get => _baseProperties.TerrainType;
        set => _baseProperties.TerrainType = value;
    }

    public override string GeneratorOptions
    {
        get => _baseProperties.GeneratorOptions;
        set => _baseProperties.GeneratorOptions = value;
    }

    public override void SetSpawn(int x, int y, int z)
    {
    }
}
