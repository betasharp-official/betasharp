using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLever : Block
{
    public BlockLever(int id, int level) : base(id, level, Material.PistonBreakable)
    {
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Lever;

    // Converted nested ternaries to clean boolean logic
    public bool canPlaceAt(IBlockReader world, int x, int y, int z, int side) =>
        (side == 1 && world.ShouldSuffocate(x, y - 1, z)) ||
        (side == 2 && world.ShouldSuffocate(x, y, z + 1)) ||
        (side == 3 && world.ShouldSuffocate(x, y, z - 1)) ||
        (side == 4 && world.ShouldSuffocate(x + 1, y, z)) ||
        (side == 5 && world.ShouldSuffocate(x - 1, y, z));

    public override bool canPlaceAt(CanPlaceAtCtx ctx) =>
        ctx.Level.Reader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) ||
        ctx.Level.Reader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) ||
        ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) ||
        ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) ||
        ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z);

    public override void onPlaced(OnPlacedEvt evt)
    {
        int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        int powered = meta & 8;
        meta &= 7;
        meta = -1;

        if (evt.Direction == 1 && evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z))
        {
            meta = 5 + Random.Shared.Next(2);
        }
        else if (evt.Direction == 2 && evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1))
        {
            meta = 4;
        }
        else if (evt.Direction == 3 && evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1))
        {
            meta = 3;
        }
        else if (evt.Direction == 4 && evt.Level.Reader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z))
        {
            meta = 2;
        }
        else if (evt.Direction == 5 && evt.Level.Reader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z))
        {
            meta = 1;
        }
        else
        {
            if (evt.Level.Reader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z)) meta = 1;
            else if (evt.Level.Reader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z)) meta = 2;
            else if (evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1)) meta = 3;
            else if (evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1)) meta = 4;
            else if (evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z)) meta = 5 + Random.Shared.Next(2);
        }

        if (meta == -1)
        {
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }
        else
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, meta + powered);
        }
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        if (breakIfCannotPlaceAt(evt))
        {
            int direction = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z) & 7;
            bool shouldDrop = false;

            if (!evt.Level.Reader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z) && direction == 1)
            {
                shouldDrop = true;
            }

            if (!evt.Level.Reader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z) && direction == 2)
            {
                shouldDrop = true;
            }

            if (!evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1) && direction == 3)
            {
                shouldDrop = true;
            }

            if (!evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1) && direction == 4)
            {
                shouldDrop = true;
            }

            if (!evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) && direction == 5)
            {
                shouldDrop = true;
            }

            if (!evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) && direction == 6)
            {
                shouldDrop = true;
            }

            if (shouldDrop)
            {
                dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
                evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
            }
        }
    }

    private bool breakIfCannotPlaceAt(OnTickEvt ctx)
    {
        if (!canPlaceAt(new CanPlaceAtCtx(ctx.Level, 0, ctx.X, ctx.Y, ctx.Z)))
        {
            dropStacks(new OnDropEvt(ctx.Level, ctx.X, ctx.Y, ctx.Z, ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z)));
            ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            return false;
        }

        return true;
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int meta = iBlockReader.GetBlockMeta(x, y, z) & 7;
        float width = 3.0F / 16.0F;

        if (meta == 1)
        {
            setBoundingBox(0.0F, 0.2F, 0.5F - width, width * 2.0F, 0.8F, 0.5F + width);
        }
        else if (meta == 2)
        {
            setBoundingBox(1.0F - width * 2.0F, 0.2F, 0.5F - width, 1.0F, 0.8F, 0.5F + width);
        }
        else if (meta == 3)
        {
            setBoundingBox(0.5F - width, 0.2F, 0.0F, 0.5F + width, 0.8F, width * 2.0F);
        }
        else if (meta == 4)
        {
            setBoundingBox(0.5F - width, 0.2F, 1.0F - width * 2.0F, 0.5F + width, 0.8F, 1.0F);
        }
        else
        {
            width = 0.25F;
            setBoundingBox(0.5F - width, 0.0F, 0.5F - width, 0.5F + width, 0.6F, 0.5F + width);
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvt ctx) => toggleLever(ctx.Level, ctx.X, ctx.Y, ctx.Z);

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.Level.IsRemote)
        {
            return true;
        }

        toggleLever(ctx.Level, ctx.X, ctx.Y, ctx.Z);
        return true;
    }

    private void toggleLever(IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        int direction = meta & 7;
        int powered = 8 - (meta & 8);

        world.BlockWriter.SetBlockMeta(x, y, z, direction + powered);
        world.Broadcaster.SetBlocksDirty(x, y, z);
        world.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "random.click", 0.3F, powered > 0 ? 0.6F : 0.5F);

        world.Broadcaster.NotifyNeighbors(x, y, z, id);

        if (direction == 1)
        {
            world.Broadcaster.NotifyNeighbors(x - 1, y, z, id);
        }
        else if (direction == 2)
        {
            world.Broadcaster.NotifyNeighbors(x + 1, y, z, id);
        }
        else if (direction == 3)
        {
            world.Broadcaster.NotifyNeighbors(x, y, z - 1, id);
        }
        else if (direction == 4)
        {
            world.Broadcaster.NotifyNeighbors(x, y, z + 1, id);
        }
        else
        {
            world.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
        }
    }

    public override void onBreak(OnBreakEvt ctx)
    {
        int meta = ctx.Level.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta & 8) > 0)
        {
            ctx.Level.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
            int direction = meta & 7;

            if (direction == 1)
            {
                ctx.Level.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
            }
            else if (direction == 2)
            {
                ctx.Level.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
            }
            else if (direction == 3)
            {
                ctx.Level.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
            }
            else if (direction == 4)
            {
                ctx.Level.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
            }
            else
            {
                ctx.Level.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            }
        }

        base.onBreak(ctx);
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) =>
        (iBlockReader.GetBlockMeta(x, y, z) & 8) > 0;

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        int meta = world.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int direction = meta & 7;
        return (direction == 6 && side == 1) ||
               (direction == 5 && side == 1) ||
               (direction == 4 && side == 2) ||
               (direction == 3 && side == 3) ||
               (direction == 2 && side == 4) ||
               (direction == 1 && side == 5);
    }

    public override bool canEmitRedstonePower() => true;
}
