using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockReed : Block
{

    public BlockReed(int id, int textureId) : base(id, Material.Plant)
    {
        base.textureId = textureId;
        float halfWidth = 6.0F / 16.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, 1.0F, 0.5F + halfWidth);
        setTickRandomly(true);
    }

    public override void onTick(OnTickContext ctx)
    {
        if (ctx.WorldView.IsAir(ctx.X, ctx.Y + 1, ctx.Z))
        {
            int heightBelow;
            for (heightBelow = 1; ctx.WorldView.GetBlockId(ctx.X, ctx.Y - heightBelow, ctx.Z) == id; ++heightBelow)
            {
            }

            if (heightBelow < 3)
            {
                int meta = ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
                if (meta == 15)
                {
                    ctx.WorldView.setBlock(ctx.X, ctx.Y + 1, ctx.Z, id);
                    ctx.WorldView.setBlockMeta(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else
                {
                    ctx.WorldView.setBlockMeta(ctx.X, ctx.Y, ctx.Z, meta + 1);
                }
            }
        }

    }

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z)
    {
        int blockBelowId = world.GetBlockId(x, y - 1, z);
        return blockBelowId == id ? true : (blockBelowId != Block.GrassBlock.id && blockBelowId != Block.Dirt.id ? false : (world.getMaterial(x - 1, y - 1, z) == Material.Water ? true : (world.getMaterial(x + 1, y - 1, z) == Material.Water ? true : (world.getMaterial(x, y - 1, z - 1) == Material.Water ? true : world.getMaterial(x, y - 1, z + 1) == Material.Water))));
    }

    public override void neighborUpdate(OnTickContext ctx)
    {
        breakIfCannotGrow(ctx);
    }

    protected void breakIfCannotGrow(OnTickContext ctx)
    {
        if (!canGrow(ctx))
        {
            dropStacks(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldView.setBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

    }

    public override bool canGrow(OnTickContext ctx)
    {
        return canPlaceAt(ctx.WorldView, ctx.X, ctx.Y, ctx.Z);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        return null;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.SugarCane.id;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Reed;
    }
}
