using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

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

    public override bool isFullCube()
    {
        return false;
    }

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z)
    {
        return !world.shouldSuffocate(x, y - 1, z) ? false : base.canPlaceAt(world, x, y, z);
    }

    public override bool canGrow(OnTickContext ctx)
    {
        return !ctx.WorldView.shouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) ? false : base.canGrow(ctx);
    }

    public override void onTick(OnTickContext ctx)
    {
        int meta = ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
        bool powered = isPowered(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, meta);
        if (lit && !powered)
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Block.Repeater.id, meta);
        }
        else if (!lit)
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Block.PoweredRepeater.id, meta);
            if (!powered)
            {
                int delaySetting = (meta & 12) >> 2;
                ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, PoweredRepeater.id, DELAY[delaySetting] * 2);
            }
        }

    }

    public override int getTexture(int side, int meta)
    {
        return side == 0 ? (lit ? 99 : 115) : (side == 1 ? (lit ? 147 : 131) : 5);
    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        return side != 0 && side != 1;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Repeater;
    }

    public override int getTexture(int side)
    {
        return getTexture(side, 0);
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        return isPoweringSide(world, x, y, z, side);
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (!lit)
        {
            return false;
        }
        else
        {
            int facing = iBlockReader.getBlockMeta(x, y, z) & 3;
            return facing == 0 && side == 3 ? true : (facing == 1 && side == 4 ? true : (facing == 2 && side == 2 ? true : facing == 3 && side == 5));
        }
    }

    public override void neighborUpdate(OnTickContext ctx)
    {
        if (!canGrow(ctx))
        {
            dropStacks(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
        else
        {
            int meta = ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
            bool powered = isPowered(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, meta);
            int delaySetting = (meta & 12) >> 2;
            if (lit && !powered)
            {
                ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, base.id, DELAY[delaySetting] * 2);
            }
            else if (!lit && powered)
            {
                ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, base.id, DELAY[delaySetting] * 2);
            }

        }
    }

    private bool isPowered(IBlockReader world, RedstoneEngine redstoneEngine, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        switch (facing)
        {
            case 0:
                return redstoneEngine.IsPoweringSide(x, y, z + 1, 3) || world.GetBlockId(x, y, z + 1) == Block.RedstoneWire.id && world.getBlockMeta(x, y, z + 1) > 0;
            case 1:
                return redstoneEngine.IsPoweringSide(x - 1, y, z, 4) || world.GetBlockId(x - 1, y, z) == Block.RedstoneWire.id && world.getBlockMeta(x - 1, y, z) > 0;
            case 2:
                return redstoneEngine.IsPoweringSide(x, y, z - 1, 2) || world.GetBlockId(x, y, z - 1) == Block.RedstoneWire.id && world.getBlockMeta(x, y, z - 1) > 0;
            case 3:
                return redstoneEngine.IsPoweringSide(x + 1, y, z, 5) || world.GetBlockId(x + 1, y, z) == Block.RedstoneWire.id && world.getBlockMeta(x + 1, y, z) > 0;
            default:
                return false;
        }
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        int meta = world.getBlockMeta(x, y, z);
        int newDelaySetting = (meta & 12) >> 2;
        newDelaySetting = newDelaySetting + 1 << 2 & 12;
        world.setBlockMeta(x, y, z, newDelaySetting | meta & 3);
        return true;
    }

    public override bool canEmitRedstonePower()
    {
        return false;
    }

    public override void onPlaced(OnPlacedContext ctx)
    {
        int facing = ((MathHelper.Floor((double)(placer.yaw * 4.0F / 360.0F) + 0.5D) & 3) + 2) % 4;
        world.setBlockMeta(x, y, z, facing);
        bool powered = isPowered(world, x, y, z, facing);
        if (powered)
        {
            world.ScheduleBlockUpdate(x, y, z, id, 1);
        }

    }

    public override void onPlaced(OnPlacedContext ctx)
    {
        ctx.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
        world.notifyNeighbors(x - 1, y, z, id);
        ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
        ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
        ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
        ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y + 1, ctx.Z, id);
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.Repeater.id;
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (lit)
        {
            int meta = world.getBlockMeta(x, y, z);
            double particleX = (double)((float)x + 0.5F) + (double)(random.NextFloat() - 0.5F) * 0.2D;
            double particleY = (double)((float)y + 0.4F) + (double)(random.NextFloat() - 0.5F) * 0.2D;
            double particleZ = (double)((float)z + 0.5F) + (double)(random.NextFloat() - 0.5F) * 0.2D;
            double offsetX = 0.0D;
            double offsetY = 0.0D;
            if (random.NextInt(2) == 0)
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

            world.addParticle("reddust", particleX + offsetX, particleY, particleZ + offsetY, 0.0D, 0.0D, 0.0D);
        }
    }
}
