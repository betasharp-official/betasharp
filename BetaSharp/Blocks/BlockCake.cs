using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockCake : Block
{
    public BlockCake(int id, int textureId) : base(id, textureId, Material.Cake) => setTickRandomly(true);

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int slicesEaten = iBlockReader.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (1 + slicesEaten * 2) / 16.0F;
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
        float minX = (1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box(x + minX, y, z + edgeInset, x + 1 - edgeInset, y + height - edgeInset, z + 1 - edgeInset);
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        int slicesEaten = world.getBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box(x + minX, y, z + edgeInset, x + 1 - edgeInset, y + height, z + 1 - edgeInset);
    }

    public override int getTexture(int side, int meta) => side == 1 ? textureId : side == 0 ? textureId + 3 : meta > 0 && side == 4 ? textureId + 2 : textureId + 1;

    public override int getTexture(int side) => side == 1 ? textureId : side == 0 ? textureId + 3 : textureId + 1;

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.Player.health < 20)
        {
            ctx.Player.heal(3);
            int slicesEaten = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) + 1;
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

    public override void onBlockBreakStart(OnBlockBreakStartEvt ctx)
    {
        if (ctx.Player.health < 20)
        {
            ctx.Player.heal(3);
            int slicesEaten = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) + 1;
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

    public override bool canPlaceAt(OnPlacedEvt ctx) => !base.canPlaceAt(ctx) ? false : canGrow(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (!canGrow(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
        {
            dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    public override bool canGrow(OnTickEvt ctx) => canGrow(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);

    private static bool canGrow(IBlockReader world, int x, int y, int z) => world.GetMaterial(x, y - 1, z).IsSolid;

    public override int getDroppedItemCount() => 0;

    public override int getDroppedItemId(int blockMeta) => 0;
}
