using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockRedstoneWire : Block
{
    private static readonly ThreadLocal<bool> s_wiresProvidePower = new(() => true);

    public BlockRedstoneWire(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F / 16.0F, 1.0F);

    public override int getTexture(int var1, int var2) => textureId;

    public override Box? getCollisionShape(IBlockReader var1, int var2, int var3, int var4) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.RedstoneWire;

    public override int getColorMultiplier(IBlockReader var1, int var2, int var3, int var4) => 8388608;

    public override bool canPlaceAt(OnPlacedEvt ctx) => ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z);

    private void updateAndPropagateCurrentStrength(World world, WorldBlockWrite worldWrite, int x, int y, int z)
    {
        HashSet<BlockPos> neighbors = [];
        func_21030_a(world, x, y, z, x, y, z, neighbors);
        List<BlockPos> neighborsCopy = [.. neighbors];
        neighbors.Clear();

        foreach (BlockPos n in neighborsCopy)
        {
            world.notifyNeighbors(n.x, n.y, n.z, id);
        }
    }

    private void func_21030_a(IBlockReader read, IBlockWrite write, RedstoneEngine redstoneEngine, int var2, int var3, int var4, int var5, int var6, int var7, HashSet<BlockPos> neighbors)
    {
        int var8 = read.GetBlockMeta(var2, var3, var4);
        int var9 = 0;
        s_wiresProvidePower.Value = false;
        bool var10 = redstoneEngine.IsPowered(var2, var3, var4);
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
                    var9 = getMaxCurrentStrength(read, var12, var3, var13, var9);
                }

                if (read.ShouldSuffocate(var12, var3, var13) && !read.ShouldSuffocate(var2, var3 + 1, var4))
                {
                    if (var12 != var5 || var3 + 1 != var6 || var13 != var7)
                    {
                        var9 = getMaxCurrentStrength(read, var12, var3 + 1, var13, var9);
                    }
                }
                else if (!read.ShouldSuffocate(var12, var3, var13) && (var12 != var5 || var3 - 1 != var6 || var13 != var7))
                {
                    var9 = getMaxCurrentStrength(read, var12, var3 - 1, var13, var9);
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
            write.PauseTicking = true;
            write.SetBlockMeta(var2, var3, var4, var9);
            write.SetBlocksDirty(var2, var3, var4, var2, var3, var4);
            write.PauseTicking = false;

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

                if (read.ShouldSuffocate(var12, var3, var13))
                {
                    var14 += 2;
                }

                bool var15 = false;
                int var16 = getMaxCurrentStrength(read, var12, var3, var13, -1);
                var9 = read.GetBlockMeta(var2, var3, var4);
                if (var9 > 0)
                {
                    --var9;
                }

                if (var16 >= 0 && var16 != var9)
                {
                    func_21030_a(read, write, redstoneEngine, var12, var3, var13, var2, var3, var4, neighbors);
                }

                var16 = getMaxCurrentStrength(read, var12, var14, var13, -1);
                var9 = read.GetBlockMeta(var2, var3, var4);
                if (var9 > 0)
                {
                    --var9;
                }

                if (var16 >= 0 && var16 != var9)
                {
                    func_21030_a(read, write, redstoneEngine, var12, var14, var13, var2, var3, var4, neighbors);
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

    private void notifyWireNeighborsOfNeighborChange(IBlockReader read, WorldEventBroadcaster broadcaster, int var2, int var3, int var4)
    {
        if (read.GetBlockId(var2, var3, var4) == id)
        {
            broadcaster.NotifyNeighbors(var2, var3, var4, id);
            broadcaster.NotifyNeighbors(var2 - 1, var3, var4, id);
            broadcaster.NotifyNeighbors(var2 + 1, var3, var4, id);
            broadcaster.NotifyNeighbors(var2, var3, var4 - 1, id);
            broadcaster.NotifyNeighbors(var2, var3, var4 + 1, id);
            broadcaster.NotifyNeighbors(var2, var3 - 1, var4, id);
            broadcaster.NotifyNeighbors(var2, var3 + 1, var4, id);
        }
    }

    public override void onPlaced(OnPlacedEvt ctx)
    {
        base.onPlaced(ctx);
        if (!ctx.IsRemote)
        {
            updateAndPropagateCurrentStrength(ctx.WorldRead, ctx.WorldWrite, ctx.X, ctx.Y, ctx.Z);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y + 1, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X - 1, ctx.Y, ctx.Z);
            notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X + 1, ctx.Y, ctx.Z);
            notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z - 1);
            notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z + 1);
            if (ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
            {
                notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X - 1, ctx.Y + 1, ctx.Z);
            }
            else
            {
                notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X - 1, ctx.Y - 1, ctx.Z);
            }

            if (ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
            {
                notifyWireNeighborsOfNeighborChange(ctx.WorldRead, ctx.Broadcaster, ctx.X + 1, ctx.Y + 1, ctx.Z);
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

    public override void onBreak(OnBreakEvt ctx)
    {
        base.onBreak(ctx);
        if (!ctx.IsRemote)
        {
            ctx.WorldWrite.NotifyNeighbors(ctx.X, ctx.Y + 1, ctx.Z, id);
            ctx.WorldWrite.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            updateAndPropagateCurrentStrength(ctx.WorldRead, ctx.WorldWrite, ctx.X, ctx.Y, ctx.Z);
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

        int var6 = var1.getBlockMeta(var2, var3, var4);
        return var6 > var5 ? var6 : var5;
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (!ctx.IsRemote)
        {
            int var6 = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            bool var7 = canPlaceAt(ctx);
            if (!var7)
            {
                dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, var6);
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                updateAndPropagateCurrentStrength(ctx.WorldRead, ctx.WorldWrite, ctx.X, ctx.Y, ctx.Z);
            }

            base.neighborUpdate(ctx);
        }
    }

    public override int getDroppedItemId(int var1) => Item.Redstone.id;

    public override bool isStrongPoweringSide(IBlockReader var1, int var2, int var3, int var4, int var5) => !s_wiresProvidePower.Value ? false : isPoweringSide(var1, var2, var3, var4, var5);

    public override bool isPoweringSide(IBlockReader var1, int var2, int var3, int var4, int var5)
    {
        if (!s_wiresProvidePower.Value)
        {
            return false;
        }

        if (var1.GetBlockMeta(var2, var3, var4) == 0)
        {
            return false;
        }

        if (var5 == 1)
        {
            return true;
        }

        bool var6 = isPowerProviderOrWire(var1, var2 - 1, var3, var4, 1) || (!var1.ShouldSuffocate(var2 - 1, var3, var4) && isPowerProviderOrWire(var1, var2 - 1, var3 - 1, var4, -1));
        bool var7 = isPowerProviderOrWire(var1, var2 + 1, var3, var4, 3) || (!var1.ShouldSuffocate(var2 + 1, var3, var4) && isPowerProviderOrWire(var1, var2 + 1, var3 - 1, var4, -1));
        bool var8 = isPowerProviderOrWire(var1, var2, var3, var4 - 1, 2) || (!var1.ShouldSuffocate(var2, var3, var4 - 1) && isPowerProviderOrWire(var1, var2, var3 - 1, var4 - 1, -1));
        bool var9 = isPowerProviderOrWire(var1, var2, var3, var4 + 1, 0) || (!var1.ShouldSuffocate(var2, var3, var4 + 1) && isPowerProviderOrWire(var1, var2, var3 - 1, var4 + 1, -1));
        if (!var1.ShouldSuffocate(var2, var3 + 1, var4))
        {
            if (var1.ShouldSuffocate(var2 - 1, var3, var4) && isPowerProviderOrWire(var1, var2 - 1, var3 + 1, var4, -1))
            {
                var6 = true;
            }

            if (var1.ShouldSuffocate(var2 + 1, var3, var4) && isPowerProviderOrWire(var1, var2 + 1, var3 + 1, var4, -1))
            {
                var7 = true;
            }

            if (var1.ShouldSuffocate(var2, var3, var4 - 1) && isPowerProviderOrWire(var1, var2, var3 + 1, var4 - 1, -1))
            {
                var8 = true;
            }

            if (var1.ShouldSuffocate(var2, var3, var4 + 1) && isPowerProviderOrWire(var1, var2, var3 + 1, var4 + 1, -1))
            {
                var9 = true;
            }
        }

        return !var8 && !var7 && !var6 && !var9 && var5 >= 2 && var5 <= 5 ? true :
            var5 == 2 && var8 && !var6 && !var7 ? true :
            var5 == 3 && var9 && !var6 && !var7 ? true :
            var5 == 4 && var6 && !var8 && !var9 ? true : var5 == 5 && var7 && !var8 && !var9;
    }

    public override bool canEmitRedstonePower() => s_wiresProvidePower.Value;

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        int var6 = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if (var6 > 0)
        {
            double x = ctx.X + 0.5D + (ctx.Random.NextFloat() - 0.5D) * 0.2D;
            double y = ctx.Y + 1.0F / 16.0F;
            double z = ctx.Z + 0.5D + (ctx.Random.NextFloat() - 0.5D) * 0.2D;
            float var13 = var6 / 15.0F;
            float xVel = var13 * 0.6F + 0.4F;
            if (var6 == 0)
            {
                xVel = 0.0F;
            }

            float yVle = var13 * var13 * 0.7F - 0.5F;
            float zVel = var13 * var13 * 0.6F - 0.7F;
            if (yVle < 0.0F)
            {
                yVle = 0.0F;
            }

            if (zVel < 0.0F)
            {
                zVel = 0.0F;
            }

            ctx.Broadcaster.AddParticle("reddust", x, y, z, xVel, yVle, zVel);
        }
    }

    public static bool isPowerProviderOrWire(IBlockReader var0, int var1, int var2, int var3, int var4)
    {
        int var5 = var0.GetBlockId(var1, var2, var3);
        if (var5 == RedstoneWire.id)
        {
            return true;
        }

        if (var5 == 0)
        {
            return false;
        }

        if (Blocks[var5].canEmitRedstonePower())
        {
            return true;
        }

        if (var5 != Repeater.id && var5 != PoweredRepeater.id)
        {
            return false;
        }

        int var6 = var0.GetBlockMeta(var1, var2, var3);
        return var4 == Facings.OPPOSITE[var6 & 3];
    }
}
