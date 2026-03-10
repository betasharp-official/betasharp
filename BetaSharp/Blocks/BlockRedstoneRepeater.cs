using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRedstoneRepeater : Block
{
    public static readonly float[] RenderOffset = [-0.0625f, 1.0f / 16.0f, 0.1875f, 0.3125f];
    private static readonly int[] DELAY = [1, 2, 3, 4];
    private readonly bool lit;

    public BlockRedstoneRepeater(int id, bool lit) : base(id, 6, Material.PistonBreakable)
    {
        this.lit = lit;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
    }

    public override bool isFullCube() => false;

    public override bool canPlaceAt(CanPlaceAtCtx ctx) => !ctx.Level.BlocksReader.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) ? false : base.canPlaceAt(ctx);

    public override bool canGrow(OnTickEvt evt) => !evt.Level.BlocksReader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) ? false : base.canGrow(evt);

    public override void onTick(OnTickEvt evt)
    {
        int meta = evt.Level.BlocksReader.GetMeta(evt.X, evt.Y, evt.Z);
        bool powered = isPowered(evt.Level.BlocksReader, evt.Level.Redstone, evt.X, evt.Y, evt.Z, meta);

        if (lit && !powered)
        {
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, Repeater.id, meta);
            NotifyOutputNeighbor(evt.Level, evt.X, evt.Y, evt.Z, meta);
        }
        else if (!lit)
        {
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, PoweredRepeater.id, meta);
            NotifyOutputNeighbor(evt.Level, evt.X, evt.Y, evt.Z, meta);

            if (!powered)
            {
                int delaySetting = (meta & 12) >> 2;
                evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, PoweredRepeater.id, DELAY[delaySetting] * 2);
            }
        }
    }

    public override int getTexture(int side, int meta) => side == 0 ? lit ? 99 : 115 : side == 1 ? lit ? 147 : 131 : 5;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side) => side != 0 && side != 1;

    public override BlockRendererType getRenderType() => BlockRendererType.Repeater;

    public override int getTexture(int side) => getTexture(side, 0);

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => isPoweringSide(world, x, y, z, side);

    public override bool isPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        if (!lit)
        {
            return false;
        }

        int facing = reader.GetMeta(x, y, z) & 3;
        return facing == 0 && side == 3 ? true : facing == 1 && side == 4 ? true : facing == 2 && side == 2 ? true : facing == 3 && side == 5;
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        if (!canGrow(evt))
        {
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Meta, evt.BlockId));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }
        else
        {
            int meta = evt.Level.BlocksReader.GetMeta(evt.X, evt.Y, evt.Z);
            bool powered = isPowered(evt.Level.BlocksReader, evt.Level.Redstone, evt.X, evt.Y, evt.Z, meta);
            int delaySetting = (meta & 12) >> 2;
            if (lit && !powered)
            {
                evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id, DELAY[delaySetting] * 2);
            }
            else if (!lit && powered)
            {
                evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id, DELAY[delaySetting] * 2);
            }
        }
    }

    private bool isPowered(IBlockReader world, RedstoneEngine redstoneEngine, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        switch (facing)
        {
            case 0:
                return redstoneEngine.IsPoweringSide(x, y, z + 1, 3) || (world.GetBlockId(x, y, z + 1) == RedstoneWire.id && world.GetMeta(x, y, z + 1) > 0);
            case 1:
                return redstoneEngine.IsPoweringSide(x - 1, y, z, 4) || (world.GetBlockId(x - 1, y, z) == RedstoneWire.id && world.GetMeta(x - 1, y, z) > 0);
            case 2:
                return redstoneEngine.IsPoweringSide(x, y, z - 1, 2) || (world.GetBlockId(x, y, z - 1) == RedstoneWire.id && world.GetMeta(x, y, z - 1) > 0);
            case 3:
                return redstoneEngine.IsPoweringSide(x + 1, y, z, 5) || (world.GetBlockId(x + 1, y, z) == RedstoneWire.id && world.GetMeta(x + 1, y, z) > 0);
            default:
                return false;
        }
    }

    public override bool onUse(OnUseEvt ctx)
    {
        int meta = ctx.Level.BlocksReader.GetMeta(ctx.X, ctx.Y, ctx.Z);
        int newDelaySetting = (meta & 12) >> 2;
        newDelaySetting = ((newDelaySetting + 1) << 2) & 12;
        ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, newDelaySetting | (meta & 3));
        return true;
    }

    public override bool canEmitRedstonePower() => true;

    public override void onPlaced(OnPlacedEvt evt)
    {
        if (evt.Placer != null)
        {
            float yaw = evt.Placer.yaw;
            int facing = ((MathHelper.Floor(yaw * 4.0F / 360.0F + 0.5D) & 3) + 2) % 4;
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, facing);
        }

        int meta = evt.Level.BlocksReader.GetMeta(evt.X, evt.Y, evt.Z);

        bool powered = isPowered(evt.Level.BlocksReader, evt.Level.Redstone, evt.X, evt.Y, evt.Z, meta);
        if (powered)
        {
            evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id, 1);
        }

        evt.Level.Broadcaster.NotifyNeighbors(evt.X + 1, evt.Y, evt.Z, id);
        evt.Level.Broadcaster.NotifyNeighbors(evt.X - 1, evt.Y, evt.Z, id);
        evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z + 1, id);
        evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z - 1, id);
        evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y - 1, evt.Z, id);
        evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y + 1, evt.Z, id);
    }

    public override bool isOpaque() => false;

    public override int getDroppedItemId(int blockMeta) => Item.Repeater.id;

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        if (!lit)
        {
            return;
        }

        int meta = ctx.Level.BlocksReader.GetMeta(ctx.X, ctx.Y, ctx.Z);
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

        ctx.Level.Broadcaster.AddParticle("reddust", particleX + offsetX, particleY, particleZ + offsetY, 0.0D, 0.0D, 0.0D);
    }

    private void NotifyOutputNeighbor(IBlockWorldContext level, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        switch (facing)
        {
            case 0: level.Broadcaster.NotifyNeighbors(x, y, z - 1, id); break; // Outputs North
            case 1: level.Broadcaster.NotifyNeighbors(x + 1, y, z, id); break; // Outputs East
            case 2: level.Broadcaster.NotifyNeighbors(x, y, z + 1, id); break; // Outputs South
            case 3: level.Broadcaster.NotifyNeighbors(x - 1, y, z, id); break; // Outputs West
        }
    }
}
