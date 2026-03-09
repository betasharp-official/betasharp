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

    public override void onTick(OnTickEvt evt)
    {
        if (!evt.Level.IsRemote)
        {
            base.onTick(evt);
            if (evt.Level.BlocksReader.GetBrightness(evt.X, evt.Y + 1, evt.Z) >= 9 && Random.Shared.Next(30) == 0)
            {
                int saplingMeta = evt.Level.BlocksReader.GetBlockMeta(evt.X, evt.Y, evt.Z);
                if ((saplingMeta & 8) == 0)
                {
                    evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, saplingMeta | 8);
                }
                else
                {
                    generate(evt.Level, evt.X, evt.Y, evt.Z);
                }
            }
        }
    }

    public override int getTexture(int side, int meta)
    {
        meta &= 3;
        return meta == 1 ? 63 : meta == 2 ? 79 : base.getTexture(side, meta);
    }

    public void generate(IBlockWorldContext world, int x, int y, int z)
    {
        int saplingType = world.BlocksReader.GetBlockMeta(x, y, z) & 3;
        world.BlockWriter.SetBlock(x, y, z, 0);
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
            if (Random.Shared.Next(10) == 0)
            {
                treeFeature = new LargeOakTreeFeature();
            }
        }

        if (!((Feature)treeFeature).Generate(world, x, y, z))
        {
            world.BlockWriter.SetBlock(x, y, z, id, saplingType);
        }
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta & 3;
}
