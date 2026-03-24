using System;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Core.Systems;

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

    public bool IsOpaque(int x, int y, int z) => false;

    public bool ShouldSuffocate(int x, int y, int z) => false;

    public BiomeSource GetBiomeSource() => null!;
    public int GetBlockMeta(int x, int y, int z) => throw new NotImplementedException();
    public Material GetMaterial(int x, int y, int z) => throw new NotImplementedException();
    public bool IsAir(int x, int y, int z) => throw new NotImplementedException();
    public int GetBrightness(int x, int y, int z) => throw new NotImplementedException();
    public bool IsTopY(int x, int y, int z) => throw new NotImplementedException();
    public int GetTopY(int x, int z) => throw new NotImplementedException();
    public int GetTopSolidBlockY(int x, int z) => throw new NotImplementedException();
    public int GetSpawnPositionValidityY(int x, int z) => throw new NotImplementedException();
    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox) => throw new NotImplementedException();
    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false) => throw new NotImplementedException();
    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate) => throw new NotImplementedException();
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity) => throw new NotImplementedException();
    public bool IsPosLoaded(int x, int y, int z) => throw new NotImplementedException();

    public float GetNaturalBrightness(int x, int y, int z, int blockLight) => 1.0f;

    public float GetLuminance(int x, int y, int z) => 1.0f;

    public int getBlockMeta(int x, int y, int z) => x == 0 && y == 0 && z == 0 ? _metadata : 0;

    public Material getMaterial(int x, int y, int z) => Material.Air;
}
