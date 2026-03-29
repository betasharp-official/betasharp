using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    private static readonly int[][] s_bedOffsets = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public override bool IsFullCube => false;

    public override bool IsOpaque => false;

    public override PistonBehavior PistonBehavior => PistonBehavior.Destroy;

    public BlockBed(int id) : base(id, 134, Material.Wool) => SetDefaultShape();

    public override bool OnUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote) return true;

        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);

        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        if (!IsHeadOfBed(meta))
        {
            int direction = GetDirection(meta);
            x += s_bedOffsets[direction][0];
            z += s_bedOffsets[direction][1];

            if (@event.World.Reader.GetBlockId(x, y, z) != Id) return true;

            meta = @event.World.Reader.GetBlockMeta(x, y, z);
        }

        if (!@event.World.Dimension.HasWorldSpawn)
        {
            @event.World.Writer.SetBlock(x, y, z, 0);

            int direction = GetDirection(meta);
            x += s_bedOffsets[direction][0];
            z += s_bedOffsets[direction][1];

            if (@event.World.Reader.GetBlockId(x, y, z) == Id)
            {
                @event.World.Writer.SetBlock(x, y, z, 0);
            }

            @event.World.CreateExplosion(null, x + 0.5F, y + 0.5F, z + 0.5F, 5.0F, true);
            return true;
        }

        if (IsBedOccupied(meta))
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

            UpdateState(@event.World.Writer, x, y, z, meta, false);
        }

        SleepAttemptResult result = @event.Player.trySleep(x, y, z);
        switch (result)
        {
            case SleepAttemptResult.OK:
                UpdateState(@event.World.Writer, x, y, z, meta, true);
                break;
            case SleepAttemptResult.NOT_POSSIBLE_NOW:
                @event.Player.sendMessage("tile.bed.noSleep");
                break;
            case SleepAttemptResult.NOT_POSSIBLE_HERE:
            case SleepAttemptResult.TOO_FAR_AWAY:
            case SleepAttemptResult.OTHER_PROBLEM:
                break;
            default:
                throw new ArgumentException($"Invalid sleep attempt result: {result}");
        }

        return true;
    }

    public override int GetTexture(Side side, int meta)
    {
        if (side == Side.Down) return Planks.TextureId;

        int direction = GetDirection(meta);
        Side sideFacing = Facings.BedFacings[direction][side.ToInt()];

        if (IsHeadOfBed(meta))
        {
            return sideFacing switch
            {
                Side.North => TextureId + 2 + 16,
                Side.East or Side.West => TextureId + 1 + 16,
                _ => TextureId + 1
            };
        }

        return sideFacing switch
        {
            Side.South => TextureId - 1 + 16,
            Side.East or Side.West => TextureId + 16,
            _ => TextureId
        };
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.Bed;


    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => SetDefaultShape();

    public override void NeighborUpdate(OnTickEvent ctx)
    {
        int blockMeta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int direction = GetDirection(blockMeta);

        if (IsHeadOfBed(blockMeta))
        {
            if (ctx.World.Reader.GetBlockId(ctx.X - s_bedOffsets[direction][0], ctx.Y, ctx.Z - s_bedOffsets[direction][1]) != Id)
            {
                ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
        else if (ctx.World.Reader.GetBlockId(ctx.X + s_bedOffsets[direction][0], ctx.Y, ctx.Z + s_bedOffsets[direction][1]) != Id)
        {
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            if (!ctx.World.IsRemote)
            {
                DropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, blockMeta));
            }
        }
    }

    public override int GetDroppedItemId(int blockMeta) => IsHeadOfBed(blockMeta) ? 0 : Item.Bed.id;

    private void SetDefaultShape() => SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 9.0F / 16.0F, 1.0F);

    public static int GetDirection(int meta) => meta & 3;

    public static bool IsHeadOfBed(int meta) => (meta & 8) != 0;

    private static bool IsBedOccupied(int meta) => (meta & 4) != 0;

    public static void UpdateState(IBlockWriter writer, int x, int y, int z, int meta, bool occupied)
    {
        if (occupied)
        {
            meta |= 4;
        }
        else
        {
            meta &= ~4;
        }

        writer.SetBlockMeta(x, y, z, meta);
    }

    public static Vec3i? FindWakeUpPosition(IBlockReader reader, int x, int y, int z, int skip)
    {
        int blockMeta = reader.GetBlockMeta(x, y, z);
        int direction = GetDirection(blockMeta);

        if (IsHeadOfBed(blockMeta))
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
                    if (!reader.ShouldSuffocate(checkX, y - 1, checkZ) ||
                        !reader.IsAir(checkX, y, checkZ) ||
                        !reader.IsAir(checkX, y + 1, checkZ))
                    {
                        continue;
                    }


                    if (skip <= 0)
                    {
                        return new Vec3i(checkX, y, checkZ);
                    }

                    --skip;
                }
            }
        }

        return null;
    }

    public override void DropStacks(OnDropEvent @event)
    {
        if (!IsHeadOfBed(@event.Meta))
        {
            base.DropStacks(@event);
        }
    }
}
