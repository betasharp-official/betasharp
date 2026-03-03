using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Biomes.Source;

namespace BetaSharp.Worlds;

/// <summary>
/// Null Object implementation of IBlockAccess for use in contexts where no world is
/// available (inventory rendering, held-item rendering, entity block rendering).
/// Returns safe open-air defaults so all block renderers function without modification.
/// </summary>
public sealed class NullBlockAccess : IBlockAccess
{
    public static readonly NullBlockAccess Instance = new();

    private NullBlockAccess() { }

    public int getBlockId(int x, int y, int z) => 0;

    public BlockEntity? getBlockEntity(int x, int y, int z) => null;

    public float getNaturalBrightness(int x, int y, int z, int blockLight) => 1.0f;

    public float getLuminance(int x, int y, int z) => 1.0f;

    public int getBlockMeta(int x, int y, int z) => 0;

    public Material getMaterial(int x, int y, int z) => Material.Air;

    public bool isOpaque(int x, int y, int z) => false;

    public bool shouldSuffocate(int x, int y, int z) => false;

    public BiomeSource getBiomeSource() => null!;
}
