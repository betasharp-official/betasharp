using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockIce : BlockBreakable
{

    public BlockIce(int id, int textureId) : base(id, textureId, Material.Ice, false)
    {
        slipperiness = 0.98F;
        setTickRandomly(true);
    }

    public override int getRenderLayer()
    {
        return 1;
    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        return base.isSideVisible(iBlockReader, x, y, z, 1 - side);
    }

    public override void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        base.afterBreak(world, player, x, y, z, meta);
        Material materialBelow = world.getMaterial(x, y - 1, z);
        if (materialBelow.BlocksMovement || materialBelow.IsFluid)
        {
            world.setBlock(x, y, z, Block.FlowingWater.id);
        }

    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override void onTick(OnTickContext ctx)
    {
        if (ctx.Lighting.GetBrightness(LightType.Block, ctx.X, ctx.Y, ctx.Z) > 11 - BlockLightOpacity[id])
        {
            dropStacks(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Block.Water.id);
        }

    }

    public override int getPistonBehavior()
    {
        return 0;
    }
}
