using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRedstoneWire : Block
{
    private static readonly ThreadLocal<bool> s_wiresProvidePower = new(() => true);

    private readonly HashSet<BlockPos> _blocksNeedingUpdate = [];

    public BlockRedstoneWire(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F / 16.0F, 1.0F);

    public override int GetTexture(Side side, int metaD) => TextureId;

    public override Box? getCollisionShape(IBlockReader reader, EntityManager entities, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.RedstoneWire;

    public override int getColorMultiplier(IBlockReader reader, int x, int y, int z) => 8388608;

    public override bool canPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    private void updateAndPropagateCurrentStrength(IWorldContext level, int startX, int startY, int startZ)
    {
        calculateCurrentChanges(level, startX, startY, startZ, -1, -1, -1);

        List<BlockPos> updateList = [.. _blocksNeedingUpdate];
        _blocksNeedingUpdate.Clear();

        foreach (BlockPos pos in updateList)
        {
            level.Broadcaster.NotifyNeighbors(pos.x, pos.y, pos.z, id);
        }
    }

    private void calculateCurrentChanges(IWorldContext level, int x, int y, int z, int sourceX, int sourceY, int sourceZ)
    {
        int oldMeta = level.Reader.GetBlockMeta(x, y, z);

        s_wiresProvidePower.Value = false;
        bool isIndirectlyPowered = level.Redstone.IsPowered(x, y, z);
        s_wiresProvidePower.Value = true;

        int maxCurrent = 0;
        if (isIndirectlyPowered)
        {
            maxCurrent = 15;
        }
        else
        {
            for (int dir = 0; dir < 4; dir++)
            {
                int nx = x + (dir == 0 ? -1 : dir == 1 ? 1 : 0);
                int nz = z + (dir == 2 ? -1 : dir == 3 ? 1 : 0);

                if (nx != sourceX || nz != sourceZ)
                {
                    maxCurrent = getMaxCurrentStrength(level.Reader, nx, y, nz, maxCurrent);
                }

                if (level.Reader.ShouldSuffocate(nx, y, nz))
                {
                    if (!level.Reader.ShouldSuffocate(x, y + 1, z) && (nx != sourceX || nz != sourceZ))
                    {
                        maxCurrent = getMaxCurrentStrength(level.Reader, nx, y + 1, nz, maxCurrent);
                    }
                }
                else if (nx != sourceX || nz != sourceZ)
                {
                    maxCurrent = getMaxCurrentStrength(level.Reader, nx, y - 1, nz, maxCurrent);
                }
            }

            if (maxCurrent > 0) maxCurrent--;
        }

        if (oldMeta == maxCurrent) return;

        level.Writer.SetBlockMeta(x, y, z, maxCurrent);

        level.Broadcaster.SetBlocksDirty(x - 1, y - 1, z - 1, x + 1, y + 1, z + 1);

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x + (dir == 0 ? -1 : dir == 1 ? 1 : 0);
            int nz = z + (dir == 2 ? -1 : dir == 3 ? 1 : 0);
            int ny = y - 1;

            if (level.Reader.ShouldSuffocate(nx, y, nz)) ny += 2;

            int neighborMax = getMaxCurrentStrength(level.Reader, nx, y, nz, -1);
            if (neighborMax >= 0 && neighborMax != (maxCurrent > 0 ? maxCurrent - 1 : 0))
            {
                calculateCurrentChanges(level, nx, y, nz, x, y, z);
            }

            neighborMax = getMaxCurrentStrength(level.Reader, nx, ny, nz, -1);
            if (neighborMax >= 0 && neighborMax != (maxCurrent > 0 ? maxCurrent - 1 : 0))
            {
                calculateCurrentChanges(level, nx, ny, nz, x, y, z);
            }
        }

        if (oldMeta == 0 || maxCurrent == 0)
        {
            _blocksNeedingUpdate.Add(new BlockPos(x, y, z));
            _blocksNeedingUpdate.Add(new BlockPos(x - 1, y, z));
            _blocksNeedingUpdate.Add(new BlockPos(x + 1, y, z));
            _blocksNeedingUpdate.Add(new BlockPos(x, y - 1, z));
            _blocksNeedingUpdate.Add(new BlockPos(x, y + 1, z));
            _blocksNeedingUpdate.Add(new BlockPos(x, y, z - 1));
            _blocksNeedingUpdate.Add(new BlockPos(x, y, z + 1));
        }
    }

    private void NotifyWireNeighborsOfNeighborChange(IWorldContext level, int x, int y, int z)
    {
        if (level.Reader.GetBlockId(x, y, z) != id) return;

        level.Broadcaster.NotifyNeighbors(x, y, z, id);
        level.Broadcaster.NotifyNeighbors(x - 1, y, z, id);
        level.Broadcaster.NotifyNeighbors(x + 1, y, z, id);
        level.Broadcaster.NotifyNeighbors(x, y, z - 1, id);
        level.Broadcaster.NotifyNeighbors(x, y, z + 1, id);
        level.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
        level.Broadcaster.NotifyNeighbors(x, y + 1, z, id);
    }

    public override void onPlaced(OnPlacedEvent @event)
    {
        base.onPlaced(@event);
        if (@event.World.IsRemote) return;

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

    public override void onBreak(OnBreakEvent @event)
    {
        base.onBreak(@event);
        if (@event.World.IsRemote) return;

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

    private int getMaxCurrentStrength(IBlockReader reader, int x, int y, int z, int power)
    {
        if (reader.GetBlockId(x, y, z) != id) return power;
        int currentStrength = reader.GetBlockMeta(x, y, z);
        return currentStrength > power ? currentStrength : power;
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        bool placeAt = canPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z));
        if (!placeAt)
        {
            dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, meta));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            updateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
        }

        base.neighborUpdate(@event);
    }

    public override int getDroppedItemId(int blockMeta) => Item.Redstone.id;

    public override bool isStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side) => s_wiresProvidePower.Value && isPoweringSide(reader, x, y, z, side);

    public override bool isPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        if (!s_wiresProvidePower.Value) return false;
        if (reader.GetBlockMeta(x, y, z) == 0) return false;
        if (side == 1) return true;

        bool connectsMinusX = isPowerProviderOrWire(reader, x - 1, y, z, 1) || (!reader.ShouldSuffocate(x - 1, y, z) && isPowerProviderOrWire(reader, x - 1, y - 1, z, -1));
        bool connectsPlusX = isPowerProviderOrWire(reader, x + 1, y, z, 3) || (!reader.ShouldSuffocate(x + 1, y, z) && isPowerProviderOrWire(reader, x + 1, y - 1, z, -1));
        bool connectsMinusZ = isPowerProviderOrWire(reader, x, y, z - 1, 2) || (!reader.ShouldSuffocate(x, y, z - 1) && isPowerProviderOrWire(reader, x, y - 1, z - 1, -1));
        bool connectsPlusZ = isPowerProviderOrWire(reader, x, y, z + 1, 0) || (!reader.ShouldSuffocate(x, y, z + 1) && isPowerProviderOrWire(reader, x, y - 1, z + 1, -1));

        if (!reader.ShouldSuffocate(x, y + 1, z))
        {
            if (reader.ShouldSuffocate(x - 1, y, z) && isPowerProviderOrWire(reader, x - 1, y + 1, z, -1)) connectsMinusX = true;
            if (reader.ShouldSuffocate(x + 1, y, z) && isPowerProviderOrWire(reader, x + 1, y + 1, z, -1)) connectsPlusX = true;
            if (reader.ShouldSuffocate(x, y, z - 1) && isPowerProviderOrWire(reader, x, y + 1, z - 1, -1)) connectsMinusZ = true;
            if (reader.ShouldSuffocate(x, y, z + 1) && isPowerProviderOrWire(reader, x, y + 1, z + 1, -1)) connectsPlusZ = true;
        }

        return !connectsMinusZ && !connectsPlusX && !connectsMinusX && !connectsPlusZ && side is >= 2 and <= 5 ||
               side == 2 && connectsMinusZ && !connectsMinusX && !connectsPlusX ||
               side == 3 && connectsPlusZ && !connectsMinusX && !connectsPlusX ||
               side == 4 && connectsMinusX && !connectsMinusZ && !connectsPlusZ ||
               side == 5 && connectsPlusX && !connectsMinusZ && !connectsPlusZ;
    }

    public override bool canEmitRedstonePower() => s_wiresProvidePower.Value;

    public override void randomDisplayTick(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (meta <= 0) return;

        double x = @event.X + 0.5D + (@event.World.Random.NextFloat() - 0.5D) * 0.2D;
        double y = @event.Y + 1.0F / 16.0F;
        double z = @event.Z + 0.5D + (@event.World.Random.NextFloat() - 0.5D) * 0.2D;
        float powerRatio = meta / 15.0F;
        float xVel = powerRatio * 0.6F + 0.4F;

        float yVle = powerRatio * powerRatio * 0.7F - 0.5F;
        float zVel = powerRatio * powerRatio * 0.6F - 0.7F;
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

    public static bool isPowerProviderOrWire(IBlockReader reader, int x, int y, int z, int direction)
    {
        int blockId = reader.GetBlockId(x, y, z);
        if (blockId == 0) return false;
        if (blockId == RedstoneWire.id) return true;
        if (blockId == StonePressurePlate.id ||
            blockId == WoodenPressurePlate.id ||
            blockId == Button.id ||
            blockId == Lever.id)
            return true;

        if (blockId != Repeater.id && blockId != PoweredRepeater.id) return Blocks[blockId].canEmitRedstonePower();

        if (direction < 0) return false;
        int meta = reader.GetBlockMeta(x, y, z);
        int orientation = meta & 3;
        int opposite = (orientation + 2) & 3;
        return direction == orientation || direction == opposite;
    }
}
