using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockSnowBlock : Block
{
    public BlockSnowBlock(int id, int textureId) : base(id, textureId, Material.SnowBlock) => setTickRandomly(true);

    public override int getDroppedItemId(int blockMeta) => Item.Snowball.id;

    public override int getDroppedItemCount() => 4;

    public override void onTick(OnTickEvt ctx)
    {
        if (ctx.Lighting.GetBrightness(LightType.Block, ctx.X, ctx.Y, ctx.Z) > 11)
        {
            dropStacks(worldView, x, y, z, worldView.getBlockMeta(x, y, z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }
}
