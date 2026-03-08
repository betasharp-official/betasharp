using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockLadder : Block
{
    public BlockLadder(int id, int textureId) : base(id, textureId, Material.PistonBreakable)
    {
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        int meta = world.GetBlockMeta(x, y, z);
        float thickness = 2.0F / 16.0F;
        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 4)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 5)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }

        return base.getCollisionShape(world, x, y, z);
    }

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        int meta = world.GetBlockMeta(x, y, z);
        float thickness = 2.0F / 16.0F;
        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 4)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 5)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }

        return base.getBoundingBox(world, x, y, z);
    }

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Ladder;

    public override bool canPlaceAt(CanPlaceAtCtx ctx) =>
        ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) ? true : ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) ? true : ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) ? true : ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1);

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta == 0 || ctx.Direction == 2) && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            meta = 2;
        }

        if ((meta == 0 || ctx.Direction == 3) && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            meta = 3;
        }

        if ((meta == 0 || ctx.Direction == 4) && ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            meta = 4;
        }

        if ((meta == 0 || ctx.Direction == 5) && ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            meta = 5;
        }

        ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        bool hasSupport = false;
        if (meta == 2 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            hasSupport = true;
        }

        if (meta == 3 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            hasSupport = true;
        }

        if (meta == 4 && ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            hasSupport = true;
        }

        if (meta == 5 && ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            hasSupport = true;
        }

        if (!hasSupport)
        {
            // TODO: Implement this
            // dropStacks(world, x, y, z, meta);
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

        base.neighborUpdate(ctx);
    }

    public override int getDroppedItemCount() => 1;
}
