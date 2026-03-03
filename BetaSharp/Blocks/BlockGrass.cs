using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Colors;

namespace BetaSharp.Blocks;

public class BlockGrass : Block
{
    public BlockGrass(int id) : base(id, Material.SolidOrganic)
    {
        textureId = 3;
        setTickRandomly(true);
    }

    public override int getTexture(int side)
    {
        if (side == 1) return 0;  // top: grass green
        if (side == 0) return 2;  // bottom: dirt
        return 3;                  // sides: grass+dirt edge
    }

    public override int getColorForFace(int meta, int face)
    {
        return face == 1 ? GrassColors.getDefaultColor() : 0xFFFFFF;
    }

    public override int getTextureId(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return 0;
        }
        else if (side == 0)
        {
            return 2;
        }
        else
        {
            Material materialAbove = iBlockAccess.getMaterial(x, y + 1, z);
            return materialAbove != Material.SnowLayer && materialAbove != Material.SnowBlock ? 3 : 68;
        }
    }

    public override int getColorMultiplier(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        iBlockAccess.getBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = iBlockAccess.getBiomeSource().TemperatureMap[0];
        double downfall = iBlockAccess.getBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (!world.isRemote)
        {
            if (world.getLightLevel(x, y + 1, z) < 4 && Block.BlockLightOpacity[world.getBlockId(x, y + 1, z)] > 2)
            {
                if (random.NextInt(4) != 0)
                {
                    return;
                }

                world.setBlock(x, y, z, Block.Dirt.id);
            }
            else if (world.getLightLevel(x, y + 1, z) >= 9)
            {
                int spreadX = x + random.NextInt(3) - 1;
                int spreadY = y + random.NextInt(5) - 3;
                int spreadZ = z + random.NextInt(3) - 1;
                int blockAboveId = world.getBlockId(spreadX, spreadY + 1, spreadZ);
                if (world.getBlockId(spreadX, spreadY, spreadZ) == Block.Dirt.id && world.getLightLevel(spreadX, spreadY + 1, spreadZ) >= 4 && Block.BlockLightOpacity[blockAboveId] <= 2)
                {
                    world.setBlock(spreadX, spreadY, spreadZ, Block.GrassBlock.id);
                }
            }
        }
    }

    public override int getDroppedItemId(int blocKMeta, JavaRandom random)
    {
        return Block.Dirt.getDroppedItemId(0, random);
    }
}
