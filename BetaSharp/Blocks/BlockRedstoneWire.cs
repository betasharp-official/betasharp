using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRedstoneWire : Block
{
    private static readonly ThreadLocal<bool> s_wiresProvidePower = new(() => true);

    public BlockRedstoneWire(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F / 16.0F, 1.0F);

    public override int GetTexture(Side side, int meta) => TextureId;

    public override Box? GetCollisionShape(IBlockReader reader, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque => false;

    public override bool IsFullCube => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.RedstoneWire;

    public override int GetColorMultiplier(IBlockReader reader, int x, int y, int z) => 0x800000;

    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    private void UpdateAndPropagateCurrentStrength(IWorldContext level, int x, int y, int z)
    {
        HashSet<BlockPos> neighbors = [];
        CalculateCurrentChanges(level, x, y, z, x, y, z, neighbors);

        List<BlockPos> neighborsCopy = [.. neighbors];
        neighbors.Clear();

        foreach (BlockPos n in neighborsCopy)
        {
            level.Broadcaster.NotifyNeighbors(n.x, n.y, n.z, Id);
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

                switch (dir)
                {
                    case 0:
                        nx = x - 1;
                        break;
                    case 1:
                        ++nx;
                        break;
                    case 2:
                        nz = z - 1;
                        break;
                    case 3:
                        ++nz;
                        break;
                }

                if (nx != srcX || y != srcY || nz != srcZ)
                {
                    newMeta = GetMaxCurrentStrength(level.Reader, nx, y, nz, newMeta);
                }

                if (level.Reader.ShouldSuffocate(nx, y, nz) && !level.Reader.ShouldSuffocate(x, y + 1, z))
                {
                    if (nx != srcX || y + 1 != srcY || nz != srcZ)
                    {
                        newMeta = GetMaxCurrentStrength(level.Reader, nx, y + 1, nz, newMeta);
                    }
                }
                else if (!level.Reader.ShouldSuffocate(nx, y, nz) && (nx != srcX || y - 1 != srcY || nz != srcZ))
                {
                    newMeta = GetMaxCurrentStrength(level.Reader, nx, y - 1, nz, newMeta);
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

        if (oldMeta == newMeta) return;


        level.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, newMeta);
        level.Broadcaster.BlockUpdateEvent(x, y, z);
        level.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);

        for (int dir = 0; dir < 4; ++dir)
        {
            int nx = x;
            int nz = z;
            int ny = y - 1;

            switch (dir)
            {
                case 0:
                    nx = x - 1;
                    break;
                case 1:
                    ++nx;
                    break;
                case 2:
                    nz = z - 1;
                    break;
                case 3:
                    ++nz;
                    break;
            }

            if (level.Reader.ShouldSuffocate(nx, y, nz))
            {
                ny += 2;
            }

            int neighborMeta = GetMaxCurrentStrength(level.Reader, nx, y, nz, -1);
            newMeta = level.Reader.GetBlockMeta(x, y, z);
            if (newMeta > 0)
            {
                --newMeta;
            }

            if (neighborMeta >= 0 && neighborMeta != newMeta)
            {
                CalculateCurrentChanges(level, nx, y, nz, x, y, z, neighbors);
            }

            neighborMeta = GetMaxCurrentStrength(level.Reader, nx, ny, nz, -1);
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

        if (oldMeta != 0 && newMeta != 0) return;

        neighbors.Add(new BlockPos(x, y, z));
        neighbors.Add(new BlockPos(x - 1, y, z));
        neighbors.Add(new BlockPos(x + 1, y, z));
        neighbors.Add(new BlockPos(x, y - 1, z));
        neighbors.Add(new BlockPos(x, y + 1, z));
        neighbors.Add(new BlockPos(x, y, z - 1));
        neighbors.Add(new BlockPos(x, y, z + 1));
    }

    private void NotifyWireNeighborsOfNeighborChange(IWorldContext level, int x, int y, int z)
    {
        if (level.Reader.GetBlockId(x, y, z) != Id) return;

        level.Broadcaster.NotifyNeighbors(x, y, z, Id);
        level.Broadcaster.NotifyNeighbors(x - 1, y, z, Id);
        level.Broadcaster.NotifyNeighbors(x + 1, y, z, Id);
        level.Broadcaster.NotifyNeighbors(x, y, z - 1, Id);
        level.Broadcaster.NotifyNeighbors(x, y, z + 1, Id);
        level.Broadcaster.NotifyNeighbors(x, y - 1, z, Id);
        level.Broadcaster.NotifyNeighbors(x, y + 1, z, Id);
    }

    public override void OnPlaced(OnPlacedEvent @event)
    {
        base.OnPlaced(@event);
        if (@event.World.IsRemote) return;

        UpdateAndPropagateCurrentStrength(@event.World, @event.X, @event.Y, @event.Z);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
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
        if (@event.World.IsRemote) return;

        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
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

    private int GetMaxCurrentStrength(IBlockReader reader, int x, int y, int z, int currentStrength)
    {
        if (reader.GetBlockId(x, y, z) != Id) return currentStrength;

        int meta = reader.GetBlockMeta(x, y, z);
        return meta > currentStrength ? meta : currentStrength;
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        bool canPlace = CanPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z));
        if (!canPlace)
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

    public override int GetDroppedItemId(int blockId) => Item.Redstone.id;

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int facing) => s_wiresProvidePower.Value &&
                                                                                                       IsPoweringSide(reader, x, y, z, facing);

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int facing)
    {
        if (!s_wiresProvidePower.Value) return false;

        if (reader.GetBlockMeta(x, y, z) == 0) return false;

        if (facing == Side.Up.ToInt()) return true;

        bool north = IsPowerProviderOrWire(reader, x - 1, y, z, 1) || (!reader.ShouldSuffocate(x - 1, y, z) && IsPowerProviderOrWire(reader, x - 1, y - 1, z, -1));
        bool south = IsPowerProviderOrWire(reader, x + 1, y, z, 3) || (!reader.ShouldSuffocate(x + 1, y, z) && IsPowerProviderOrWire(reader, x + 1, y - 1, z, -1));
        bool west = IsPowerProviderOrWire(reader, x, y, z - 1, 2) || (!reader.ShouldSuffocate(x, y, z - 1) && IsPowerProviderOrWire(reader, x, y - 1, z - 1, -1));
        bool east = IsPowerProviderOrWire(reader, x, y, z + 1, 0) || (!reader.ShouldSuffocate(x, y, z + 1) && IsPowerProviderOrWire(reader, x, y - 1, z + 1, -1));
        if (reader.ShouldSuffocate(x, y + 1, z))
        {
            return (!west && !south && !north && !east && facing is >= 2 and <= 5) ||
                   (facing == 2 && west && !north && !south) ||
                   (facing == 3 && east && !north && !south) ||
                   (facing == 4 && north && !west && !east) ||
                   (facing == 5 && south && !west && !east);
        }

        if (reader.ShouldSuffocate(x - 1, y, z) && IsPowerProviderOrWire(reader, x - 1, y + 1, z, -1))
        {
            north = true;
        }

        if (reader.ShouldSuffocate(x + 1, y, z) && IsPowerProviderOrWire(reader, x + 1, y + 1, z, -1))
        {
            south = true;
        }

        if (reader.ShouldSuffocate(x, y, z - 1) && IsPowerProviderOrWire(reader, x, y + 1, z - 1, -1))
        {
            west = true;
        }

        if (reader.ShouldSuffocate(x, y, z + 1) && IsPowerProviderOrWire(reader, x, y + 1, z + 1, -1))
        {
            east = true;
        }

        return (!west && !south && !north && !east && facing is >= 2 and <= 5) ||
               (facing == 2 && west && !north && !south) ||
               (facing == 3 && east && !north && !south) ||
               (facing == 4 && north && !west && !east) ||
               (facing == 5 && south && !west && !east);
    }

    public override bool CanEmitRedstonePower => s_wiresProvidePower.Value;

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
        float strength = meta / 15.0F;
        float xVel = strength * 0.6F + 0.4F;

        float yVle = strength * strength * 0.7F - 0.5F;
        float zVel = strength * strength * 0.6F - 0.7F;
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

    public static bool IsPowerProviderOrWire(IBlockReader reader, int x, int y, int z, int facing)
    {
        int blockId = reader.GetBlockId(x, y, z);
        if (blockId == RedstoneWire.Id)
        {
            return true;
        }

        if (blockId == 0)
        {
            return false;
        }

        if (Blocks[blockId]!.CanEmitRedstonePower)
        {
            return true;
        }

        if (blockId != Repeater.Id && blockId != PoweredRepeater.Id)
        {
            return false;
        }

        int meta = reader.GetBlockMeta(x, y, z);
        return facing == Facings.OppositeHorizontalDir(meta & 3);
    }
}
