using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLever : Block
{
    public BlockLever(int id, int level) : base(id, level, Material.PistonBreakable)
    {
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Lever;

    public bool canPlaceAt(IBlockReader world, int x, int y, int z, int side) =>
        (side == 1 && world.ShouldSuffocate(x, y - 1, z)) ||
        (side == 2 && world.ShouldSuffocate(x, y, z + 1)) ||
        (side == 3 && world.ShouldSuffocate(x, y, z - 1)) ||
        (side == 4 && world.ShouldSuffocate(x + 1, y, z)) ||
        (side == 5 && world.ShouldSuffocate(x - 1, y, z));

    public override bool canPlaceAt(CanPlaceAtContext context) =>
        context.World.Reader.ShouldSuffocate(context.X - 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X + 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z - 1) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z + 1) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    public override void onPlaced(OnPlacedEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        int powered = meta & 8;
        meta &= 7;
        meta = -1;

        if (@event.Direction == 1 && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z))
        {
            meta = 5 + Random.Shared.Next(2);
        }
        else if (@event.Direction == 2 && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
        {
            meta = 4;
        }
        else if (@event.Direction == 3 && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
        {
            meta = 3;
        }
        else if (@event.Direction == 4 && @event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
        {
            meta = 2;
        }
        else if (@event.Direction == 5 && @event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
        {
            meta = 1;
        }
        else
        {
            if (@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z)) meta = 1;
            else if (@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z)) meta = 2;
            else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1)) meta = 3;
            else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1)) meta = 4;
            else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z)) meta = 5 + Random.Shared.Next(2);
        }

        if (meta == -1)
        {
            dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta + powered);
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (breakIfCannotPlaceAt(@event))
        {
            int direction = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) & 7;
            bool shouldDrop = false;

            if (!@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z) && direction == 1)
            {
                shouldDrop = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z) && direction == 2)
            {
                shouldDrop = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1) && direction == 3)
            {
                shouldDrop = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1) && direction == 4)
            {
                shouldDrop = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) && direction == 5)
            {
                shouldDrop = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) && direction == 6)
            {
                shouldDrop = true;
            }

            if (shouldDrop)
            {
                dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }
        }
    }

    private bool breakIfCannotPlaceAt(OnTickEvent ctx)
    {
        if (!canPlaceAt(new CanPlaceAtContext(ctx.World, 0, ctx.X, ctx.Y, ctx.Z)))
        {
            dropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z)));
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            return false;
        }

        return true;
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z) & 7;
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

    public override void onBlockBreakStart(OnBlockBreakStartEvent ctx) => toggleLever(ctx.World, ctx.X, ctx.Y, ctx.Z);

    public override bool onUse(OnUseEvent ctx)
    {
        if (ctx.World.IsRemote)
        {
            return true;
        }

        toggleLever(ctx.World, ctx.X, ctx.Y, ctx.Z);
        return true;
    }

    private void toggleLever(IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        int direction = meta & 7;
        int powered = 8 - (meta & 8);

        world.Writer.SetBlockMeta(x, y, z, direction + powered);
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

    public override void onBreak(OnBreakEvent ctx)
    {
        int meta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta & 8) > 0)
        {
            ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
            int direction = meta & 7;

            if (direction == 1)
            {
                ctx.World.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
            }
            else if (direction == 2)
            {
                ctx.World.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
            }
            else if (direction == 3)
            {
                ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
            }
            else if (direction == 4)
            {
                ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
            }
            else
            {
                ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
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
