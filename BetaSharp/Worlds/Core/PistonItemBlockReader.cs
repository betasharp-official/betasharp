using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Generation.Biomes.Source;

namespace BetaSharp.Worlds.Core;

/// <summary>
///     IBlockAccess implementation for rendering a single piston block in item form.
///     Reports a fixed (blockId, metadata) at (0,0,0) and open-air defaults elsewhere.
/// </summary>
public sealed class PistonItemBlockReader : IBlockReader
{
    private readonly int _blockId;
    private readonly int _metadata;

    public PistonItemBlockReader(int blockId, int metadata)
    {
        _blockId = blockId;
        _metadata = metadata;
    }

    public int GetBlockId(int x, int y, int z) => x == 0 && y == 0 && z == 0 ? _blockId : 0;

    public BlockEntity? GetBlockEntity(int x, int y, int z) => null;

    public float GetNaturalBrightness(int x, int y, int z, int blockLight) => 1.0f;

    public float GetLuminance(int x, int y, int z) => 1.0f;

    public int getBlockMeta(int x, int y, int z) => x == 0 && y == 0 && z == 0 ? _metadata : 0;

    public Material getMaterial(int x, int y, int z) => Material.Air;

    public bool IsOpaque(int x, int y, int z) => false;

    public bool ShouldSuffocate(int x, int y, int z) => false;

    public BiomeSource GetBiomeSource() => null!;
}
