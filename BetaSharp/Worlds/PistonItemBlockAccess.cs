using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Biomes.Source;

namespace BetaSharp.Worlds;

/// <summary>
/// IBlockAccess implementation for rendering a single piston block in item form.
/// Reports a fixed (blockId, metadata) at (0,0,0) and open-air defaults elsewhere.
/// </summary>
public sealed class PistonItemBlockAccess : IBlockAccess
{
    private readonly int _blockId;
    private readonly int _metadata;

    public PistonItemBlockAccess(int blockId, int metadata)
    {
        _blockId = blockId;
        _metadata = metadata;
    }

    public int getBlockId(int x, int y, int z)
    {
        return x == 0 && y == 0 && z == 0 ? _blockId : 0;
    }

    public BlockEntity? getBlockEntity(int x, int y, int z) => null;

    public float getNaturalBrightness(int x, int y, int z, int blockLight) => 1.0f;

    public float getLuminance(int x, int y, int z) => 1.0f;

    public int getBlockMeta(int x, int y, int z)
    {
        return x == 0 && y == 0 && z == 0 ? _metadata : 0;
    }

    public Material getMaterial(int x, int y, int z) => Material.Air;

    public bool isOpaque(int x, int y, int z) => false;

    public bool shouldSuffocate(int x, int y, int z) => false;

    public BiomeSource getBiomeSource() => null!;
}

