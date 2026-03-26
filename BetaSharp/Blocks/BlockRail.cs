using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRail : Block
{
    private readonly bool alwaysStraight;

    public BlockRail(int id, int textureId, bool alwaysStraight) : base(id, textureId, Material.PistonBreakable)
    {
        this.alwaysStraight = alwaysStraight;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
    }

    public static bool isRail(IWorldContext level, int x, int y, int z)
    {
        int blockId = level.Reader.GetBlockId(x, y, z);
        return blockId == Rail.id || blockId == PoweredRail.id || blockId == DetectorRail.id;
    }

    public static bool isRail(int blockId) => blockId == Rail.id || blockId == PoweredRail.id || blockId == DetectorRail.id;

    public bool isAlwaysStraight() => alwaysStraight;

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        if (meta >= 2 && meta <= 5)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 10.0F / 16.0F, 1.0F);
        }
        else
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
        }
    }

    public override int getTexture(int side, int meta)
    {
        if (alwaysStraight)
        {
            if (id == PoweredRail.id && (meta & 8) == 0)
            {
                return textureId - 16;
            }
        }
        else if (meta >= 6)
        {
            return textureId - 16;
        }

        return textureId;
    }

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.MinecartTrack;

    public override bool canPlaceAt(CanPlaceAtContext evt) => evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z);

    public override void onPlaced(OnPlacedEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            updateShape(@event.World, @event.X, @event.Y, @event.Z, true);
            if (id == PoweredRail.id)
            {
                int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
                neighborUpdate(new OnTickEvent(@event.World, @event.X, @event.Y, @event.Z, meta, id));
            }
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            int railMeta = meta;
            if (alwaysStraight)
            {
                railMeta = meta & 7;
            }

            bool shouldBreak = false;
            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z))
            {
                shouldBreak = true;
            }

            if (railMeta == 2 && !@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
            {
                shouldBreak = true;
            }

            if (railMeta == 3 && !@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
            {
                shouldBreak = true;
            }

            if (railMeta == 4 && !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
            {
                shouldBreak = true;
            }

            if (railMeta == 5 && !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
            {
                shouldBreak = true;
            }

            if (shouldBreak)
            {
                dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }
            else if (id == PoweredRail.id)
            {
                bool isPowered = @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) || @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z);
                isPowered = isPowered || isPoweredByConnectedRails(@event.World, @event.X, @event.Y, @event.Z, meta, true, 0) || isPoweredByConnectedRails(@event.World, @event.X, @event.Y, @event.Z, meta, false, 0);
                bool stateChanged = false;
                if (isPowered && (meta & 8) == 0)
                {
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, railMeta | 8);
                    stateChanged = true;
                }
                else if (!isPowered && (meta & 8) != 0)
                {
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, railMeta);
                    stateChanged = true;
                }

                if (stateChanged)
                {
                    @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, id);
                }
            }
            else if (id > 0 && Blocks[id].canEmitRedstonePower() && !alwaysStraight && RailLogic.GetNAdjacentTracks(new RailLogic(this, @event.World, new Vec3i(@event.X, @event.Y, @event.Z))) == 3)
            {
                updateShape(@event.World, @event.X, @event.Y, @event.Z, false);
            }
        }
    }

    private void updateShape(IWorldContext level, int x, int y, int z, bool force)
    {
        if (!level.IsRemote)
        {
            new RailLogic(this, level, new Vec3i(x, y, z)).UpdateState(level.Redstone.IsPowered(x, y, z), force);
        }
    }

    private bool isPoweredByConnectedRails(IWorldContext level, int x, int y, int z, int meta, bool towardsNegative, int depth)
    {
        if (depth >= 8)
        {
            return false;
        }

        int shape = meta & 7;
        bool isSameY = true;
        switch (shape)
        {
            case 0:
                if (towardsNegative)
                {
                    ++z;
                }
                else
                {
                    --z;
                }

                break;
            case 1:
                if (towardsNegative)
                {
                    --x;
                }
                else
                {
                    ++x;
                }

                break;
            case 2:
                if (towardsNegative)
                {
                    --x;
                }
                else
                {
                    ++x;
                    ++y;
                    isSameY = false;
                }

                shape = 1;
                break;
            case 3:
                if (towardsNegative)
                {
                    --x;
                    ++y;
                    isSameY = false;
                }
                else
                {
                    ++x;
                }

                shape = 1;
                break;
            case 4:
                if (towardsNegative)
                {
                    ++z;
                }
                else
                {
                    --z;
                    ++y;
                    isSameY = false;
                }

                shape = 0;
                break;
            case 5:
                if (towardsNegative)
                {
                    ++z;
                    ++y;
                    isSameY = false;
                }
                else
                {
                    --z;
                }

                shape = 0;
                break;
        }

        return isPoweredByRail(level, x, y, z, towardsNegative, depth, shape) ? true : isSameY && isPoweredByRail(level, x, y - 1, z, towardsNegative, depth, shape);
    }

    private bool isPoweredByRail(IWorldContext level, int x, int y, int z, bool towardsNegative, int depth, int shape)
    {
        int blockId = level.Reader.GetBlockId(x, y, z);
        if (blockId == PoweredRail.id)
        {
            int meta = level.Reader.GetBlockMeta(x, y, z);
            int railMeta = meta & 7;
            if (shape == 1 && (railMeta == 0 || railMeta == 4 || railMeta == 5))
            {
                return false;
            }

            if (shape == 0 && (railMeta == 1 || railMeta == 2 || railMeta == 3))
            {
                return false;
            }

            if ((meta & 8) != 0)
            {
                if (!level.Redstone.IsPowered(x, y, z) && !level.Redstone.IsPowered(x, y + 1, z))
                {
                    return isPoweredByConnectedRails(level, x, y, z, meta, towardsNegative, depth + 1);
                }

                return true;
            }
        }

        return false;
    }

    public override int getPistonBehavior() => 0;
}
