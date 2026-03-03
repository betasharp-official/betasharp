using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockRedstoneWire : Block
{

    private static readonly ThreadLocal<bool> s_wiresProvidePower = new(() => true);

    public BlockRedstoneWire(int id, int textureId) : base(id, textureId, Material.PistonBreakable)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F / 16.0F, 1.0F);
    }

    public override int getTexture(int var1, int var2)
    {
        return textureId;
    }

    public override Box? getCollisionShape(World var1, int var2, int var3, int var4)
    {
        return null;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.RedstoneWire;
    }

    public override int getColorMultiplier(IBlockAccess var1, int var2, int var3, int var4)
    {
        return 8388608;
    }

    public override bool canPlaceAt(World var1, int var2, int var3, int var4)
    {
        return var1.shouldSuffocate(var2, var3 - 1, var4);
    }

    private void updateAndPropagateCurrentStrength(World world, int var2, int var3, int var4)
    {
        HashSet<BlockPos> neighbors = [];
        func_21030_a(world, var2, var3, var4, var2, var3, var4, neighbors);
        List<BlockPos> neighborsCopy = [.. neighbors];
        neighbors.Clear();

        foreach (BlockPos n in neighborsCopy)
        {
            world.notifyNeighbors(n.x, n.y, n.z, id);
        }

    }

    private void func_21030_a(World world, int var2, int var3, int var4, int var5, int var6, int var7, HashSet<BlockPos> neighbors)
    {
        int var8 = world.getBlockMeta(var2, var3, var4);
        int var9 = 0;
        s_wiresProvidePower.Value = false;
        bool var10 = world.isPowered(var2, var3, var4);
        s_wiresProvidePower.Value = true;
        int var11;
        int var12;
        int var13;
        if (var10)
        {
            var9 = 15;
        }
        else
        {
            for (var11 = 0; var11 < 4; ++var11)
            {
                var12 = var2;
                var13 = var4;
                if (var11 == 0)
                {
                    var12 = var2 - 1;
                }

                if (var11 == 1)
                {
                    ++var12;
                }

                if (var11 == 2)
                {
                    var13 = var4 - 1;
                }

                if (var11 == 3)
                {
                    ++var13;
                }

                if (var12 != var5 || var3 != var6 || var13 != var7)
                {
                    var9 = getMaxCurrentStrength(world, var12, var3, var13, var9);
                }

                if (world.shouldSuffocate(var12, var3, var13) && !world.shouldSuffocate(var2, var3 + 1, var4))
                {
                    if (var12 != var5 || var3 + 1 != var6 || var13 != var7)
                    {
                        var9 = getMaxCurrentStrength(world, var12, var3 + 1, var13, var9);
                    }
                }
                else if (!world.shouldSuffocate(var12, var3, var13) && (var12 != var5 || var3 - 1 != var6 || var13 != var7))
                {
                    var9 = getMaxCurrentStrength(world, var12, var3 - 1, var13, var9);
                }
            }

            if (var9 > 0)
            {
                --var9;
            }
            else
            {
                var9 = 0;
            }
        }

        if (var8 != var9)
        {
            world.pauseTicking = true;
            world.setBlockMeta(var2, var3, var4, var9);
            world.setBlocksDirty(var2, var3, var4, var2, var3, var4);
            world.pauseTicking = false;

            for (var11 = 0; var11 < 4; ++var11)
            {
                var12 = var2;
                var13 = var4;
                int var14 = var3 - 1;
                if (var11 == 0)
                {
                    var12 = var2 - 1;
                }

                if (var11 == 1)
                {
                    ++var12;
                }

                if (var11 == 2)
                {
                    var13 = var4 - 1;
                }

                if (var11 == 3)
                {
                    ++var13;
                }

                if (world.shouldSuffocate(var12, var3, var13))
                {
                    var14 += 2;
                }

                bool var15 = false;
                int var16 = getMaxCurrentStrength(world, var12, var3, var13, -1);
                var9 = world.getBlockMeta(var2, var3, var4);
                if (var9 > 0)
                {
                    --var9;
                }

                if (var16 >= 0 && var16 != var9)
                {
                    func_21030_a(world, var12, var3, var13, var2, var3, var4, neighbors);
                }

                var16 = getMaxCurrentStrength(world, var12, var14, var13, -1);
                var9 = world.getBlockMeta(var2, var3, var4);
                if (var9 > 0)
                {
                    --var9;
                }

                if (var16 >= 0 && var16 != var9)
                {
                    func_21030_a(world, var12, var14, var13, var2, var3, var4, neighbors);
                }
            }

            if (var8 == 0 || var9 == 0)
            {
                neighbors.Add(new BlockPos(var2, var3, var4));
                neighbors.Add(new BlockPos(var2 - 1, var3, var4));
                neighbors.Add(new BlockPos(var2 + 1, var3, var4));
                neighbors.Add(new BlockPos(var2, var3 - 1, var4));
                neighbors.Add(new BlockPos(var2, var3 + 1, var4));
                neighbors.Add(new BlockPos(var2, var3, var4 - 1));
                neighbors.Add(new BlockPos(var2, var3, var4 + 1));
            }
        }

    }

    private void notifyWireNeighborsOfNeighborChange(World var1, int var2, int var3, int var4)
    {
        if (var1.getBlockId(var2, var3, var4) == id)
        {
            var1.notifyNeighbors(var2, var3, var4, id);
            var1.notifyNeighbors(var2 - 1, var3, var4, id);
            var1.notifyNeighbors(var2 + 1, var3, var4, id);
            var1.notifyNeighbors(var2, var3, var4 - 1, id);
            var1.notifyNeighbors(var2, var3, var4 + 1, id);
            var1.notifyNeighbors(var2, var3 - 1, var4, id);
            var1.notifyNeighbors(var2, var3 + 1, var4, id);
        }
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        base.onPlaced(world, x, y, z);
        if (!world.isRemote)
        {
            updateAndPropagateCurrentStrength(world, x, y, z);
            world.notifyNeighbors(x, y + 1, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
            notifyWireNeighborsOfNeighborChange(world, x - 1, y, z);
            notifyWireNeighborsOfNeighborChange(world, x + 1, y, z);
            notifyWireNeighborsOfNeighborChange(world, x, y, z - 1);
            notifyWireNeighborsOfNeighborChange(world, x, y, z + 1);
            if (world.shouldSuffocate(x - 1, y, z))
            {
                notifyWireNeighborsOfNeighborChange(world, x - 1, y + 1, z);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x - 1, y - 1, z);
            }

            if (world.shouldSuffocate(x + 1, y, z))
            {
                notifyWireNeighborsOfNeighborChange(world, x + 1, y + 1, z);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x + 1, y - 1, z);
            }

            if (world.shouldSuffocate(x, y, z - 1))
            {
                notifyWireNeighborsOfNeighborChange(world, x, y + 1, z - 1);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x, y - 1, z - 1);
            }

            if (world.shouldSuffocate(x, y, z + 1))
            {
                notifyWireNeighborsOfNeighborChange(world, x, y + 1, z + 1);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x, y - 1, z + 1);
            }

        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        base.onBreak(world, x, y, z);
        if (!world.isRemote)
        {
            world.notifyNeighbors(x, y + 1, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
            updateAndPropagateCurrentStrength(world, x, y, z);
            notifyWireNeighborsOfNeighborChange(world, x - 1, y, z);
            notifyWireNeighborsOfNeighborChange(world, x + 1, y, z);
            notifyWireNeighborsOfNeighborChange(world, x, y, z - 1);
            notifyWireNeighborsOfNeighborChange(world, x, y, z + 1);
            if (world.shouldSuffocate(x - 1, y, z))
            {
                notifyWireNeighborsOfNeighborChange(world, x - 1, y + 1, z);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x - 1, y - 1, z);
            }

            if (world.shouldSuffocate(x + 1, y, z))
            {
                notifyWireNeighborsOfNeighborChange(world, x + 1, y + 1, z);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x + 1, y - 1, z);
            }

            if (world.shouldSuffocate(x, y, z - 1))
            {
                notifyWireNeighborsOfNeighborChange(world, x, y + 1, z - 1);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x, y - 1, z - 1);
            }

            if (world.shouldSuffocate(x, y, z + 1))
            {
                notifyWireNeighborsOfNeighborChange(world, x, y + 1, z + 1);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(world, x, y - 1, z + 1);
            }

        }
    }

    private int getMaxCurrentStrength(World var1, int var2, int var3, int var4, int var5)
    {
        if (var1.getBlockId(var2, var3, var4) != id)
        {
            return var5;
        }
        else
        {
            int var6 = var1.getBlockMeta(var2, var3, var4);
            return var6 > var5 ? var6 : var5;
        }
    }

    public override void neighborUpdate(World var1, int var2, int var3, int var4, int var5)
    {
        if (!var1.isRemote)
        {
            int var6 = var1.getBlockMeta(var2, var3, var4);
            bool var7 = canPlaceAt(var1, var2, var3, var4);
            if (!var7)
            {
                dropStacks(var1, var2, var3, var4, var6);
                var1.setBlock(var2, var3, var4, 0);
            }
            else
            {
                updateAndPropagateCurrentStrength(var1, var2, var3, var4);
            }

            base.neighborUpdate(var1, var2, var3, var4, var5);
        }
    }

    public override int getDroppedItemId(int var1, JavaRandom var2)
    {
        return Item.Redstone.id;
    }

    public override bool isStrongPoweringSide(World var1, int var2, int var3, int var4, int var5)
    {
        return !s_wiresProvidePower.Value ? false : isPoweringSide(var1, var2, var3, var4, var5);
    }

    public override bool isPoweringSide(IBlockAccess var1, int var2, int var3, int var4, int var5)
    {
        if (!s_wiresProvidePower.Value)
        {
            return false;
        }
        else if (var1.getBlockMeta(var2, var3, var4) == 0)
        {
            return false;
        }
        else if (var5 == 1)
        {
            return true;
        }
        else
        {
            bool var6 = isPowerProviderOrWire(var1, var2 - 1, var3, var4, 1) || !var1.shouldSuffocate(var2 - 1, var3, var4) && isPowerProviderOrWire(var1, var2 - 1, var3 - 1, var4, -1);
            bool var7 = isPowerProviderOrWire(var1, var2 + 1, var3, var4, 3) || !var1.shouldSuffocate(var2 + 1, var3, var4) && isPowerProviderOrWire(var1, var2 + 1, var3 - 1, var4, -1);
            bool var8 = isPowerProviderOrWire(var1, var2, var3, var4 - 1, 2) || !var1.shouldSuffocate(var2, var3, var4 - 1) && isPowerProviderOrWire(var1, var2, var3 - 1, var4 - 1, -1);
            bool var9 = isPowerProviderOrWire(var1, var2, var3, var4 + 1, 0) || !var1.shouldSuffocate(var2, var3, var4 + 1) && isPowerProviderOrWire(var1, var2, var3 - 1, var4 + 1, -1);
            if (!var1.shouldSuffocate(var2, var3 + 1, var4))
            {
                if (var1.shouldSuffocate(var2 - 1, var3, var4) && isPowerProviderOrWire(var1, var2 - 1, var3 + 1, var4, -1))
                {
                    var6 = true;
                }

                if (var1.shouldSuffocate(var2 + 1, var3, var4) && isPowerProviderOrWire(var1, var2 + 1, var3 + 1, var4, -1))
                {
                    var7 = true;
                }

                if (var1.shouldSuffocate(var2, var3, var4 - 1) && isPowerProviderOrWire(var1, var2, var3 + 1, var4 - 1, -1))
                {
                    var8 = true;
                }

                if (var1.shouldSuffocate(var2, var3, var4 + 1) && isPowerProviderOrWire(var1, var2, var3 + 1, var4 + 1, -1))
                {
                    var9 = true;
                }
            }

            return !var8 && !var7 && !var6 && !var9 && var5 >= 2 && var5 <= 5 ? true : (var5 == 2 && var8 && !var6 && !var7 ? true : (var5 == 3 && var9 && !var6 && !var7 ? true : (var5 == 4 && var6 && !var8 && !var9 ? true : var5 == 5 && var7 && !var8 && !var9)));
        }
    }

    public override bool canEmitRedstonePower()
    {
        return s_wiresProvidePower.Value;
    }

    public override void randomDisplayTick(World var1, int var2, int var3, int var4, JavaRandom var5)
    {
        int var6 = var1.getBlockMeta(var2, var3, var4);
        if (var6 > 0)
        {
            double var7 = (double)var2 + 0.5D + ((double)var5.NextFloat() - 0.5D) * 0.2D;
            double var9 = (double)((float)var3 + 1.0F / 16.0F);
            double var11 = (double)var4 + 0.5D + ((double)var5.NextFloat() - 0.5D) * 0.2D;
            float var13 = (float)var6 / 15.0F;
            float var14 = var13 * 0.6F + 0.4F;
            if (var6 == 0)
            {
                var14 = 0.0F;
            }

            float var15 = var13 * var13 * 0.7F - 0.5F;
            float var16 = var13 * var13 * 0.6F - 0.7F;
            if (var15 < 0.0F)
            {
                var15 = 0.0F;
            }

            if (var16 < 0.0F)
            {
                var16 = 0.0F;
            }

            var1.addParticle("reddust", var7, var9, var11, (double)var14, (double)var15, (double)var16);
        }

    }

    public static bool isPowerProviderOrWire(IBlockAccess var0, int var1, int var2, int var3, int var4)
    {
        int var5 = var0.getBlockId(var1, var2, var3);
        if (var5 == Block.RedstoneWire.id)
        {
            return true;
        }
        else if (var5 == 0)
        {
            return false;
        }
        else if (Block.Blocks[var5].canEmitRedstonePower())
        {
            return true;
        }
        else if (var5 != Block.Repeater.id && var5 != Block.PoweredRepeater.id)
        {
            return false;
        }
        else
        {
            int var6 = var0.getBlockMeta(var1, var2, var3);
            return var4 == Facings.OPPOSITE[var6 & 3];
        }
    }
}
