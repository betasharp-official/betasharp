using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Blocks;

internal class BlockSapling : BlockPlant
{
    private static readonly JavaRandom s_random = new ();
    private const float HalfSize = 0.4F;

    public BlockSapling(int id) : base(id, BlockTextures.SaplingOak)
    {
        setBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, HalfSize * 2.0F, 0.5F + HalfSize);
    }

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        base.onTick(@event);
        if (@event.World.Reader.GetBrightness(@event.X, @event.Y + 1, @event.Z) < 9 || Random.Shared.Next(30) != 0) return;

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

    public override int GetTexture(Side side, int meta)
    {
        meta &= 3;
        return meta switch
        {
            1 => BlockTextures.SaplingPine,
            2 => BlockTextures.SaplingBirch,
            _ => BlockTextures.SaplingOak
        };
    }

    public void generate(IWorldContext world, int x, int y, int z)
    {
        int saplingType = world.Reader.GetBlockMeta(x, y, z) & 3;
        world.Writer.SetBlock(x, y, z, 0);
        Feature? treeFeature = null; 
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

        if (!treeFeature!.Generate(world, s_random, x, y, z))
        {
            world.Writer.SetBlock(x, y, z, id, saplingType);
        }
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta & 3;
}
