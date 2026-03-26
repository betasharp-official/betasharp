using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Blocks;

internal class BlockSapling : BlockPlant
{
    private readonly JavaRandom _random;

    public BlockSapling(int i, int j) : base(i, j)
    {
        _random = new JavaRandom();
        float halfSize = 0.4F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
    }

    public override void onTick(OnTickEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            base.onTick(@event);
            if (@event.World.Reader.GetBrightness(@event.X, @event.Y + 1, @event.Z) >= 9 && Random.Shared.Next(30) == 0)
            {
                int saplingMeta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
                if ((saplingMeta & 8) == 0)
                {
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, saplingMeta | 8);
                }
                else
                {
                    generate(@event.World, @event.X, @event.Y, @event.Z);
                }
            }
        }
    }

    public override int getTexture(int side, int meta)
    {
        meta &= 3;
        return meta == 1 ? 63 : meta == 2 ? 79 : base.getTexture(side, meta);
    }

    public void generate(IWorldContext world, int x, int y, int z)
    {
        int saplingType = world.Reader.GetBlockMeta(x, y, z) & 3;
        world.Writer.SetBlock(x, y, z, 0);
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

        if (!((Feature)treeFeature).Generate(world, _random, x, y, z))
        {
            world.Writer.SetBlock(x, y, z, id, saplingType);
        }
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta & 3;
}
