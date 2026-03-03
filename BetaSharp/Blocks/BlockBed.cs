using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    public static readonly int[][] BED_OFFSETS = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public BlockBed(int id) : base(id, 134, Material.Wool)
    {
        setDefaultShape();
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.isRemote)
        {
            return true;
        }
        else
        {
            int meta = world.getBlockMeta(x, y, z);
            if (!isHeadOfBed(meta))
            {
                int direction = getDirection(meta);
                x += BED_OFFSETS[direction][0];
                z += BED_OFFSETS[direction][1];
                if (world.getBlockId(x, y, z) != id)
                {
                    return true;
                }

                meta = world.getBlockMeta(x, y, z);
            }

            if (!world.dimension.HasWorldSpawn)
            {
                double posX = (double)x + 0.5D;
                double posY = (double)y + 0.5D;
                double posZ = (double)z + 0.5D;
                world.setBlock(x, y, z, 0);
                int direction = getDirection(meta);
                x += BED_OFFSETS[direction][0];
                z += BED_OFFSETS[direction][1];
                if (world.getBlockId(x, y, z) == id)
                {
                    world.setBlock(x, y, z, 0);
                    posX = (posX + (double)x + 0.5D) / 2.0D;
                    posY = (posY + (double)y + 0.5D) / 2.0D;
                    posZ = (posZ + (double)z + 0.5D) / 2.0D;
                }

                world.createExplosion((Entity)null, (double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F), 5.0F, true);
                return true;
            }
            else
            {
                if (isBedOccupied(meta))
                {
                    EntityPlayer occupant = null;
                    foreach (var otherPlayer in world.players) {
                        if (otherPlayer.isSleeping())
                        {
                            Vec3i sleepingPos = otherPlayer.sleepingPos;
                            if (sleepingPos.X == x && sleepingPos.Y == y && sleepingPos.Z == z)
                            {
                                occupant = otherPlayer;
                            }
                        }
                    }

                    if (occupant != null)
                    {
                        player.sendMessage("tile.bed.occupied");
                        return true;
                    }

                    updateState(world, x, y, z, false);
                }

                SleepAttemptResult result = player.trySleep(x, y, z);
                if (result == SleepAttemptResult.OK)
                {
                    updateState(world, x, y, z, true);
                    return true;
                }
                else
                {
                    if (result == SleepAttemptResult.NOT_POSSIBLE_NOW)
                    {
                        player.sendMessage("tile.bed.noSleep");
                    }

                    return true;
                }
            }
        }
    }

    public override int getTexture(int side, int meta)
    {
        if (side == 0)
        {
            return Block.Planks.textureId;
        }
        else
        {
            int direction = getDirection(meta);
            int sideFacing = Facings.BED_FACINGS[direction][side];
            return isHeadOfBed(meta) ?
                (sideFacing == 2 ? textureId + 2 + 16 : (sideFacing != 5 && sideFacing != 4 ? textureId + 1 : textureId + 1 + 16)) :
                (sideFacing == 3 ? textureId - 1 + 16 : (sideFacing != 5 && sideFacing != 4 ? textureId : textureId + 16));
        }
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Bed;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        setDefaultShape();
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        int blockMeta = world.getBlockMeta(x, y, z);
        int direction = getDirection(blockMeta);
        if (isHeadOfBed(blockMeta))
        {
            if (world.getBlockId(x - BED_OFFSETS[direction][0], y, z - BED_OFFSETS[direction][1]) != this.id)
            {
                world.setBlock(x, y, z, 0);
            }
        }
        else if (world.getBlockId(x + BED_OFFSETS[direction][0], y, z + BED_OFFSETS[direction][1]) != this.id)
        {
            world.setBlock(x, y, z, 0);
            if (!world.isRemote)
            {
                dropStacks(world, x, y, z, blockMeta);
            }
        }

    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return isHeadOfBed(blockMeta) ? 0 : Item.Bed.id;
    }

    private void setDefaultShape()
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 9.0F / 16.0F, 1.0F);
    }

    public static int getDirection(int meta)
    {
        return meta & 3;
    }

    public static bool isHeadOfBed(int meta)
    {
        return (meta & 8) != 0;
    }

    public static bool isBedOccupied(int meta)
    {
        return (meta & 4) != 0;
    }

    public static void updateState(World world, int x, int y, int z, bool occupied)
    {
        int blockMeta = world.getBlockMeta(x, y, z);
        if (occupied)
        {
            blockMeta |= 4;
        }
        else
        {
            blockMeta &= -5;
        }

        world.setBlockMeta(x, y, z, blockMeta);
    }

    public static Vec3i? findWakeUpPosition(World world, int x, int y, int z, int skip)
    {
        int blockMeta = world.getBlockMeta(x, y, z);
        int direction = getDirection(blockMeta);

        for (int bedHalf = 0; bedHalf <= 1; ++bedHalf)
        {
            int searchMinX = x - BED_OFFSETS[direction][0] * bedHalf - 1;
            int searchMinZ = z - BED_OFFSETS[direction][1] * bedHalf - 1;
            int searchMaxX = searchMinX + 2;
            int searchMaxZ = searchMinZ + 2;

            for (int checkX = searchMinX; checkX <= searchMaxX; ++checkX)
            {
                for (int checkZ = searchMinZ; checkZ <= searchMaxZ; ++checkZ)
                {
                    if (world.shouldSuffocate(checkX, y - 1, checkZ) && world.isAir(checkX, y, checkZ) && world.isAir(checkX, y + 1, checkZ))
                    {
                        if (skip <= 0)
                        {
                            return new Vec3i(checkX, y, checkZ);
                        }

                        --skip;
                    }
                }
            }
        }

        return null;
    }

    public override void dropStacks(World world, int x, int y, int z, int meta, float luck)
    {
        if (!isHeadOfBed(meta))
        {
            base.dropStacks(world, x, y, z, meta, luck);
        }

    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
