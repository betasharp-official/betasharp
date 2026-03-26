using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRedstoneWire : Block
{
    private static readonly ThreadLocal<bool> s_wiresProvidePower = new(() => true);

    public BlockRedstoneWire(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F / 16.0F, 1.0F);

    public override int getTexture(int var1, int var2) => textureId;

    public override Box? getCollisionShape(IBlockReader var1, EntityManager entities, int var2, int var3, int var4) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.RedstoneWire;

    public override int getColorMultiplier(IBlockReader var1, int var2, int var3, int var4) => 8388608;

    public override bool canPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    private void updateAndPropagateCurrentStrength(IWorldContext level, int x, int y, int z)
    {
        HashSet<BlockPos> neighbors = [];
        CalculateCurrentChanges(level, x, y, z, x, y, z, neighbors);

        List<BlockPos> neighborsCopy = [.. neighbors];
        neighbors.Clear();

        foreach (BlockPos n in neighborsCopy)
        {
            level.Broadcaster.NotifyNeighbors(n.x, n.y, n.z, id);
        }
    }

    private void CalculateCurrentChanges(IWorldContext level, int x, int y, int z, int srcX, int srcY, int srcZ, HashSet<BlockPos> neighbors)
    {
        int oldMeta = level.Reader.GetBlockMeta(x, y, z);
        int newMeta = 0;

        s_wiresProvidePower.Value = false;
        bool isPowered = level.Redstone.IsPowered(x, y, z);
        s_wiresProvidePower.Value = true;

        if (isPowered)
        {
            newMeta = 15;
        }
        else
        {
            for (int dir = 0; dir < 4; ++dir)
            {
                int nx = x;
                int nz = z;

                if (dir == 0) nx = x - 1;
                if (dir == 1) ++nx;
                if (dir == 2) nz = z - 1;
                if (dir == 3) ++nz;

                if (nx != srcX || y != srcY || nz != srcZ)
                {
                    newMeta = getMaxCurrentStrength(level.Reader, nx, y, nz, newMeta);
                }

                if (level.Reader.ShouldSuffocate(nx, y, nz) && !level.Reader.ShouldSuffocate(x, y + 1, z))
                {
                    if (nx != srcX || y + 1 != srcY || nz != srcZ)
                    {
                        newMeta = getMaxCurrentStrength(level.Reader, nx, y + 1, nz, newMeta);
                    }
                }
                else if (!level.Reader.ShouldSuffocate(nx, y, nz) && (nx != srcX || y - 1 != srcY || nz != srcZ))
                {
                    newMeta = getMaxCurrentStrength(level.Reader, nx, y - 1, nz, newMeta);
                }
            }

            if (newMeta > 0)
            {
                --newMeta;
            }
            else
            {
                newMeta = 0;
            }
        }

        if (oldMeta != newMeta)
        {
            level.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, newMeta);
            level.Broadcaster.BlockUpdateEvent(x, y, z);
            level.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);

            for (int dir = 0; dir < 4; ++dir)
            {
                int nx = x;
                int nz = z;
                int ny = y - 1;

                if (dir == 0) nx = x - 1;
                if (dir == 1) ++nx;
                if (dir == 2) nz = z - 1;
                if (dir == 3) ++nz;

                if (level.Reader.ShouldSuffocate(nx, y, nz))
                {
                    ny += 2;
                }

                int neighborMeta = getMaxCurrentStrength(level.Reader, nx, y, nz, -1);
                newMeta = level.Reader.GetBlockMeta(x, y, z);
                if (newMeta > 0)
                {
                    --newMeta;
                }

                if (neighborMeta >= 0 && neighborMeta != newMeta)
                {
                    CalculateCurrentChanges(level, nx, y, nz, x, y, z, neighbors);
                }

                neighborMeta = getMaxCurrentStrength(level.Reader, nx, ny, nz, -1);
                newMeta = level.Reader.GetBlockMeta(x, y, z);
                if (newMeta > 0)
                {
                    --newMeta;
                }

                if (neighborMeta >= 0 && neighborMeta != newMeta)
                {
                    CalculateCurrentChanges(level, nx, ny, nz, x, y, z, neighbors);
                }
            }

            if (oldMeta == 0 || newMeta == 0)
            {
                neighbors.Add(new BlockPos(x, y, z));
                neighbors.Add(new BlockPos(x - 1, y, z));
                neighbors.Add(new BlockPos(x + 1, y, z));
                neighbors.Add(new BlockPos(x, y - 1, z));
                neighbors.Add(new BlockPos(x, y + 1, z));
                neighbors.Add(new BlockPos(x, y, z - 1));
                neighbors.Add(new BlockPos(x, y, z + 1));
            }
        }
    }

    private void NotifyWireNeighborsOfNeighborChange(IWorldContext level, int var2, int var3, int var4)
    {
        if (level.Reader.GetBlockId(var2, var3, var4) == id)
        {
            level.Broadcaster.NotifyNeighbors(var2, var3, var4, id);
            level.Broadcaster.NotifyNeighbors(var2 - 1, var3, var4, id);
            level.Broadcaster.NotifyNeighbors(var2 + 1, var3, var4, id);
            level.Broadcaster.NotifyNeighbors(var2, var3, var4 - 1, id);
            level.Broadcaster.NotifyNeighbors(var2, var3, var4 + 1, id);
            level.Broadcaster.NotifyNeighbors(var2, var3 - 1, var4, id);
            level.Broadcaster.NotifyNeighbors(var2, var3 + 1, var4, id);
        }
    }

    public override void onPlaced(OnPlacedEvent @event)
    {
        base.onPlaced(@event);
        if (!@event.World.IsRemote)
        {
            updateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X - 1, @event.Y, @event.Z);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X + 1, @event.Y, @event.Z);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y, @event.Z - 1);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y, @event.Z + 1);
            if (@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X - 1, @event.Y + 1, @event.Z);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X - 1, @event.Y - 1, @event.Z);
            }

            if (@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X + 1, @event.Y + 1, @event.Z);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X + 1, @event.Y - 1, @event.Z);
            }

            if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y + 1, @event.Z - 1);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y - 1, @event.Z - 1);
            }

            if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y + 1, @event.Z + 1);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y - 1, @event.Z + 1);
            }
        }
    }

    public override void onBreak(OnBreakEvent @event)
    {
        base.onBreak(@event);
        if (!@event.World.IsRemote)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
            updateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X - 1, @event.Y, @event.Z);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X + 1, @event.Y, @event.Z);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y, @event.Z - 1);
            NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y, @event.Z + 1);
            if (@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X - 1, @event.Y + 1, @event.Z);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X - 1, @event.Y - 1, @event.Z);
            }

            if (@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X + 1, @event.Y + 1, @event.Z);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X + 1, @event.Y - 1, @event.Z);
            }

            if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y + 1, @event.Z - 1);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y - 1, @event.Z - 1);
            }

            if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y + 1, @event.Z + 1);
            }
            else
            {
                NotifyWireNeighborsOfNeighborChange(@event.World, @event.X, @event.Y - 1, @event.Z + 1);
            }
        }
    }

    private int getMaxCurrentStrength(IBlockReader var1, int var2, int var3, int var4, int var5)
    {
        if (var1.GetBlockId(var2, var3, var4) != id)
        {
            return var5;
        }

        int var6 = var1.GetBlockMeta(var2, var3, var4);
        return var6 > var5 ? var6 : var5;
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            int var6 = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            bool var7 = canPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z));
            if (!var7)
            {
                dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, var6));
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }
            else
            {
                updateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
            }

            base.neighborUpdate(@event);
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

    public override void randomDisplayTick(OnTickEvent @event)
    {
        int var6 = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (var6 > 0)
        {
            double x = @event.X + 0.5D + (@event.World.Random.NextFloat() - 0.5D) * 0.2D;
            double y = @event.Y + 1.0F / 16.0F;
            double z = @event.Z + 0.5D + (@event.World.Random.NextFloat() - 0.5D) * 0.2D;
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

            @event.World.Broadcaster.AddParticle("reddust", x, y, z, xVel, yVle, zVel);
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
