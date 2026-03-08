using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockCake : Block
{

    public BlockCake(int id, int textureId) : base(id, textureId, Material.Cake)
    {
        setTickRandomly(true);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int slicesEaten = iBlockReader.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        setBoundingBox(minX, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override void setupRenderBoundingBox()
    {
        float edgeInset = 1.0F / 16.0F;
        float height = 0.5F;
        setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height - edgeInset), (double)((float)(z + 1) - edgeInset));
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        int slicesEaten = world.getBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height), (double)((float)(z + 1) - edgeInset));
    }

    public override int getTexture(int side, int meta)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : (meta > 0 && side == 4 ? textureId + 2 : textureId + 1));
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : textureId + 1);
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool onUse(OnUseContext ctx)
    {
        if (ctx.Player.health < 20)
        {
            ctx.Player.heal(3);
            int slicesEaten = ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) + 1;
            if (slicesEaten >= 6)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, slicesEaten);
                ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z);
            }
        }
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartContext ctx)
    {
        if (ctx.Player.health < 20)
        {
            ctx.Player.heal(3);
            int slicesEaten = ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) + 1;
            if (slicesEaten >= 6)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, slicesEaten);
                ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z);
            }
        }
    }

    public override bool canPlaceAt(OnPlacedContext ctx)
    {
        return !base.canPlaceAt(ctx) ? false : canGrow(ctx.WorldView, ctx.X, ctx.Y, ctx.Z);
    }

    public override void neighborUpdate(OnTickContext ctx)
    {
        if (!canGrow(ctx.WorldView, ctx.X, ctx.Y, ctx.Z))
        {
            dropStacks(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, ctx.WorldView.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

    }

    public override bool canGrow(OnTickContext ctx)
    {
        return canGrow(ctx.WorldView, ctx.X, ctx.Y, ctx.Z);
    }

    private static bool canGrow(IBlockReader world, int x, int y, int z)
    {
        return world.GetMaterial(x, y - 1, z).IsSolid;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return 0;
    }
}
