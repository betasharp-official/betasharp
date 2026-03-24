using BetaSharp.Items;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockTallGrass : BlockPlant
{
    public BlockTallGrass(int i, int j) : base(i, j)
    {
        const float halfSize = 0.4F;
        SetBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, 0.8F, 0.5F + halfSize);
    }

    public override int GetTexture(Side side, int meta) => meta == 1 ? TextureId : meta == 2 ? TextureId + 16 + 1 : meta == 0 ? TextureId + 16 : TextureId;

    public override int GetColor(int meta) => meta == 0 ? 0xFFFFFF : GrassColors.getDefaultColor();

    public override int GetColorMultiplier(IBlockReader reader, int x, int y, int z)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        if (meta == 0) return 0xFFFFFF;

        long positionSeed = x * 3129871 + z * 6129781 + y;
        positionSeed = positionSeed * positionSeed * 42317861L + positionSeed * 11L;
        x = (int)(x + ((positionSeed >> 14) & 31L));
        z = (int)(z + ((positionSeed >> 24) & 31L));
        reader.GetBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = reader.GetBiomeSource().TemperatureMap[0];
        double downfall = reader.GetBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override int GetColorMultiplier(IBlockReader reader, int x, int y, int z, int knownMeta)
    {
        if (knownMeta == 0) return 0xFFFFFF;

        long positionSeed = x * 3129871 + z * 6129781 + y;
        positionSeed = positionSeed * positionSeed * 42317861L + positionSeed * 11L;
        int bx = (int)(x + ((positionSeed >> 14) & 31L));
        int by = (int)(y + ((positionSeed >> 19) & 31L));
        int bz = (int)(z + ((positionSeed >> 24) & 31L));
        reader.GetBiomeSource().GetBiomesInArea(bx, bz, 1, 1);
        double temperature = reader.GetBiomeSource().TemperatureMap[0];
        double downfall = reader.GetBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override int GetDroppedItemId(int blockMeta) => Random.Shared.Next(8) == 0 ? Item.Seeds.id : -1;
}
