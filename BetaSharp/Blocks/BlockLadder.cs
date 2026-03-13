using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

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
        ctx.Level.Reader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) ? true :
        ctx.Level.Reader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) ? true :
        ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) ? true : ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1);

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int meta = ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta == 0 || ctx.Direction == 2) && ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            meta = 2;
        }

        if ((meta == 0 || ctx.Direction == 3) && ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            meta = 3;
        }

        if ((meta == 0 || ctx.Direction == 4) && ctx.Level.Reader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            meta = 4;
        }

        if ((meta == 0 || ctx.Direction == 5) && ctx.Level.Reader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            meta = 5;
        }

        ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        int meta = ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        bool hasSupport = false;
        if (meta == 2 && ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            hasSupport = true;
        }

        if (meta == 3 && ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            hasSupport = true;
        }

        if (meta == 4 && ctx.Level.Reader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            hasSupport = true;
        }

        if (meta == 5 && ctx.Level.Reader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            hasSupport = true;
        }

        if (!hasSupport)
        {
            dropStacks(new OnDropEvt(ctx.Level, ctx.X, ctx.Y, ctx.Z, meta));
            ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

        base.neighborUpdate(ctx);
    }

    public override int getDroppedItemCount() => 1;
}
