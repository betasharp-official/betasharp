using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRedstoneRepeater : Block
{
    public static readonly float[] RenderOffset = [-0.0625f, 1.0f / 16.0f, 0.1875f, 0.3125f];
    private static readonly int[] s_delay = [1, 2, 3, 4];
    private readonly bool _lit;

    public BlockRedstoneRepeater(int id, bool lit) : base(id, 6, Material.PistonBreakable)
    {
        _lit = lit;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
    }

    public override bool IsFullCube() => false;

    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z) && base.CanPlaceAt(context);

    public override bool CanGrow(OnTickEvent @event) => @event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) && base.CanGrow(@event);

    public override void OnTick(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        bool powered = IsPowered(@event.World.Reader, @event.World.Redstone, @event.X, @event.Y, @event.Z, meta);

        switch (_lit)
        {
            case true when !powered:
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Repeater.ID, meta);
                break;
            case false:
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, PoweredRepeater.ID, meta);

                    if (!powered)
                    {
                        int delaySetting = (meta & 12) >> 2;
                        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, PoweredRepeater.ID, s_delay[delaySetting] * 2);
                    }

                    break;
                }
        }
    }

    public override int GetTexture(Side side, int meta) => side switch
    {
        Side.Down => _lit ? BlockTextures.RedstoneTorchLit : BlockTextures.RedstoneTorchUnlit,
        Side.Up => _lit ? BlockTextures.RepeaterTopLit : BlockTextures.RepeaterTopUnlit,
        _ => BlockTextures.StoneSlabSide
    };

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side) => side != 0 && side != Side.Up;

    public override BlockRendererType GetRenderType() => BlockRendererType.Repeater;

    public override int GetTexture(Side side) => GetTexture(side, 0);

    public override bool IsStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => IsPoweringSide(world, x, y, z, side);

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        if (!_lit)
        {
            return false;
        }

        int facing = reader.GetBlockMeta(x, y, z) & 3;
        return (facing == 0 && side == 3) ||
               (facing == 1 && side == 4) ||
               (facing == 2 && side == 2) ||
               (facing == 3 && side == 5);
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!CanGrow(@event))
        {
            DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.Meta, @event.BlockId));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            bool powered = IsPowered(@event.World.Reader, @event.World.Redstone, @event.X, @event.Y, @event.Z, meta);
            int delaySetting = (meta & 12) >> 2;
            if ((_lit && !powered) || (!_lit && powered))
            {
                @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, ID, s_delay[delaySetting] * 2);
            }
        }
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        base.OnBreak(@event);
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        NotifyTargetNeighbors(@event.World, @event.X, @event.Y, @event.Z, meta);
    }

    private static bool IsPowered(IBlockReader world, RedstoneEngine redstoneEngine, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        return facing switch
        {
            0 => redstoneEngine.IsPoweringSide(x, y, z + 1, 3) || (world.GetBlockId(x, y, z + 1) == RedstoneWire.ID && world.GetBlockMeta(x, y, z + 1) > 0),
            1 => redstoneEngine.IsPoweringSide(x - 1, y, z, 4) || (world.GetBlockId(x - 1, y, z) == RedstoneWire.ID && world.GetBlockMeta(x - 1, y, z) > 0),
            2 => redstoneEngine.IsPoweringSide(x, y, z - 1, 2) || (world.GetBlockId(x, y, z - 1) == RedstoneWire.ID && world.GetBlockMeta(x, y, z - 1) > 0),
            3 => redstoneEngine.IsPoweringSide(x + 1, y, z, 5) || (world.GetBlockId(x + 1, y, z) == RedstoneWire.ID && world.GetBlockMeta(x + 1, y, z) > 0),
            _ => false
        };
    }

    public override bool OnUse(OnUseEvent ctx)
    {
        int meta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int newDelaySetting = (meta & 12) >> 2;
        newDelaySetting = ((newDelaySetting + 1) << 2) & 12;
        ctx.World.Writer.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, newDelaySetting | (meta & 3));
        return true;
    }

    public override bool CanEmitRedstonePower() => true;

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer != null)
        {
            float yaw = @event.Placer.Yaw;
            int facing = ((MathHelper.Floor(yaw * 4.0F / 360.0F + 0.5D) & 3) + 2) % 4;
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing);
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);

        bool powered = IsPowered(@event.World.Reader, @event.World.Redstone, @event.X, @event.Y, @event.Z, meta);
        if (powered)
        {
            @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, ID, 1);
        }

        @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, ID);

        if (!@event.World.IsRemote)
        {
            NotifyTargetNeighbors(@event.World, @event.X, @event.Y, @event.Z, meta);
        }
    }

    private void NotifyTargetNeighbors(IWorldContext ctx, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        int targetX = x;
        int targetZ = z;

        switch (facing)
        {
            case 0: targetZ--; break;
            case 1: targetX++; break;
            case 2: targetZ++; break;
            case 3: targetX--; break;
        }

        ctx.Broadcaster.NotifyNeighbors(targetX, y, targetZ, ID);
    }

    public override bool IsOpaque() => false;

    public override int GetDroppedItemId(int blockMeta) => Item.Repeater.id;

    public override void RandomDisplayTick(OnTickEvent ctx)
    {
        if (!_lit)
        {
            return;
        }

        int meta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        double particleX = ctx.X + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleY = ctx.Y + 0.4F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleZ = ctx.Z + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double offsetX = 0.0D;
        double offsetY = 0.0D;
        if (Random.Shared.Next(2) == 0)
        {
            switch (meta & 3)
            {
                case 0:
                    offsetY = -0.3125D;
                    break;
                case 1:
                    offsetX = 0.3125D;
                    break;
                case 2:
                    offsetY = 0.3125D;
                    break;
                case 3:
                    offsetX = -0.3125D;
                    break;
            }
        }
        else
        {
            int delayIndex = (meta & 12) >> 2;
            switch (meta & 3)
            {
                case 0:
                    offsetY = RenderOffset[delayIndex];
                    break;
                case 1:
                    offsetX = -RenderOffset[delayIndex];
                    break;
                case 2:
                    offsetY = -RenderOffset[delayIndex];
                    break;
                case 3:
                    offsetX = RenderOffset[delayIndex];
                    break;
            }
        }

        ctx.World.Broadcaster.AddParticle("reddust", particleX + offsetX, particleY, particleZ + offsetY, 0.0D, 0.0D, 0.0D);
    }
}
