using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockSnowBlock : Block
{

    public BlockSnowBlock(int id, int textureId) : base(id, textureId, Material.SnowBlock)
    {
        setTickRandomly(true);
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.Snowball.id;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 4;
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (world.GetBrightness(LightType.Block, x, y, z) > 11)
        {
            dropStacks(world, x, y, z, world.GetBlockMeta(x, y, z));
            world.SetBlock(x, y, z, 0);
        }

    }
}
