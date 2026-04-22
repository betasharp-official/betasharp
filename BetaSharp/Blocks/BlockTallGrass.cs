using BetaSharp.Items;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockTallGrass : BlockPlant
{
    private const float HalfSize = 0.4F;

    public BlockTallGrass(int i, int j) : base(i, j) => setBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, 0.8F, 0.5F + HalfSize);

    public override int GetTexture(Side side, int meta) => meta switch
    {
        1 => TextureId,
        2 => TextureId + 16 + 1,
        0 => TextureId + 16,
        _ => TextureId
    };

    public override int getColor(int meta) => meta == 0 ? 0xFFFFFF : GrassColors.getDefaultColor();

    public override int getColorMultiplier(IBlockReader iBlockReader, int x, int y, int z)
    {
        int meta = iBlockReader.GetBlockMeta(x, y, z);
        if (meta == 0) return 0xFFFFFF;

        long positionSeed = x * 3129871 + z * 6129781 + y;
        positionSeed = positionSeed * positionSeed * 42317861L + positionSeed * 11L;
        x = (int)(x + ((positionSeed >> 14) & 31L));
        y = (int)(y + ((positionSeed >> 19) & 31L));
        z = (int)(z + ((positionSeed >> 24) & 31L));
        iBlockReader.GetBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = iBlockReader.GetBiomeSource().TemperatureMap[0];
        double downfall = iBlockReader.GetBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override int getColorMultiplier(IBlockReader iBlockReader, int x, int y, int z, int knownMeta)
    {
        if (knownMeta == 0) return 0xFFFFFF;

        long positionSeed = x * 3129871 + z * 6129781 + y;
        positionSeed = positionSeed * positionSeed * 42317861L + positionSeed * 11L;
        int bx = (int)(x + ((positionSeed >> 14) & 31L));
        int by = (int)(y + ((positionSeed >> 19) & 31L));
        int bz = (int)(z + ((positionSeed >> 24) & 31L));
        iBlockReader.GetBiomeSource().GetBiomesInArea(bx, bz, 1, 1);
        double temperature = iBlockReader.GetBiomeSource().TemperatureMap[0];
        double downfall = iBlockReader.GetBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override int getDroppedItemId(int blockMeta) => Random.Shared.Next(8) == 0 ? Item.Seeds.id : -1;
}
