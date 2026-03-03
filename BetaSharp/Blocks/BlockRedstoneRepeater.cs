using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

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

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return !world.shouldSuffocate(x, y - 1, z) ? false : base.canPlaceAt(world, x, y, z);
    }

    public override bool canGrow(World world, int x, int y, int z)
    {
        return !world.shouldSuffocate(x, y - 1, z) ? false : base.canGrow(world, x, y, z);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        int meta = world.getBlockMeta(x, y, z);
        bool powered = isPowered(world, x, y, z, meta);
        if (lit && !powered)
        {
            world.setBlock(x, y, z, Block.Repeater.id, meta);
        }
        else if (!lit)
        {
            world.setBlock(x, y, z, Block.PoweredRepeater.id, meta);
            if (!powered)
            {
                int delaySetting = (meta & 12) >> 2;
                world.ScheduleBlockUpdate(x, y, z, PoweredRepeater.id, DELAY[delaySetting] * 2);
            }
        }

    }

    public override int getTexture(int side, int meta)
    {
        return side == 0 ? (lit ? 99 : 115) : (side == 1 ? (lit ? 147 : 131) : 5);
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
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

    public override bool isStrongPoweringSide(World world, int x, int y, int z, int side)
    {
        return isPoweringSide(world, x, y, z, side);
    }

    public override bool isPoweringSide(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (!lit)
        {
            return false;
        }
        else
        {
            int facing = iBlockAccess.getBlockMeta(x, y, z) & 3;
            return facing == 0 && side == 3 ? true : (facing == 1 && side == 4 ? true : (facing == 2 && side == 2 ? true : facing == 3 && side == 5));
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!canGrow(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }
        else
        {
            int meta = world.getBlockMeta(x, y, z);
            bool powered = isPowered(world, x, y, z, meta);
            int delaySetting = (meta & 12) >> 2;
            if (lit && !powered)
            {
                world.ScheduleBlockUpdate(x, y, z, base.id, DELAY[delaySetting] * 2);
            }
            else if (!lit && powered)
            {
                world.ScheduleBlockUpdate(x, y, z, base.id, DELAY[delaySetting] * 2);
            }

        }
    }

    private bool isPowered(World world, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        switch (facing)
        {
            case 0:
                return world.isPoweringSide(x, y, z + 1, 3) || world.getBlockId(x, y, z + 1) == Block.RedstoneWire.id && world.getBlockMeta(x, y, z + 1) > 0;
            case 1:
                return world.isPoweringSide(x - 1, y, z, 4) || world.getBlockId(x - 1, y, z) == Block.RedstoneWire.id && world.getBlockMeta(x - 1, y, z) > 0;
            case 2:
                return world.isPoweringSide(x, y, z - 1, 2) || world.getBlockId(x, y, z - 1) == Block.RedstoneWire.id && world.getBlockMeta(x, y, z - 1) > 0;
            case 3:
                return world.isPoweringSide(x + 1, y, z, 5) || world.getBlockId(x + 1, y, z) == Block.RedstoneWire.id && world.getBlockMeta(x + 1, y, z) > 0;
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

    public override void onPlaced(World world, int x, int y, int z, EntityLiving placer)
    {
        int facing = ((MathHelper.Floor((double)(placer.yaw * 4.0F / 360.0F) + 0.5D) & 3) + 2) % 4;
        world.setBlockMeta(x, y, z, facing);
        bool powered = isPowered(world, x, y, z, facing);
        if (powered)
        {
            world.ScheduleBlockUpdate(x, y, z, id, 1);
        }

    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        world.notifyNeighbors(x + 1, y, z, id);
        world.notifyNeighbors(x - 1, y, z, id);
        world.notifyNeighbors(x, y, z + 1, id);
        world.notifyNeighbors(x, y, z - 1, id);
        world.notifyNeighbors(x, y - 1, z, id);
        world.notifyNeighbors(x, y + 1, z, id);
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
