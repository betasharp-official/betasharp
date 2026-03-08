using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Blocks;

internal class BlockSapling : BlockPlant
{
    public BlockSapling(int i, int j) : base(i, j)
    {
        float halfSize = 0.4F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
    }

    public override void onTick(WorldBlockView worldView, int x, int y, int z, JavaRandom random, WorldEventBroadcaster broadcaster, bool isRemote)
    {
        if (!isRemote)
        {
            base.onTick(worldView, x, y, z, random, broadcaster, isRemote);
            if (worldView.getLightLevel(x, y + 1, z) >= 9 && random.NextInt(30) == 0)
            {
                int saplingMeta = worldView.getBlockMeta(x, y, z);
                if ((saplingMeta & 8) == 0)
                {
                    worldView.setBlockMeta(x, y, z, saplingMeta | 8);
                }
                else
                {
                    generate(worldView, x, y, z, random);
                }
            }
        }
    }

    public override int getTexture(int side, int meta)
    {
        meta &= 3;
        return meta == 1 ? 63 : meta == 2 ? 79 : base.getTexture(side, meta);
    }

    public void generate(World world, int x, int y, int z, JavaRandom random)
    {
        int saplingType = world.getBlockMeta(x, y, z) & 3;
        world.setBlockWithoutNotifyingNeighbors(x, y, z, 0);
        object treeFeature = null;
        if (saplingType == 1)
        {
            treeFeature = new SpruceTreeFeature();
        }
        else if (saplingType == 2)
        {
            treeFeature = new BirchTreeFeature();
        }
        else
        {
            treeFeature = new OakTreeFeature();
            if (random.NextInt(10) == 0)
            {
                treeFeature = new LargeOakTreeFeature();
            }
        }

        if (!((Feature)treeFeature).Generate(world, random, x, y, z))
        {
            world.setBlockWithoutNotifyingNeighbors(x, y, z, id, saplingType);
        }
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta & 3;
}
