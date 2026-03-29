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
        const float halfSize = 0.4F;
        SetBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        base.OnTick(@event);

        if (@event.World.Reader.GetBrightness(@event.X, @event.Y + 1, @event.Z) < 9 || Random.Shared.Next(30) != 0) return;

        int saplingMeta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((saplingMeta & 8) == 0)
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, saplingMeta | 8);
        }
        else
        {
            Generate(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    public override int GetTexture(Side side, int meta)
    {
        meta &= 3;
        return meta switch
        {
            1 => 63,
            2 => 79,
            _ => base.GetTexture(side, meta)
        };
    }

    public void Generate(IWorldContext world, int x, int y, int z)
    {
        int saplingType = world.Reader.GetBlockMeta(x, y, z) & 3;
        world.Writer.SetBlock(x, y, z, 0);
        Feature? treeFeature;
        switch (saplingType)
        {
            case 1:
                treeFeature = new SpruceTreeFeature();
                break;
            case 2:
                treeFeature = new BirchTreeFeature();
                break;
            default:
                {
                    treeFeature = new OakTreeFeature();
                    if (Random.Shared.Next(10) == 0)
                    {
                        treeFeature = new LargeOakTreeFeature();
                    }

                    break;
                }
        }

        if (!treeFeature.Generate(world, _random, x, y, z))
        {
            world.Writer.SetBlock(x, y, z, Id, saplingType);
        }
    }

    protected override int GetDroppedItemMeta(int blockMeta) => blockMeta & 3;
}
