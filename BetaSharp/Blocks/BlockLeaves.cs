using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Colors;

namespace BetaSharp.Blocks;

public class BlockLeaves : BlockLeavesBase
{
    private int spriteIndex;
    private readonly ThreadLocal<int[]> s_decayRegion = new(() => null);

    public BlockLeaves(int id, int textureId) : base(id, textureId, Material.Leaves, false)
    {
        spriteIndex = textureId;
        setTickRandomly(true);
    }

    public override int getColor(int meta)
    {
        return (meta & 1) == 1 ? FoliageColors.getSpruceColor() : ((meta & 2) == 2 ? FoliageColors.getBirchColor() : FoliageColors.getDefaultColor());
    }

    public override int getColorMultiplier(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int meta = iBlockAccess.getBlockMeta(x, y, z);
        if ((meta & 1) == 1)
        {
            return FoliageColors.getSpruceColor();
        }
        else if ((meta & 2) == 2)
        {
            return FoliageColors.getBirchColor();
        }
        else
        {
            iBlockAccess.getBiomeSource().GetBiomesInArea(x, z, 1, 1);
            double temperature = iBlockAccess.getBiomeSource().TemperatureMap[0];
            double downfall = iBlockAccess.getBiomeSource().DownfallMap[0];
            return FoliageColors.getFoliageColor(temperature, downfall);
        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        sbyte searchRadius = 1;
        int loadCheckExtent = searchRadius + 1;
        if (world.isRegionLoaded(x - loadCheckExtent, y - loadCheckExtent, z - loadCheckExtent, x + loadCheckExtent, y + loadCheckExtent, z + loadCheckExtent))
        {
            for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
                {
                    for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                    {
                        int blockId = world.getBlockId(x + offsetX, y + offsetY, z + offsetZ);
                        if (blockId == Block.Leaves.id)
                        {
                            int leavesMeta = world.getBlockMeta(x + offsetX, y + offsetY, z + offsetZ);
                            world.SetBlockMetaWithoutNotifyingNeighbors(x + offsetX, y + offsetY, z + offsetZ, leavesMeta | 8);
                        }
                    }
                }
            }
        }

    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (!world.isRemote)
        {
            int meta = world.getBlockMeta(x, y, z);
            if ((meta & 8) != 0)
            {
                sbyte decayRadius = 4;
                int loadCheckExtent = decayRadius + 1;
                sbyte regionSize = 32;
                int planeSize = regionSize * regionSize;
                int centerOffset = regionSize / 2;
                if (s_decayRegion.Value == null)
                {
                    s_decayRegion.Value = new int[regionSize * regionSize * regionSize];
                }

                int[] decayRegion = s_decayRegion.Value;

                int distanceToLog;
                if (world.isRegionLoaded(x - loadCheckExtent, y - loadCheckExtent, z - loadCheckExtent, x + loadCheckExtent, y + loadCheckExtent, z + loadCheckExtent))
                {
                    distanceToLog = -decayRadius;

                    while (distanceToLog <= decayRadius)
                    {
                        int dx;
                        int dy;
                        int dz;

                        for (dx = -decayRadius; dx <= decayRadius; ++dx)
                        {
                            for (dy = -decayRadius; dy <= decayRadius; ++dy)
                            {
                                dz = world.getBlockId(x + distanceToLog, y + dx, z + dy);
                                if (dz == Block.Log.id)
                                {
                                    decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = 0;
                                }
                                else if (dz == Block.Leaves.id)
                                {
                                    decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = -2;
                                }
                                else
                                {
                                    decayRegion[(distanceToLog + centerOffset) * planeSize + (dx + centerOffset) * regionSize + dy + centerOffset] = -1;
                                }
                            }
                        }

                        ++distanceToLog;
                    }

                    for (distanceToLog = 1; distanceToLog <= 4; ++distanceToLog)
                    {
                        int dx;
                        int dy;
                        int dz;

                        for (dx = -decayRadius; dx <= decayRadius; ++dx)
                        {
                            for (dy = -decayRadius; dy <= decayRadius; ++dy)
                            {
                                for (dz = -decayRadius; dz <= decayRadius; ++dz)
                                {
                                    if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] == distanceToLog - 1)
                                    {
                                        if (decayRegion[(dx + centerOffset - 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset - 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset + 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset + 1) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset - 1) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset - 1) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset + 1) * regionSize + dz + centerOffset] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset + 1) * regionSize + dz + centerOffset] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + (dz + centerOffset - 1)] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + (dz + centerOffset - 1)] = distanceToLog;
                                        }

                                        if (decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset + 1] == -2)
                                        {
                                            decayRegion[(dx + centerOffset) * planeSize + (dy + centerOffset) * regionSize + dz + centerOffset + 1] = distanceToLog;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                distanceToLog = decayRegion[centerOffset * planeSize + centerOffset * regionSize + centerOffset];
                if (distanceToLog >= 0)
                {
                    world.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, meta & -9);
                }
                else
                {
                    breakLeaves(world, x, y, z);
                }
            }

        }
    }

    private void breakLeaves(World world, int x, int y, int z)
    {
        dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
        world.setBlock(x, y, z, 0);
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return random.NextInt(20) == 0 ? 1 : 0;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Block.Sapling.id;
    }

    public override void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        if (!world.isRemote && player.getHand() != null && player.getHand().itemId == Item.Shears.id)
        {
            player.increaseStat(Stats.Stats.MineBlockStatArray[id], 1);
            dropStack(world, x, y, z, new ItemStack(Block.Leaves.id, 1, meta & 3));
        }
        else
        {
            base.afterBreak(world, player, x, y, z, meta);
        }

    }

    protected override int getDroppedItemMeta(int blockMeta)
    {
        return blockMeta & 3;
    }

    public override bool isOpaque()
    {
        return !graphicsLevel;
    }

    public override int getTexture(int side, int meta)
    {
        return (meta & 3) == 1 ? textureId + 80 : textureId;
    }

    public void setGraphicsLevel(bool bl)
    {
        graphicsLevel = bl;
        textureId = spriteIndex + (bl ? 0 : 1);
    }

    public override void onSteppedOn(World world, int x, int y, int z, Entity entity)
    {
        base.onSteppedOn(world, x, y, z, entity);
    }
}
