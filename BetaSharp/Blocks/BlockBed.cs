using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockBed : Block
{
    // List of facings the bed can have, indexed by the direction the bed is facing (0-3) and the side being rendered (0-5).
    public static readonly Side[][] BedFacings =
    [
        [Side.Up, Side.Down, Side.South, Side.North, Side.East, Side.West],
        [Side.Up, Side.Down, Side.East, Side.West, Side.North, Side.South],
        [Side.Up, Side.Down, Side.North, Side.South, Side.West, Side.East],
        [Side.Up, Side.Down, Side.West, Side.East, Side.South, Side.North]
    ];

    // Offsets to find the other half of the bed based on the direction it is facing.
    private static readonly int[][] s_bedOffsets = [[0, 1], [-1, 0], [0, -1], [1, 0]];

    public BlockBed(int id) : base(id, 134, Material.Wool) => SetDefaultShape();

    public override bool OnUse(OnUseEvent e)
    {
        if (e.World.IsRemote)
        {
            return true;
        }

        (int x, int y, int z) = (e.X, e.Y, e.Z);

        int meta = e.World.Reader.GetBlockMeta(x, y, z);
        if (!IsHeadOfBed(meta))
        {
            int direction = GetDirection(meta);
            x += s_bedOffsets[direction][0];
            z += s_bedOffsets[direction][1];

            if (e.World.Reader.GetBlockId(x, y, z) != ID)
            {
                return true;
            }

            meta = e.World.Reader.GetBlockMeta(x, y, z);
        }

        if (!e.World.Dimension.HasWorldSpawn)
        {
            double posX = x + 0.5D;
            double posY = y + 0.5D;
            double posZ = z + 0.5D;
            e.World.Writer.SetBlock(x, y, z, 0);

            int direction = GetDirection(meta);
            x += s_bedOffsets[direction][0];
            z += s_bedOffsets[direction][1];

            if (e.World.Reader.GetBlockId(x, y, z) == ID)
            {
                e.World.Writer.SetBlock(x, y, z, 0);
                posX = (posX + x + 0.5D) / 2.0D;
                posY = (posY + y + 0.5D) / 2.0D;
                posZ = (posZ + z + 0.5D) / 2.0D;
            }

            e.World.CreateExplosion(null, x + 0.5F, y + 0.5F, z + 0.5F, 5.0F, true);
            return true;
        }

        if (IsBedOccupied(meta))
        {
            EntityPlayer? occupant = null;
            foreach (EntityPlayer otherPlayer in e.World.Entities.Players)
            {
                if (!otherPlayer.isSleeping())
                {
                    continue;
                }

                Vec3i sleepingPos = otherPlayer.sleepingPos;
                if (sleepingPos.X == x && sleepingPos.Y == y && sleepingPos.Z == z)
                {
                    occupant = otherPlayer;
                }
            }

            if (occupant != null)
            {
                e.Player.sendMessage("tile.bed.occupied");
                return true;
            }

            UpdateState(e.World.Writer, x, y, z, meta, false);
        }

        SleepAttemptResult result = e.Player.trySleep(x, y, z);
        switch (result)
        {
            case SleepAttemptResult.OK:
                UpdateState(e.World.Writer, x, y, z, meta, true);
                return true;
            case SleepAttemptResult.NOT_POSSIBLE_NOW:
                e.Player.sendMessage("tile.bed.noSleep");
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
        int direction = GetDirection(meta);
        Side sideFacing = BedFacings[direction][side.ToInt()];
        if (side == Side.Down)
        {
            return Planks.TextureId;
        }

        if (IsHeadOfBed(meta))
        {
            if (sideFacing == Side.North)
            {
                return TextureId + 2 + 16;
            }

            if (sideFacing != Side.East && sideFacing != Side.West)
            {
                return TextureId + 1;
            }

            return TextureId + 1 + 16;
        }

        if (sideFacing == Side.South)
        {
            return TextureId - 1 + 16;
        }

        if (sideFacing != Side.East && sideFacing != Side.West)
        {
            return TextureId;
        }

        return TextureId + 16;
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.Bed;

    public override bool IsFullCube() => false;

    public override bool IsOpaque() => false;

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => SetDefaultShape();

    public override void NeighborUpdate(OnTickEvent ctx)
    {
        int blockMeta = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int direction = GetDirection(blockMeta);

        if (IsHeadOfBed(blockMeta))
        {
            if (ctx.World.Reader.GetBlockId(ctx.X - s_bedOffsets[direction][0], ctx.Y, ctx.Z - s_bedOffsets[direction][1]) != ID)
            {
                ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
        else if (ctx.World.Reader.GetBlockId(ctx.X + s_bedOffsets[direction][0], ctx.Y, ctx.Z + s_bedOffsets[direction][1]) != ID)
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

    public static bool IsBedOccupied(int meta) => (meta & 4) != 0;

    public static void UpdateState(IBlockWriter worldWriter, int x, int y, int z, int meta, bool occupied)
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

                    if (skip <= 0) return new Vec3i(checkX, y, checkZ);

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

    public override PistonBehavior GetPistonBehavior() => PistonBehavior.Break;
}
