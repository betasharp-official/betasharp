using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Biomes.Source;

namespace BetaSharp.Worlds;

/// <summary>
/// IBlockAccess implementation for rendering a single block in item/entity contexts.
/// Reports a fixed (blockId, metadata) at (0,0,0), open-air defaults elsewhere, and
/// a constant luminance so custom block renderers can respect caller-provided brightness.
/// </summary>
public sealed class ItemRenderBlockAccess : IBlockAccess
{
    private readonly int _blockId;
    private readonly int _metadata;
    private readonly float _brightness;

    public ItemRenderBlockAccess(int blockId, int metadata, float brightness)
    {
        _blockId = blockId;
        _metadata = metadata;
        _brightness = brightness;
    }

    public int getBlockId(int x, int y, int z)
    {
        return x == 0 && y == 0 && z == 0 ? _blockId : 0;
    }

    public BlockEntity? getBlockEntity(int x, int y, int z) => null;

    public float getNaturalBrightness(int x, int y, int z, int blockLight) => _brightness;

    public float getLuminance(int x, int y, int z) => _brightness;

    public int getBlockMeta(int x, int y, int z)
    {
        return x == 0 && y == 0 && z == 0 ? _metadata : 0;
    }

    public Material getMaterial(int x, int y, int z) => Material.Air;

    public bool isOpaque(int x, int y, int z) => false;

    public bool shouldSuffocate(int x, int y, int z) => false;

    public BiomeSource getBiomeSource() => null!;
}
