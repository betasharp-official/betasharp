using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;

namespace BetaSharp.Worlds.Core.Systems;

public interface IBlockReader
{
    public int GetBlockId(int x, int y, int z);
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
    public void MarkChunkDirty(int x, int z);
    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox);
    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false);
    public bool IsPosLoaded(int x, int y, int z);
    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate);
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity);
}
