using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRedstoneWire : Block
{
    private static readonly ThreadLocal<bool> s_wiresProvidePower = new(() => true);

    private readonly HashSet<BlockPos> _blocksNeedingUpdate = [];

    public BlockRedstoneWire(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F / 16.0F, 1.0F);

    public override int GetTexture(Side side, int metaD) => TextureId;

    public override Box? GetCollisionShape(IBlockReader reader, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.RedstoneWire;

    public override int GetColorMultiplier(IBlockReader reader, int x, int y, int z) => 8388608;

    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    private void UpdateAndPropagateCurrentStrength(IWorldContext level, int startX, int startY, int startZ)
    {
        CalculateCurrentChanges(level, startX, startY, startZ, -1, -1, -1);

        List<BlockPos> updateList = [.. _blocksNeedingUpdate];
        _blocksNeedingUpdate.Clear();

        foreach (BlockPos pos in updateList)
        {
            level.Broadcaster.NotifyNeighbors(pos.x, pos.y, pos.z, ID);
        }
    }

    private void CalculateCurrentChanges(IWorldContext level, int x, int y, int z, int sourceX, int sourceY, int sourceZ)
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
                    maxCurrent = GetMaxCurrentStrength(level.Reader, nx, y, nz, maxCurrent);
                }

                if (level.Reader.ShouldSuffocate(nx, y, nz))
                {
                    if (!level.Reader.ShouldSuffocate(x, y + 1, z) && (nx != sourceX || nz != sourceZ))
                    {
                        maxCurrent = GetMaxCurrentStrength(level.Reader, nx, y + 1, nz, maxCurrent);
                    }
                }
                else if (nx != sourceX || nz != sourceZ)
                {
                    maxCurrent = GetMaxCurrentStrength(level.Reader, nx, y - 1, nz, maxCurrent);
                }
            }

            if (maxCurrent > 0)
            {
                maxCurrent--;
            }
        }

        if (oldMeta == maxCurrent)
        {
            return;
        }

        level.Writer.SetBlockMeta(x, y, z, maxCurrent);

        level.Broadcaster.SetBlocksDirty(x - 1, y - 1, z - 1, x + 1, y + 1, z + 1);

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x + (dir == 0 ? -1 : dir == 1 ? 1 : 0);
            int nz = z + (dir == 2 ? -1 : dir == 3 ? 1 : 0);
            int ny = y - 1;

            if (level.Reader.ShouldSuffocate(nx, y, nz))
            {
                ny += 2;
            }

            int neighborMax = GetMaxCurrentStrength(level.Reader, nx, y, nz, -1);
            if (neighborMax >= 0 && neighborMax != (maxCurrent > 0 ? maxCurrent - 1 : 0))
            {
                CalculateCurrentChanges(level, nx, y, nz, x, y, z);
            }

            neighborMax = GetMaxCurrentStrength(level.Reader, nx, ny, nz, -1);
            if (neighborMax >= 0 && neighborMax != (maxCurrent > 0 ? maxCurrent - 1 : 0))
            {
                CalculateCurrentChanges(level, nx, ny, nz, x, y, z);
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
        if (level.Reader.GetBlockId(x, y, z) != ID)
        {
            return;
        }

        level.Broadcaster.NotifyNeighbors(x, y, z, ID);
        level.Broadcaster.NotifyNeighbors(x - 1, y, z, ID);
        level.Broadcaster.NotifyNeighbors(x + 1, y, z, ID);
        level.Broadcaster.NotifyNeighbors(x, y, z - 1, ID);
        level.Broadcaster.NotifyNeighbors(x, y, z + 1, ID);
        level.Broadcaster.NotifyNeighbors(x, y - 1, z, ID);
        level.Broadcaster.NotifyNeighbors(x, y + 1, z, ID);
    }

    public override void OnPlaced(OnPlacedEvent @event)
    {
        base.OnPlaced(@event);
        if (@event.World.IsRemote)
        {
            return;
        }

        UpdateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, ID);
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

    public override void OnBreak(OnBreakEvent @event)
    {
        base.OnBreak(@event);
        if (@event.World.IsRemote)
        {
            return;
        }

        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, ID);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, ID);
        UpdateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
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

    private int GetMaxCurrentStrength(IBlockReader reader, int x, int y, int z, int power)
    {
        if (reader.GetBlockId(x, y, z) != ID)
        {
            return power;
        }

        int currentStrength = reader.GetBlockMeta(x, y, z);
        return currentStrength > power ? currentStrength : power;
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        bool placeAt = CanPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z));
        if (!placeAt)
        {
            DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, meta));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            UpdateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
        }

        base.NeighborUpdate(@event);
    }

    public override int GetDroppedItemId(int blockMeta) => Item.Redstone.id;

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side) => s_wiresProvidePower.Value && IsPoweringSide(reader, x, y, z, side);

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        if (!s_wiresProvidePower.Value)
        {
            return false;
        }

        if (reader.GetBlockMeta(x, y, z) == 0)
        {
            return false;
        }

        if (side == 1)
        {
            return true;
        }

        bool connectsMinusX = IsPowerProviderOrWire(reader, x - 1, y, z, 1) || (!reader.ShouldSuffocate(x - 1, y, z) && IsPowerProviderOrWire(reader, x - 1, y - 1, z, -1));
        bool connectsPlusX = IsPowerProviderOrWire(reader, x + 1, y, z, 3) || (!reader.ShouldSuffocate(x + 1, y, z) && IsPowerProviderOrWire(reader, x + 1, y - 1, z, -1));
        bool connectsMinusZ = IsPowerProviderOrWire(reader, x, y, z - 1, 2) || (!reader.ShouldSuffocate(x, y, z - 1) && IsPowerProviderOrWire(reader, x, y - 1, z - 1, -1));
        bool connectsPlusZ = IsPowerProviderOrWire(reader, x, y, z + 1, 0) || (!reader.ShouldSuffocate(x, y, z + 1) && IsPowerProviderOrWire(reader, x, y - 1, z + 1, -1));

        if (!reader.ShouldSuffocate(x, y + 1, z))
        {
            if (reader.ShouldSuffocate(x - 1, y, z) && IsPowerProviderOrWire(reader, x - 1, y + 1, z, -1))
            {
                connectsMinusX = true;
            }

            if (reader.ShouldSuffocate(x + 1, y, z) && IsPowerProviderOrWire(reader, x + 1, y + 1, z, -1))
            {
                connectsPlusX = true;
            }

            if (reader.ShouldSuffocate(x, y, z - 1) && IsPowerProviderOrWire(reader, x, y + 1, z - 1, -1))
            {
                connectsMinusZ = true;
            }

            if (reader.ShouldSuffocate(x, y, z + 1) && IsPowerProviderOrWire(reader, x, y + 1, z + 1, -1))
            {
                connectsPlusZ = true;
            }
        }

        return (!connectsMinusZ && !connectsPlusX && !connectsMinusX && !connectsPlusZ && side is >= 2 and <= 5) ||
               (side == 2 && connectsMinusZ && !connectsMinusX && !connectsPlusX) ||
               (side == 3 && connectsPlusZ && !connectsMinusX && !connectsPlusX) ||
               (side == 4 && connectsMinusX && !connectsMinusZ && !connectsPlusZ) ||
               (side == 5 && connectsPlusX && !connectsMinusZ && !connectsPlusZ);
    }

    public override bool CanEmitRedstonePower() => s_wiresProvidePower.Value;

    public override void RandomDisplayTick(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (meta <= 0)
        {
            return;
        }

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

    public static bool IsPowerProviderOrWire(IBlockReader reader, int x, int y, int z, int direction)
    {
        int blockId = reader.GetBlockId(x, y, z);
        if (blockId == 0)
        {
            return false;
        }

        if (blockId == RedstoneWire.ID)
        {
            return true;
        }

        if (blockId == StonePressurePlate.ID ||
            blockId == WoodenPressurePlate.ID ||
            blockId == Button.ID ||
            blockId == Lever.ID)
        {
            return true;
        }

        if (blockId != Repeater.ID && blockId != PoweredRepeater.ID)
        {
            return Blocks[blockId].CanEmitRedstonePower();
        }

        if (direction < 0)
        {
            return false;
        }

        int meta = reader.GetBlockMeta(x, y, z);
        int orientation = meta & 3;
        int opposite = (orientation + 2) & 3;
        return direction == orientation || direction == opposite;
    }
}
