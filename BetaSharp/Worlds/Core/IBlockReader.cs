using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Generation.Biomes.Source;

namespace BetaSharp.Worlds.Core;

public interface IBlockReader
{
    public int GetBlockId(int x, int y, int z);
    public BlockEntity? GetBlockEntity(int x, int y, int z);
    public int GetBlockMeta(int x, int y, int z);
    public Material GetMaterial(int x, int y, int z);
    public bool IsOpaque(int x, int y, int z);
    public bool ShouldSuffocate(int x, int y, int z);
    public BiomeSource GetBiomeSource();
    public bool IsAir(int x, int y, int z);
    public int GetBrightness(int x, int y, int z);
    public bool IsTopY(int x, int y, int z);
    public int GetTopY(int x, int z);
    public int GetTopSolidBlockY(int x, int z);
    public int GetSpawnPositionValidityY(int x, int z);
}
