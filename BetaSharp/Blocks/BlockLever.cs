using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLever(int id, int level) : Block(id, level, Material.PistonBreakable)
{
    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Lever;

    public override bool CanPlaceAt(CanPlaceAtContext context) =>
        context.World.Reader.ShouldSuffocate(context.X - 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X + 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z - 1) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z + 1) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    public override void OnPlaced(OnPlacedEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        int powered = meta & 8;
        meta = -1;

        switch (@event.Direction)
        {
            case Side.Up when @event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z):
                meta = 5 + Random.Shared.Next(2);
                break;
            case Side.North when @event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1):
                meta = 4;
                break;
            case Side.South when @event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1):
                meta = 3;
                break;
            case Side.West when @event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z):
                meta = 2;
                break;
            default:
                {
                    if ((@event.Direction == Side.East && @event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z)) || @event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
                    {
                        meta = 1;
                    }
                    else if (@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
                    {
                        meta = 2;
                    }
                    else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
                    {
                        meta = 3;
                    }
                    else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
                    {
                        meta = 4;
                    }
                    else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z))
                    {
                        meta = 5 + Random.Shared.Next(2);
                    }

                    break;
                }
        }

        if (meta == -1)
        {
            DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta + powered);
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!breakIfCannotPlaceAt(@event))
        {
            return;
        }

        int direction = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) & 7;
        bool shouldDrop = (!@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z) && direction == 1) ||
                          (!@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z) && direction == 2) ||
                          (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1) && direction == 3) ||
                          (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1) && direction == 4) ||
                          (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) && direction == 5) ||
                          (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) && direction == 6);

        if (!shouldDrop)
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    private bool breakIfCannotPlaceAt(OnTickEvent ctx)
    {
        if (CanPlaceAt(new CanPlaceAtContext(ctx.World, 0, ctx.X, ctx.Y, ctx.Z)))
            return true;

        DropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z)));
        ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        return false;
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z) & 7;
        float width = 3.0F / 16.0F;

        switch (meta)
        {
            case 1:
                SetBoundingBox(0.0F, 0.2F, 0.5F - width, width * 2.0F, 0.8F, 0.5F + width);
                break;
            case 2:
                SetBoundingBox(1.0F - width * 2.0F, 0.2F, 0.5F - width, 1.0F, 0.8F, 0.5F + width);
                break;
            case 3:
                SetBoundingBox(0.5F - width, 0.2F, 0.0F, 0.5F + width, 0.8F, width * 2.0F);
                break;
            case 4:
                SetBoundingBox(0.5F - width, 0.2F, 1.0F - width * 2.0F, 0.5F + width, 0.8F, 1.0F);
                break;
            default:
                width = 0.25F;
                SetBoundingBox(0.5F - width, 0.0F, 0.5F - width, 0.5F + width, 0.6F, 0.5F + width);
                break;
        }
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent ctx) => toggleLever(ctx.World, ctx.X, ctx.Y, ctx.Z);

    public override bool OnUse(OnUseEvent ctx)
    {
        if (ctx.World.IsRemote)
            return true;

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

        world.Broadcaster.NotifyNeighbors(x, y, z, Id);

        switch (direction)
        {
            case 1:
                world.Broadcaster.NotifyNeighbors(x - 1, y, z, Id);
                break;
            case 2:
                world.Broadcaster.NotifyNeighbors(x + 1, y, z, Id);
                break;
            case 3:
                world.Broadcaster.NotifyNeighbors(x, y, z - 1, Id);
                break;
            case 4:
                world.Broadcaster.NotifyNeighbors(x, y, z + 1, Id);
                break;
            default:
                world.Broadcaster.NotifyNeighbors(x, y - 1, z, Id);
                break;
        }
    }

    public override void OnBreak(OnBreakEvent ctx)
    {
        int meta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta & 8) > 0)
        {
            ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, Id);
            int direction = meta & 7;

            switch (direction)
            {
                case 1:
                    ctx.World.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, Id);
                    break;
                case 2:
                    ctx.World.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, Id);
                    break;
                case 3:
                    ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, Id);
                    break;
                case 4:
                    ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, Id);
                    break;
                default:
                    ctx.World.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, Id);
                    break;
            }
        }

        base.OnBreak(ctx);
    }

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side) =>
        (reader.GetBlockMeta(x, y, z) & 8) > 0;

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int direction = meta & 7;
        return (direction == 6 && side == (int)Side.Up) ||
               (direction == 5 && side == (int)Side.Up) ||
               (direction == 4 && side == (int)Side.North) ||
               (direction == 3 && side == (int)Side.South) ||
               (direction == 2 && side == (int)Side.West) ||
               (direction == 1 && side == (int)Side.East);
    }

    public override bool CanEmitRedstonePower() => true;
}
