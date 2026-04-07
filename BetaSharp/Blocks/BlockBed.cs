using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    public static readonly Side[][] BedFacings =
    [
        [Side.Up, Side.Down, Side.South, Side.North, Side.East, Side.West],
        [Side.Up, Side.Down, Side.East, Side.West, Side.North, Side.South],
        [Side.Up, Side.Down, Side.North, Side.South, Side.West, Side.East],
        [Side.Up, Side.Down, Side.West, Side.East, Side.South, Side.North]
    ];

    private static readonly int[][] s_bedOffsets = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public BlockBed(int id) : base(id, 134, Material.Wool)
    {
        setDefaultShape();
    }

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return true;
        }

        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);

        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        if (!isHeadOfBed(meta))
        {
            int direction = getDirection(meta);
            x += s_bedOffsets[direction][0];
            z += s_bedOffsets[direction][1];

            if (@event.World.Reader.GetBlockId(x, y, z) != id)
            {
                return true;
            }

            meta = @event.World.Reader.GetBlockMeta(x, y, z);
        }

        if (!@event.World.Dimension.HasWorldSpawn)
        {
            double posX = x + 0.5D;
            double posY = y + 0.5D;
            double posZ = z + 0.5D;
            @event.World.Writer.SetBlock(x, y, z, 0);

            int direction = getDirection(meta);
            x += s_bedOffsets[direction][0];
            z += s_bedOffsets[direction][1];

            if (@event.World.Reader.GetBlockId(x, y, z) == id)
            {
                @event.World.Writer.SetBlock(x, y, z, 0);
                posX = (posX + x + 0.5D) / 2.0D;
                posY = (posY + y + 0.5D) / 2.0D;
                posZ = (posZ + z + 0.5D) / 2.0D;
            }

            @event.World.CreateExplosion(null, x + 0.5F, y + 0.5F, z + 0.5F, 5.0F, true);
            return true;
        }

        if (isBedOccupied(meta))
        {
            EntityPlayer? occupant = null;
            foreach (EntityPlayer otherPlayer in @event.World.Entities.Players)
            {
                if (!otherPlayer.isSleeping()) continue;

                Vec3i sleepingPos = otherPlayer.sleepingPos;
                if (sleepingPos.X == x && sleepingPos.Y == y && sleepingPos.Z == z)
                {
                    occupant = otherPlayer;
                }
            }

            if (occupant != null)
            {
                @event.Player.sendMessage("tile.bed.occupied");
                return true;
            }

            updateState(@event.World.Writer, x, y, z, meta, false);
        }

        SleepAttemptResult result = @event.Player.trySleep(x, y, z);
        switch (result)
        {
            case SleepAttemptResult.OK:
                updateState(@event.World.Writer, x, y, z, meta, true);
                return true;
            case SleepAttemptResult.NOT_POSSIBLE_NOW:
                @event.Player.sendMessage("tile.bed.noSleep");
                break;
            case SleepAttemptResult.NOT_POSSIBLE_HERE:
                break;
            case SleepAttemptResult.TOO_FAR_AWAY:
                break;
            case SleepAttemptResult.OTHER_PROBLEM:
                break;
            default:
                throw new ArgumentException($"Invalid sleep attempt result: {result}");
        }

        return true;
    }

    public override int GetTexture(Side side, int meta)
    {
        int direction = getDirection(meta);
        Side sideFacing = BedFacings[direction][side.ToInt()];
        if (side == Side.Down) return Planks.TextureId;
        if (isHeadOfBed(meta))
        {
            if (sideFacing == Side.North) return TextureId + 2 + 16;
            if (sideFacing != Side.East && sideFacing != Side.West) return TextureId + 1;
            return TextureId + 1 + 16;
        }

        if (sideFacing == Side.South) return TextureId - 1 + 16;
        if (sideFacing != Side.East && sideFacing != Side.West) return TextureId;
        return TextureId + 16;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Bed;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        setDefaultShape();
    }

    public override void neighborUpdate(OnTickEvent ctx)
    {
        int blockMeta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int direction = getDirection(blockMeta);

        if (isHeadOfBed(blockMeta))
        {
            if (ctx.World.Reader.GetBlockId(ctx.X - s_bedOffsets[direction][0], ctx.Y, ctx.Z - s_bedOffsets[direction][1]) != id)
            {
                ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
        else if (ctx.World.Reader.GetBlockId(ctx.X + s_bedOffsets[direction][0], ctx.Y, ctx.Z + s_bedOffsets[direction][1]) != id)
        {
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            if (!ctx.World.IsRemote)
            {
                dropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, blockMeta));
            }
        }
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return isHeadOfBed(blockMeta) ? 0 : Item.Bed.id;
    }

    private void setDefaultShape()
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 9.0F / 16.0F, 1.0F);
    }

    public static int getDirection(int meta)
    {
        return meta & 3;
    }

    public static bool isHeadOfBed(int meta)
    {
        return (meta & 8) != 0;
    }

    public static bool isBedOccupied(int meta)
    {
        return (meta & 4) != 0;
    }

    public static void updateState(IBlockWriter worldWriter, int x, int y, int z, int meta, bool occupied)
    {
        if (occupied)
        {
            meta |= 4;
        }
        else
        {
            meta &= ~4;
        }

        worldWriter.SetBlockMeta(x, y, z, meta);
    }

    public static Vec3i? findWakeUpPosition(IBlockReader reader, int x, int y, int z, int skip)
    {
        int blockMeta = reader.GetBlockMeta(x, y, z);
        int direction = getDirection(blockMeta);

        if (isHeadOfBed(blockMeta))
        {
            x -= s_bedOffsets[direction][0];
            z -= s_bedOffsets[direction][1];
        }

        for (int bedHalf = 0; bedHalf <= 1; ++bedHalf)
        {
            int centerX = x + s_bedOffsets[direction][0] * bedHalf;
            int centerZ = z + s_bedOffsets[direction][1] * bedHalf;

            int searchMinX = centerX - 1;
            int searchMinZ = centerZ - 1;
            int searchMaxX = centerX + 1;
            int searchMaxZ = centerZ + 1;

            for (int checkX = searchMinX; checkX <= searchMaxX; ++checkX)
            {
                for (int checkZ = searchMinZ; checkZ <= searchMaxZ; ++checkZ)
                {
                    if (reader.ShouldSuffocate(checkX, y - 1, checkZ) &&
                        reader.IsAir(checkX, y, checkZ) &&
                        reader.IsAir(checkX, y + 1, checkZ))
                    {
                        if (skip <= 0)
                        {
                            return new Vec3i(checkX, y, checkZ);
                        }

                        --skip;
                    }
                }
            }
        }

        return null;
    }

    public override void dropStacks(OnDropEvent @event)
    {
        if (!isHeadOfBed(@event.Meta))
        {
            base.dropStacks(@event);
        }
    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
