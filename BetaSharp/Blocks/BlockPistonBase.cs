using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockPistonBase : Block
{
    private bool sticky;
    private bool deaf;

    public BlockPistonBase(int id, int textureId, bool sticky) : base(id, textureId, Material.Piston)
    {
        this.sticky = sticky;
        setSoundGroup(SoundStoneFootstep);
        setHardness(0.5F);
    }

    public int getTopTexture()
    {
        return sticky ? 106 : 107;
    }

    public override int getTexture(int side)
    {
        return side switch
        {
            1 => getTopTexture(),
            0 => 109,
            _ => 108
        };
    }

    public override int getTexture(int side, int meta)
    {
        int var3 = getFacing(meta);
        return var3 > 5
            ? textureId
            : (side == var3
                ? (!isExtended(meta) && BoundingBox.MinX <= 0.0D && BoundingBox.MinY <= 0.0D && BoundingBox.MinZ <= 0.0D && BoundingBox.MaxX >= 1.0D && BoundingBox.MaxY >= 1.0D && BoundingBox.MaxZ >= 1.0D ? textureId : 110)
                : (side == PistonConstants.field_31057_a[var3] ? 109 : 108));
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.PistonBase;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        return false;
    }

    public override void onPlaced(World world, int x, int y, int z, EntityLiving placer)
    {
        int var6 = getFacingForPlacement(world, x, y, z, (EntityPlayer)placer);
        world.setBlockMeta(x, y, z, var6);
        if (!world.isRemote)
        {
            checkExtended(world, x, y, z);
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!world.isRemote && !deaf)
        {
            checkExtended(world, x, y, z);
        }
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (!world.isRemote && world.getBlockEntity(x, y, z) == null)
        {
            checkExtended(world, x, y, z);
        }
    }

    private void checkExtended(World world, int x, int y, int z)
    {
        int blockMeta = world.getBlockMeta(x, y, z);
        int facingDir = getFacing(blockMeta);
        bool shouldBeExtended = this.shouldExtend(world, x, y, z, facingDir);
        if (blockMeta != 7)
        {
            if (shouldBeExtended && !isExtended(blockMeta))
            {
                if (canExtend(world, x, y, z, facingDir))
                {
                    world.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facingDir | 8);
                    world.playNoteBlockActionAt(x, y, z, 0, facingDir);
                }
            }
            else if (!shouldBeExtended && isExtended(blockMeta))
            {
                world.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facingDir);
                world.playNoteBlockActionAt(x, y, z, 1, facingDir);
            }
        }
    }

    private bool shouldExtend(World world, int x, int y, int z, int facing)
    {
        return facing != 0 && world.isPoweringSide(x, y - 1, z, 0)
            ? true
            : (facing != 1 && world.isPoweringSide(x, y + 1, z, 1)
                ? true
                : (facing != 2 && world.isPoweringSide(x, y, z - 1, 2)
                    ? true
                    : (facing != 3 && world.isPoweringSide(x, y, z + 1, 3)
                        ? true
                        : (facing != 5 && world.isPoweringSide(x + 1, y, z, 5)
                            ? true
                            : (facing != 4 && world.isPoweringSide(x - 1, y, z, 4)
                                ? true
                                : (world.isPoweringSide(x, y, z, 0)
                                    ? true
                                    : (world.isPoweringSide(x, y + 2, z, 1)
                                        ? true
                                        : (world.isPoweringSide(x, y + 1, z - 1, 2)
                                            ? true
                                            : (world.isPoweringSide(x, y + 1, z + 1, 3) ? true : (world.isPoweringSide(x - 1, y + 1, z, 4) ? true : world.isPoweringSide(x + 1, y + 1, z, 5)))))))))));
    }

    public override void onBlockAction(World world, int x, int y, int z, int data1, int data2)
    {
        deaf = true;
        if (data1 == 0)
        {
            if (push(world, x, y, z, data2))
            {
                world.setBlockMeta(x, y, z, data2 | 8);
                world.playSound((double)x + 0.5D, (double)y + 0.5D, (double)z + 0.5D, "tile.piston.out", 0.5F, world.random.NextFloat() * 0.25F + 0.6F);
            }
        }
        else if (data1 == 1)
        {
            BlockEntity var8 = world.getBlockEntity(x + PistonConstants.HEAD_OFFSET_X[data2], y + PistonConstants.HEAD_OFFSET_Y[data2], z + PistonConstants.HEAD_OFFSET_Z[data2]);
            if (var8 != null && var8 is BlockEntityPiston)
            {
                ((BlockEntityPiston)var8).finish();
            }

            world.SetBlockWithoutNotifyingNeighbors(x, y, z, MovingPiston.id, data2);
            world.setBlockEntity(x, y, z, BlockPistonMoving.createPistonBlockEntity(id, data2, data2, false, true));
            if (sticky)
            {
                int var9 = x + PistonConstants.HEAD_OFFSET_X[data2] * 2;
                int var10 = y + PistonConstants.HEAD_OFFSET_Y[data2] * 2;
                int var11 = z + PistonConstants.HEAD_OFFSET_Z[data2] * 2;
                int var12 = world.getBlockId(var9, var10, var11);
                int var13 = world.getBlockMeta(var9, var10, var11);
                bool var14 = false;
                if (var12 == MovingPiston.id)
                {
                    BlockEntity var15 = world.getBlockEntity(var9, var10, var11);
                    if (var15 != null && var15 is BlockEntityPiston)
                    {
                        BlockEntityPiston var16 = (BlockEntityPiston)var15;
                        if (var16.getFacing() == data2 && var16.isExtending())
                        {
                            var16.finish();
                            var12 = var16.getPushedBlockId();
                            var13 = var16.getPushedBlockData();
                            var14 = true;
                        }
                    }
                }

                if (var14 || var12 <= 0 || !canMoveBlock(var12, world, var9, var10, var11, false) || Block.Blocks[var12].getPistonBehavior() != 0 && var12 != Block.Piston.id && var12 != Block.StickyPiston.id)
                {
                    if (!var14)
                    {
                        deaf = false;
                        world.setBlock(x + PistonConstants.HEAD_OFFSET_X[data2], y + PistonConstants.HEAD_OFFSET_Y[data2], z + PistonConstants.HEAD_OFFSET_Z[data2], 0);
                        deaf = true;
                    }
                }
                else
                {
                    deaf = false;
                    world.setBlock(var9, var10, var11, 0);
                    deaf = true;
                    x += PistonConstants.HEAD_OFFSET_X[data2];
                    y += PistonConstants.HEAD_OFFSET_Y[data2];
                    z += PistonConstants.HEAD_OFFSET_Z[data2];
                    world.SetBlockWithoutNotifyingNeighbors(x, y, z, MovingPiston.id, var13);
                    world.setBlockEntity(x, y, z, BlockPistonMoving.createPistonBlockEntity(var12, var13, data2, false, false));
                }
            }
            else
            {
                deaf = false;
                world.setBlock(x + PistonConstants.HEAD_OFFSET_X[data2], y + PistonConstants.HEAD_OFFSET_Y[data2], z + PistonConstants.HEAD_OFFSET_Z[data2], 0);
                deaf = true;
            }

            world.playSound((double)x + 0.5D, (double)y + 0.5D, (double)z + 0.5D, "tile.piston.in", 0.5F, world.random.NextFloat() * 0.15F + 0.6F);
        }

        deaf = false;
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int var5 = iBlockAccess.getBlockMeta(x, y, z);
        if (isExtended(var5))
        {
            switch (getFacing(var5))
            {
                case 0:
                    setBoundingBox(0.0F, 0.25F, 0.0F, 1.0F, 1.0F, 1.0F);
                    break;
                case 1:
                    setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 12.0F / 16.0F, 1.0F);
                    break;
                case 2:
                    setBoundingBox(0.0F, 0.0F, 0.25F, 1.0F, 1.0F, 1.0F);
                    break;
                case 3:
                    setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 12.0F / 16.0F);
                    break;
                case 4:
                    setBoundingBox(0.25F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                    break;
                case 5:
                    setBoundingBox(0.0F, 0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F);
                    break;
            }
        }
        else
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }
    }

    public override void setupRenderBoundingBox()
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override void addIntersectingBoundingBox(World world, int x, int y, int z, Box box, List<Box> boxes)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
    }

    public override bool isFullCube()
    {
        return false;
    }

    public static int getFacing(int meta)
    {
        return meta & 7;
    }

    public static bool isExtended(int meta)
    {
        return (meta & 8) != 0;
    }

    private static int getFacingForPlacement(World world, int x, int y, int z, EntityPlayer player)
    {
        if (MathF.Abs((float)player.x - (float)x) < 2.0F && MathHelper.Abs((float)player.z - (float)z) < 2.0F)
        {
            double var5 = player.y + 1.82D - (double)player.standingEyeHeight;
            if (var5 - (double)y > 2.0D)
            {
                return 1;
            }

            if ((double)y - var5 > 0.0D)
            {
                return 0;
            }
        }

        int var7 = MathHelper.Floor((double)(player.yaw * 4.0F / 360.0F) + 0.5D) & 3;
        return var7 == 0 ? 2 : (var7 == 1 ? 5 : (var7 == 2 ? 3 : (var7 == 3 ? 4 : 0)));
    }

    private static bool canMoveBlock(int id, World world, int x, int y, int z, bool allowBreaking)
    {
        if (id == Block.Obsidian.id)
        {
            return false;
        }
        else
        {
            if (id != Block.Piston.id && id != Block.StickyPiston.id)
            {
                if (Block.Blocks[id].getHardness() == -1.0F)
                {
                    return false;
                }

                if (Block.Blocks[id].getPistonBehavior() == 2)
                {
                    return false;
                }

                if (!allowBreaking && Block.Blocks[id].getPistonBehavior() == 1)
                {
                    return false;
                }
            }
            else if (isExtended(world.getBlockMeta(x, y, z)))
            {
                return false;
            }

            BlockEntity var6 = world.getBlockEntity(x, y, z);
            return var6 == null;
        }
    }

    private static bool canExtend(World world, int x, int y, int z, int dir)
    {
        int checkX = x + PistonConstants.HEAD_OFFSET_X[dir];
        int checkY = y + PistonConstants.HEAD_OFFSET_Y[dir];
        int checkZ = z + PistonConstants.HEAD_OFFSET_Z[dir];
        int blocksPushed = 0;

        while (true)
        {
            if (blocksPushed < 13)
            {
                if (checkY <= 0 || checkY >= 127)
                {
                    return false;
                }

                int blockId = world.getBlockId(checkX, checkY, checkZ);
                if (blockId != 0)
                {
                    if (!canMoveBlock(blockId, world, checkX, checkY, checkZ, true))
                    {
                        return false;
                    }

                    if (Block.Blocks[blockId].getPistonBehavior() != 1)
                    {
                        if (blocksPushed == 12)
                        {
                            return false;
                        }

                        checkX += PistonConstants.HEAD_OFFSET_X[dir];
                        checkY += PistonConstants.HEAD_OFFSET_Y[dir];
                        checkZ += PistonConstants.HEAD_OFFSET_Z[dir];
                        ++blocksPushed;
                        continue;
                    }
                }
            }

            return true;
        }
    }

    private bool push(World world, int x, int y, int z, int dir)
    {
        int currentX = x + PistonConstants.HEAD_OFFSET_X[dir];
        int currentY = y + PistonConstants.HEAD_OFFSET_Y[dir];
        int currentZ = z + PistonConstants.HEAD_OFFSET_Z[dir];
        int blocksInPath = 0;

        while (true)
        {
            int neighborBlockId;
            if (blocksInPath < 13)
            {
                if (currentY <= 0 || currentY >= 127)
                {
                    return false;
                }

                neighborBlockId = world.getBlockId(currentX, currentY, currentZ);
                if (neighborBlockId != 0)
                {
                    if (!canMoveBlock(neighborBlockId, world, currentX, currentY, currentZ, true))
                    {
                        return false;
                    }

                    if (Block.Blocks[neighborBlockId].getPistonBehavior() != 1)
                    {
                        if (blocksInPath == 12)
                        {
                            return false;
                        }

                        currentX += PistonConstants.HEAD_OFFSET_X[dir];
                        currentY += PistonConstants.HEAD_OFFSET_Y[dir];
                        currentZ += PistonConstants.HEAD_OFFSET_Z[dir];
                        ++blocksInPath;
                        continue;
                    }

                    Block.Blocks[neighborBlockId].dropStacks(world, currentX, currentY, currentZ, world.getBlockMeta(currentX, currentY, currentZ));
                    world.setBlock(currentX, currentY, currentZ, 0);
                }
            }

            while (currentX != x || currentY != y || currentZ != z)
            {
                int prevX = currentX - PistonConstants.HEAD_OFFSET_X[dir];
                int prevY = currentY - PistonConstants.HEAD_OFFSET_Y[dir];
                int prevZ = currentZ - PistonConstants.HEAD_OFFSET_Z[dir];
                int prevBlockId = world.getBlockId(prevX, prevY, prevZ);
                int prevBlockMeta = world.getBlockMeta(prevX, prevY, prevZ);
                if (prevBlockId == id && prevX == x && prevY == y && prevZ == z)
                {
                    world.SetBlockWithoutNotifyingNeighbors(currentX, currentY, currentZ, MovingPiston.id, dir | (sticky ? 8 : 0));
                    world.setBlockEntity(currentX, currentY, currentZ, BlockPistonMoving.createPistonBlockEntity(PistonHead.id, dir | (sticky ? 8 : 0), dir, true, false));
                }
                else
                {
                    world.SetBlockWithoutNotifyingNeighbors(currentX, currentY, currentZ, MovingPiston.id, prevBlockMeta);
                    world.setBlockEntity(currentX, currentY, currentZ, BlockPistonMoving.createPistonBlockEntity(prevBlockId, prevBlockMeta, dir, true, false));
                }

                currentX = prevX;
                currentY = prevY;
                currentZ = prevZ;
            }

            return true;
        }
    }
}
