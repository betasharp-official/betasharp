using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockLog : Block
{
    public BlockLog(int id) : base(id, Material.Wood) => textureId = 20;

    public override int getDroppedItemCount() => 1;

    public override int getDroppedItemId(int blockMeta) => Log.id;

    public override void afterBreak(OnAfterBreakEvt ctx) => base.afterBreak(ctx);

    public override void onBreak(OnBreakEvt ctx)
    {
        sbyte searchRadius = 4;
        int regionExtent = searchRadius + 1;
        if (ctx.world.IsRegionLoaded(ctx.X - regionExtent, ctx.Y - regionExtent, ctx.Z - regionExtent, ctx.X + regionExtent, ctx.Y + regionExtent, ctx.Z + regionExtent))
        {
            for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
                {
                    for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                    {
                        int neighborBlockId = ctx.WorldRead.GetBlockId(ctx.X + offsetX, ctx.Y + offsetY, ctx.Z + offsetZ);
                        if (neighborBlockId == Leaves.id)
                        {
                            int leavesMeta = ctx.WorldRead.GetBlockMeta(ctx.X + offsetX, ctx.Y + offsetY, ctx.Z + offsetZ);
                            if ((leavesMeta & 8) == 0)
                            {
                                ctx.WorldWrite.SetBlockMetaWithoutNotifyingNeighbors(ctx.X + offsetX, ctx.Y + offsetY, ctx.Z + offsetZ, leavesMeta | 8);
                            }
                        }
                    }
                }
            }
        }
    }

    public override int getTexture(int side, int meta) => side == 1 ? 21 : side == 0 ? 21 : meta == 1 ? 116 : meta == 2 ? 117 : 20;

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta;
}
