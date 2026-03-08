using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockReed : Block
{
    public BlockReed(int id, int textureId) : base(id, Material.Plant)
    {
        this.textureId = textureId;
        float halfWidth = 6.0F / 16.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, 1.0F, 0.5F + halfWidth);
        setTickRandomly(true);
    }

    public override void onTick(OnTickEvt ctx)
    {
        if (ctx.WorldRead.IsAir(ctx.X, ctx.Y + 1, ctx.Z))
        {
            int heightBelow;
            for (heightBelow = 1; ctx.WorldRead.GetBlockId(ctx.X, ctx.Y - heightBelow, ctx.Z) == id; ++heightBelow)
            {
            }

            if (heightBelow < 3)
            {
                int meta = ctx.WorldRead.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
                if (meta == 15)
                {
                    ctx.WorldRead.setBlock(ctx.X, ctx.Y + 1, ctx.Z, id);
                    ctx.WorldRead.setBlockMeta(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else
                {
                    ctx.WorldRead.setBlockMeta(ctx.X, ctx.Y, ctx.Z, meta + 1);
                }
            }
        }
    }

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z)
    {
        int blockBelowId = world.GetBlockId(x, y - 1, z);
        return blockBelowId == id ? true :
            blockBelowId != GrassBlock.id && blockBelowId != Dirt.id ? false :
            world.getMaterial(x - 1, y - 1, z) == Material.Water ? true :
            world.getMaterial(x + 1, y - 1, z) == Material.Water ? true :
            world.getMaterial(x, y - 1, z - 1) == Material.Water ? true : world.getMaterial(x, y - 1, z + 1) == Material.Water;
    }

    public override void neighborUpdate(OnTickEvt ctx) => breakIfCannotGrow(ctx);

    protected void breakIfCannotGrow(OnTickEvt ctx)
    {
        if (!canGrow(ctx))
        {
            dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.getBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldRead.setBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    public override bool canGrow(OnTickEvt ctx) => canPlaceAt(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override int getDroppedItemId(int blockMeta) => Item.SugarCane.id;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;
}
