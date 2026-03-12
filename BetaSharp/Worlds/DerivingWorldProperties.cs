using BetaSharp.NBT;

namespace BetaSharp.Worlds;

internal class DerivingWorldProperties : WorldProperties
{
    private readonly WorldProperties _baseProperties;
    public override long RandomSeed { get => _baseProperties.RandomSeed; }
    public override int SpawnX { get => _baseProperties.SpawnX; }
    public override int SpawnY { get => _baseProperties.SpawnY; }
    public override int SpawnZ { get => _baseProperties.SpawnZ; }
    public override long WorldTime { get => _baseProperties.WorldTime; }
    public override long LastTimePlayed { get => _baseProperties.LastTimePlayed; }
    public override long SizeOnDisk { get => _baseProperties.SizeOnDisk; }
    public override NBTTagCompound? PlayerTag { get => _baseProperties.PlayerTag; set => _baseProperties.PlayerTag = value; }
    public override NBTTagCompound? RulesTag { get => _baseProperties.RulesTag; set => _baseProperties.RulesTag = value; }
    public override int Dimension { get => _baseProperties.Dimension; }
    public override string LevelName { get => _baseProperties.LevelName; }
    public override int SaveVersion { get => _baseProperties.SaveVersion; }
    public override bool IsRaining { get => _baseProperties.IsRaining; }
    public override int RainTime { get => _baseProperties.RainTime; }
    public override bool IsThundering { get => _baseProperties.IsThundering; }
    public override int ThunderTime { get => _baseProperties.ThunderTime; }
    public override WorldType TerrainType { get => _baseProperties.TerrainType; set => _baseProperties.TerrainType = value; }
    public override string GeneratorOptions { get => _baseProperties.GeneratorOptions; set => _baseProperties.GeneratorOptions = value; }

    public DerivingWorldProperties(WorldProperties baseProperties)
    {
        _baseProperties = baseProperties;
    }

    public override void SetSpawn(int x, int y, int z)
    {
    }
}
