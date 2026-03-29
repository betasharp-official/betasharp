using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockRail : Block
{
    private readonly bool _alwaysStraight;
    public override PistonBehavior PistonBehavior => 0;
    public BlockRail(int id, int textureId, bool alwaysStraight) : base(id, textureId, Material.PistonBreakable)
    {
        _alwaysStraight = alwaysStraight;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
    }

    public static bool IsRail(IWorldContext level, int x, int y, int z)
    {
        int blockId = level.Reader.GetBlockId(x, y, z);
        return blockId == Rail.Id || blockId == PoweredRail.Id || blockId == DetectorRail.Id;
    }

    public static bool IsRail(int blockId) => blockId == Rail.Id || blockId == PoweredRail.Id || blockId == DetectorRail.Id;

    public bool IsAlwaysStraight() => _alwaysStraight;

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque => false;

    public override HitResult Raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        UpdateBoundingBox(world, x, y, z);
        return base.Raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        if (meta is >= 2 and <= 5)
        {
            SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 10.0F / 16.0F, 1.0F);
        }
        else
        {
            SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
        }
    }

    public override int GetTexture(Side side, int meta)
    {
        if (_alwaysStraight && Id == PoweredRail.Id && (meta & 8) == 0 || meta >= 6)
        {
            return TextureId - 16;
        }

        return TextureId;
    }

    public override bool IsFullCube => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.MinecartTrack;

    public override bool CanPlaceAt(CanPlaceAtContext evt) => evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z);

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.World.IsRemote) return;

        UpdateShape(@event.World, @event.X, @event.Y, @event.Z, true);
        if (Id != PoweredRail.Id) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        NeighborUpdate(new OnTickEvent(@event.World, @event.X, @event.Y, @event.Z, meta, Id));
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        int railMeta = meta;
        if (_alwaysStraight)
        {
            railMeta = meta & 7;
        }

        bool shouldBreak = !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) ||
                           (railMeta == 2 && !@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z)) ||
                           (railMeta == 3 && !@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z)) ||
                           (railMeta == 4 && !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1)) ||
                           (railMeta == 5 && !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1));

        if (shouldBreak)
        {
            DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else if (Id == PoweredRail.Id)
        {
            bool isPowered = @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) || @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z);
            isPowered = isPowered || IsPoweredByConnectedRails(@event.World, @event.X, @event.Y, @event.Z, meta, true, 0) || IsPoweredByConnectedRails(@event.World, @event.X, @event.Y, @event.Z, meta, false, 0);
            bool stateChanged = false;
            switch (isPowered)
            {
                case true when (meta & 8) == 0:
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, railMeta | 8);
                    stateChanged = true;
                    break;
                case false when (meta & 8) != 0:
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, railMeta);
                    stateChanged = true;
                    break;
            }

            if (stateChanged)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, Id);
            }
        }
        else if (Id > 0 && Blocks[Id]!.CanEmitRedstonePower && !_alwaysStraight && RailLogic.GetNAdjacentTracks(new RailLogic(this, @event.World, new Vec3i(@event.X, @event.Y, @event.Z))) == 3)
        {
            UpdateShape(@event.World, @event.X, @event.Y, @event.Z, false);
        }
    }

    private void UpdateShape(IWorldContext level, int x, int y, int z, bool force)
    {
        if (level.IsRemote) return;

        new RailLogic(this, level, new Vec3i(x, y, z)).UpdateState(level.Redstone.IsPowered(x, y, z), force);
    }

    private bool IsPoweredByConnectedRails(IWorldContext level, int x, int y, int z, int meta, bool towardsNegative, int depth)
    {
        if (depth >= 8) return false;

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

        return IsPoweredByRail(level, x, y, z, towardsNegative, depth, shape) || (isSameY &&
                                                                                  IsPoweredByRail(level, x, y - 1, z, towardsNegative, depth, shape));
    }

    private bool IsPoweredByRail(IWorldContext level, int x, int y, int z, bool towardsNegative, int depth, int shape)
    {
        if (level.Reader.GetBlockId(x, y, z) != PoweredRail.Id) return false;

        int meta = level.Reader.GetBlockMeta(x, y, z);
        int railMeta = meta & 7;

        if ((shape == 1 && railMeta is 0 or 4 or 5) || (shape == 0 && railMeta is 1 or 2 or 3)) return false;
        if ((meta & 8) == 0)  return false;

        if (!level.Redstone.IsPowered(x, y, z) && !level.Redstone.IsPowered(x, y + 1, z))
        {
            return IsPoweredByConnectedRails(level, x, y, z, meta, towardsNegative, depth + 1);
        }

        return true;
    }
}
