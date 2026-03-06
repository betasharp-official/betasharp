using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Generation.Biomes.Source;

namespace BetaSharp.Worlds.Core;

/// <summary>
///     Null Object implementation of IBlockAccess for use in contexts where no world is
///     available (inventory rendering, held-item rendering, entity block rendering).
///     Returns safe open-air defaults so all block renderers function without modification.
/// </summary>
public sealed class NullBlockAccess : IBlockAccess
{
    public static readonly NullBlockAccess Instance = new();

    private NullBlockAccess()
    {
    }

    public int GetBlockId(int x, int y, int z) => 0;

    public BlockEntity? GetBlockEntity(int x, int y, int z) => null;

    public float GetNaturalBrightness(int x, int y, int z, int blockLight) => 1.0f;

    public float GetLuminance(int x, int y, int z) => 1.0f;

    public int GetBlockMeta(int x, int y, int z) => 0;

    public Material GetMaterial(int x, int y, int z) => Material.Air;

    public bool IsOpaque(int x, int y, int z) => false;

    public bool ShouldSuffocate(int x, int y, int z) => false;

    public BiomeSource GetBiomeSource() => null!;
}
