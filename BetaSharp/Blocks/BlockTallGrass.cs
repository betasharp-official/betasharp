using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Colors;

namespace BetaSharp.Blocks;

public class BlockTallGrass : BlockPlant
{
    public BlockTallGrass(int i, int j) : base(i, j)
    {
        float halfSize = 0.4F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, 0.8F, 0.5F + halfSize);
    }

    public override int getTexture(int side, int meta)
    {
        return meta == 1 ? textureId : (meta == 2 ? textureId + 16 + 1 : (meta == 0 ? textureId + 16 : textureId));
    }

    public override int getColor(int meta)
    {
        return meta == 0 ? 0xFFFFFF : GrassColors.getDefaultColor();
    }

    public override int getColorMultiplier(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int meta = iBlockAccess.getBlockMeta(x, y, z);
        if (meta == 0)
        {
            return 0xFFFFFF;
        }
        else
        {
            long positionSeed = (long)(x * 3129871 + z * 6129781 + y);
            positionSeed = positionSeed * positionSeed * 42317861L + positionSeed * 11L;
            x = (int)((long)x + (positionSeed >> 14 & 31L));
            y = (int)((long)y + (positionSeed >> 19 & 31L));
            z = (int)((long)z + (positionSeed >> 24 & 31L));
            iBlockAccess.getBiomeSource().GetBiomesInArea(x, z, 1, 1);
            double temperature = iBlockAccess.getBiomeSource().TemperatureMap[0];
            double downfall = iBlockAccess.getBiomeSource().DownfallMap[0];
            return GrassColors.getColor(temperature, downfall);
        }
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return random.NextInt(8) == 0 ? Item.Seeds.id : -1;
    }
}
