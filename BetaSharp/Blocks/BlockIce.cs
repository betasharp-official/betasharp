using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockIce : BlockBreakable
{
    public BlockIce(int id, int textureId) : base(id, textureId, Material.Ice, false)
    {
        slipperiness = 0.98F;
        setTickRandomly(true);
    }

    public override int getRenderLayer() => 1;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side) => base.isSideVisible(iBlockReader, x, y, z, 1 - side);

    public override void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        base.afterBreak(world, player, x, y, z, meta);
        Material materialBelow = world.getMaterial(x, y - 1, z);
        if (materialBelow.BlocksMovement || materialBelow.IsFluid)
        {
            world.setBlock(x, y, z, FlowingWater.id);
        }
    }

    public override int getDroppedItemCount() => 0;

    public override void onTick(OnTickEvt ctx)
    {
        if (ctx.Lighting.GetBrightness(LightType.Block, ctx.X, ctx.Y, ctx.Z) > 11 - BlockLightOpacity[id])
        {
            dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.getBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Water.id);
        }
    }

    public override int getPistonBehavior() => 0;
}
