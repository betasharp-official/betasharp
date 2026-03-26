using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Core;

/// <summary>
/// IBlockAccess implementation for rendering a single block in item/entity contexts.
/// Reports a fixed (blockId, metadata) at (0,0,0), open-air defaults elsewhere, and
/// a constant luminance so custom block renderers can respect caller-provided brightness.
/// </summary>
public sealed class ItemRenderBlockAccess : IBlockReader, ILightProvider
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
        => x == 0 && y == 0 && z == 0 ? _blockId : 0;

    public BlockEntity? getBlockEntity(int x, int y, int z) => null;

    public float getNaturalBrightness(int x, int y, int z, int blockLight) => _brightness;

    public float getLuminance(int x, int y, int z) => _brightness;

    public int GetMeta(int x, int y, int z)
    {
        return x == 0 && y == 0 && z == 0 ? _metadata : 0;
    }

    public Material getMaterial(int x, int y, int z) => Material.Air;

    public bool isOpaque(int x, int y, int z) => false;

    public bool shouldSuffocate(int x, int y, int z) => false;

    public BiomeSource getBiomeSource() => null!;
    public int GetBlockId(int x, int y, int z) => getBlockId(x, y, z);
    public int GetBlockMeta(int x, int y, int z) => GetMeta(x, y, z);
    public BlockEntity? GetBlockEntity(int x, int y, int z) => getBlockEntity(x, y, z);
    public Material GetMaterial(int x, int y, int z) => getMaterial(x, y, z);
    public bool IsOpaque(int x, int y, int z) => isOpaque(x, y, z);
    public bool ShouldSuffocate(int x, int y, int z) => shouldSuffocate(x, y, z);
    public BiomeSource GetBiomeSource() => getBiomeSource();
    public bool IsAir(int x, int y, int z) => GetBlockId(x, y, z) == 0;
    public int GetBrightness(int x, int y, int z)
    {
        int value = (int)(_brightness * 15f);
        if (value < 0) value = 0;
        if (value > 15) value = 15;
        return value;
    }
    public bool IsTopY(int x, int y, int z) => false;
    public int GetTopY(int x, int z) => 0;
    public int GetTopSolidBlockY(int x, int z) => 0;
    public int GetSpawnPositionValidityY(int x, int z) => 0;
    public void MarkChunkDirty(int x, int z) => throw new NotImplementedException();

    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox) => 1.0f;
    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false) => new(HitResultType.MISS);
    public bool IsPosLoaded(int x, int y, int z) => true;
    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate) => false;
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity) => false;
    public float GetNaturalBrightness(int x, int y, int z, int minLight) => getNaturalBrightness(x, y, z, minLight);
    public float GetLuminance(int x, int y, int z) => getLuminance(x, y, z);
}
